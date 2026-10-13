using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using Sharper.Step2Constraints.Models;
using Sharper.Step2Constraints.Runtime;
using Sharper.Step2Constraints.Rules;

namespace Sharper.Step2Constraints;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static async Task<int> Main(string[] args)
    {
        var traceOption = new Option<FileInfo>(
            name: "--trace",
            description: "Path to trace_logs.jsonl from Step 1")
        { IsRequired = true };

        var reportOption = new Option<FileInfo>(
            name: "--report",
            description: "Destination JSON file for constraint violations")
        { IsRequired = true };

        var root = new RootCommand("Sharper Step 2 - Constraint Analyzer");
        root.AddOption(traceOption);
        root.AddOption(reportOption);
        root.SetHandler(async (FileInfo trace, FileInfo report) =>
        {
            var violations = await AnalyzeAsync(trace, report);
            Console.WriteLine($"Trace analyzed. Violations: {violations.Count}");
        }, traceOption, reportOption);

        return await root.InvokeAsync(args);
    }

    private static async Task<IReadOnlyList<ConstraintViolation>> AnalyzeAsync(FileInfo traceFile, FileInfo reportFile)
    {
        if (!traceFile.Exists)
        {
            throw new FileNotFoundException("Trace file not found.", traceFile.FullName);
        }

        reportFile.Directory?.Create();

        var tracker = new StateTracker();
        var rules = new List<IRule>
        {
            new BufferBoundsRule(),
            new NullDereferenceRule(),
            new ExceptionRule(),
            new InvalidCastingRule(),
            new IntegerOverflowRule()
        };

        var violations = new List<ConstraintViolation>();
        var pendingExceptions = new List<PendingExceptionViolation>();
        var handledExceptions = new List<HandledExceptionEvent>();
        var pendingMemorySafety = new List<PendingMemorySafetyViolation>();
        var observedFaults = new List<ObservedFault>();
        var eventIndex = 0;

        await foreach (var evt in TraceReader.ReadAsync(traceFile.FullName))
        {
            tracker.Update(evt);

            if (IsHandledExceptionType(evt, out var handledType))
            {
                handledExceptions.Add(new HandledExceptionEvent(
                    eventIndex,
                    evt.ThreadId,
                    handledType));
            }

            // Record the actual runtime faults the program threw, so memory-safety
            // predictions can be corroborated against real evidence below.
            if (TryGetObservedFault(evt, out var observedKind))
            {
                observedFaults.Add(new ObservedFault(eventIndex, evt.ThreadId, evt.File, observedKind));
            }

            foreach (var rule in rules)
            {
                if (rule.TryEvaluate(evt, tracker, out var violation))
                {
                    if (violation.Predicate == "NoUnhandledException(event)"
                        && TryGetExceptionType(violation, out var exceptionType))
                    {
                        pendingExceptions.Add(new PendingExceptionViolation(
                            violation,
                            eventIndex,
                            evt.ThreadId,
                            exceptionType));
                    }
                    else if (TryGetRequiredFaultKind(violation.Predicate, out var requiredKind))
                    {
                        // Forward memory-safety predictions (buffer bounds, null
                        // dereference, invalid cast) are only trustworthy if the
                        // program actually faulted. Defer and corroborate.
                        pendingMemorySafety.Add(new PendingMemorySafetyViolation(
                            violation,
                            eventIndex,
                            evt.ThreadId,
                            violation.File,
                            requiredKind));
                    }
                    else
                    {
                        violations.Add(violation);
                    }
                }
            }

            eventIndex++;
        }

        violations.AddRange(pendingExceptions
            .Where(candidate => !IsHandledByCaller(candidate, handledExceptions))
            .Select(candidate => candidate.Violation));

        // Keep a memory-safety violation only when a matching runtime fault was
        // actually observed on the same thread and in the same source file,
        // within a small forward window. This removes false positives such as
        // circular-buffer modulo indexing (no IndexOutOfRangeException ever
        // thrown) and cross-method null mis-attribution (the real
        // NullReferenceException is in a different file).
        violations.AddRange(pendingMemorySafety
            .Where(candidate => IsCorroboratedByRuntimeFault(candidate, observedFaults))
            .Select(candidate => candidate.Violation));

        await using var stream = reportFile.Create();
        await JsonSerializer.SerializeAsync(stream, violations, JsonOptions);

        return violations;
    }

    private static bool IsHandledExceptionType(TraceEvent evt, out string exceptionType)
    {
        exceptionType = "";
        if (!string.Equals(evt.Message, "Handled Exception Type", StringComparison.OrdinalIgnoreCase)
            || evt.Variable?.Value is not { } value)
        {
            return false;
        }

        exceptionType = value;
        return !string.IsNullOrWhiteSpace(exceptionType);
    }

    private static bool TryGetExceptionType(ConstraintViolation violation, out string exceptionType)
    {
        exceptionType = "";
        if (!violation.Bindings.TryGetValue("event.exception.type", out var value)
            || value is not string text
            || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        exceptionType = text;
        return true;
    }

    private static bool IsHandledByCaller(
        PendingExceptionViolation candidate,
        IReadOnlyList<HandledExceptionEvent> handledExceptions)
    {
        const int MaxForwardEventGap = 16;
        return handledExceptions.Any(handled =>
            handled.ThreadId == candidate.ThreadId
            && handled.Index > candidate.Index
            && handled.Index - candidate.Index <= MaxForwardEventGap
            && string.Equals(handled.ExceptionType, candidate.ExceptionType, StringComparison.Ordinal));
    }

    // Maps a memory-safety predicate to the runtime fault that must actually
    // occur for the prediction to be real. Predicates not listed here are not
    // corroboration-gated (they are already evidence-based).
    private static bool TryGetRequiredFaultKind(string predicate, out FaultKind kind)
    {
        kind = default;
        if (predicate.StartsWith("BufferInBounds", StringComparison.Ordinal)) { kind = FaultKind.Index; return true; }
        if (predicate.StartsWith("NotNull", StringComparison.Ordinal)) { kind = FaultKind.Null; return true; }
        if (predicate.StartsWith("ValidCast", StringComparison.Ordinal)) { kind = FaultKind.Cast; return true; }
        return false;
    }

    // Recognises trace events that represent an actually-thrown runtime fault:
    // the SafetyRewriter's caught "Null Dereference" signal, or an
    // "Exception Type" record for IndexOutOfRange / NullReference / InvalidCast.
    private static bool TryGetObservedFault(TraceEvent evt, out FaultKind kind)
    {
        kind = default;

        if (string.Equals(evt.Message, "Null Dereference", StringComparison.OrdinalIgnoreCase))
        {
            kind = FaultKind.Null;
            return true;
        }

        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable?.Value is { } value)
        {
            if (value.Contains("IndexOutOfRangeException", StringComparison.OrdinalIgnoreCase)) { kind = FaultKind.Index; return true; }
            if (value.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)) { kind = FaultKind.Null; return true; }
            if (value.Contains("InvalidCastException", StringComparison.OrdinalIgnoreCase)) { kind = FaultKind.Cast; return true; }
        }

        return false;
    }

    // A predicted memory-safety violation is kept only when the program threw a
    // matching fault on the same thread, in the same source file, within a small
    // event window around the prediction. Same-thread + same-file is the key
    // discriminator: it removes circular-buffer modulo false positives (no fault
    // ever thrown) and cross-file null mis-attribution.
    private static bool IsCorroboratedByRuntimeFault(
        PendingMemorySafetyViolation candidate,
        IReadOnlyList<ObservedFault> observedFaults)
    {
        const int BackwardEventGap = 8;
        const int ForwardEventGap = 64;
        var candidateFile = SafeFileName(candidate.File);

        return observedFaults.Any(fault =>
            fault.Kind == candidate.Kind
            && fault.ThreadId == candidate.ThreadId
            && string.Equals(SafeFileName(fault.File), candidateFile, StringComparison.OrdinalIgnoreCase)
            && fault.Index - candidate.Index >= -BackwardEventGap
            && fault.Index - candidate.Index <= ForwardEventGap);
    }

    private static string SafeFileName(string? path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        var normalized = path.Replace('\\', '/');
        var slash = normalized.LastIndexOf('/');
        return slash >= 0 ? normalized.Substring(slash + 1) : normalized;
    }

    private enum FaultKind
    {
        Index,
        Null,
        Cast
    }

    private sealed record PendingExceptionViolation(
        ConstraintViolation Violation,
        int Index,
        int ThreadId,
        string ExceptionType);

    private sealed record HandledExceptionEvent(
        int Index,
        int ThreadId,
        string ExceptionType);

    private sealed record PendingMemorySafetyViolation(
        ConstraintViolation Violation,
        int Index,
        int ThreadId,
        string File,
        FaultKind Kind);

    private sealed record ObservedFault(
        int Index,
        int ThreadId,
        string File,
        FaultKind Kind);
}

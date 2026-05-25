using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;
using AutoRepair.Step2Constraints.Rules;

namespace AutoRepair.Step2Constraints;

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

        var root = new RootCommand("AutoRepair Step 2 – Constraint Analyzer");
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

    private sealed record PendingExceptionViolation(
        ConstraintViolation Violation,
        int Index,
        int ThreadId,
        string ExceptionType);

    private sealed record HandledExceptionEvent(
        int Index,
        int ThreadId,
        string ExceptionType);
}

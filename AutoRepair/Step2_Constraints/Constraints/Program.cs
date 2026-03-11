using System.CommandLine;
using System.Globalization;
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
            new InvocationFailureRule(),
            new ExceptionRule(),
            new InvalidCastingRule(),
            new IntegerOverflowRule()
        };

        var violations = new List<ConstraintViolation>();
        var seenViolationKeys = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var evt in TraceReader.ReadAsync(traceFile.FullName))
        {
            tracker.Update(evt);

            foreach (var rule in rules)
            {
                if (rule.TryEvaluate(evt, tracker, out var violation))
                {
                    var key = BuildViolationKey(violation);
                    if (seenViolationKeys.Add(key))
                    {
                        violations.Add(violation);
                    }
                }
            }
        }

        await using var stream = reportFile.Create();
        await JsonSerializer.SerializeAsync(stream, violations, JsonOptions);

        return violations;
    }

    private static string BuildViolationKey(ConstraintViolation violation) =>
        string.Join("|",
            violation.Predicate,
            violation.Assertion,
            violation.File,
            violation.Line.ToString(CultureInfo.InvariantCulture),
            violation.TraceId);
}

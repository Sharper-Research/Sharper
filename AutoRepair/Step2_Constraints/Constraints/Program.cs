using System.CommandLine;                   // for command-line argument parsing
using System.Text.Encodings.Web;            // for relaxed json encoding
using System.Text.Json;                     // for serializing violations to json
using AutoRepair.Step2Constraints.Models;   // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime;  // for TraceReader, StateTracker
using AutoRepair.Step2Constraints.Rules;    // for all the rule classes

namespace AutoRepair.Step2Constraints;

// entry point for step 2: reads trace logs from step 1 and checks them against rules
internal static class Program
{
    // json options: camelCase, indented, and relaxed encoding so special chars dont get escaped weirdly
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static async Task<int> Main(string[] args)
    {
        // set up the --trace argument (the jsonl file from step 1)
        var traceOption = new Option<FileInfo>(
            name: "--trace",
            description: "Path to trace_logs.jsonl from Step 1")
        { IsRequired = true };

        // set up the --report argument (where to write the violations json)
        var reportOption = new Option<FileInfo>(
            name: "--report",
            description: "Destination JSON file for constraint violations")
        { IsRequired = true };

        // wire it all up with System.CommandLine
        var root = new RootCommand("AutoRepair Step 2 – Constraint Analyzer");
        root.AddOption(traceOption);
        root.AddOption(reportOption);
        root.SetHandler(async (FileInfo trace, FileInfo report) =>
        {
            var violations = await AnalyzeAsync(trace, report);
            Console.WriteLine($"Trace analyzed. Violations: {violations.Count}");
        }, traceOption, reportOption);

        return await root.InvokeAsync(args); // run the command
    }

    // does the actual analysis: reads events from the trace file and checks each one against every rule
    private static async Task<IReadOnlyList<ConstraintViolation>> AnalyzeAsync(FileInfo traceFile, FileInfo reportFile)
    {
        if (!traceFile.Exists)
        {
            throw new FileNotFoundException("Trace file not found.", traceFile.FullName);
        }

        reportFile.Directory?.Create(); // make sure the output folder exists

        var tracker = new StateTracker(); // keeps track of heap state, buffer sizes, etc

        // create all the rules we want to check
        // order matters because the first matching rule wins for certain events
        var rules = new List<IRule>
        {
            new BufferBoundsRule(),       // array index out of bounds
            new OffByOneRule(),           // index == array.Length (the <= vs < mistake)
            new NullDereferenceRule(),    // null reference errors
            new ExceptionRule(),          // generic exception catch-all
            new InvalidCastingRule(),     // bad type casts
            new IntegerOverflowRule()     // arithmetic overflow
        };

        var violations = new List<ConstraintViolation>();

        // read through every trace event one by one
        await foreach (var evt in TraceReader.ReadAsync(traceFile.FullName))
        {
            tracker.Update(evt); // update the state tracker first

            // then run every rule against the event
            foreach (var rule in rules)
            {
                if (rule.TryEvaluate(evt, tracker, out var violation))
                {
                    violations.Add(violation); // got a violation, save it
                }
            }
        }

        // write all violations to the report file as json
        await using var stream = reportFile.Create();
        await JsonSerializer.SerializeAsync(stream, violations, JsonOptions);

        return violations;
    }
}

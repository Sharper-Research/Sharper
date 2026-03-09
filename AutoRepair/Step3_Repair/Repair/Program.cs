using System.CommandLine;                   // for command-line argument parsing
using System.Text.Encodings.Web;            // for relaxed json encoding
using System.Text.Json;                     // for serializing the patch report
using AutoRepair.Step3Repair.Core;          // for PatchEngine
using AutoRepair.Step3Repair.Strategies;    // for all the repair strategy classes

namespace AutoRepair.Step3Repair;

// entry point for step 3: reads violations from step 2 and generates patches
internal static class Program
{
    // json formatting options: camelCase keys, indented, relaxed encoding
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static async Task<int> Main(string[] args)
    {
        // set up the required --violations argument (json from step 2)
        var violationsOption = new Option<FileInfo>(
            name: "--violations",
            description: "Path to violations.json from Step 2")
        { IsRequired = true };

        // set up the required --target argument (the project to patch)
        var targetOption = new Option<DirectoryInfo>(
            name: "--target",
            description: "Root directory of the target source project")
        { IsRequired = true };

        // set up the required --report argument (where to write patches.json)
        var reportOption = new Option<FileInfo>(
            name: "--report",
            description: "Destination JSON file for the patch report")
        { IsRequired = true };

        // optional --apply flag: false means dry-run (just generate report), true means actually write patches
        var applyOption = new Option<bool>(
            name: "--apply",
            description: "Apply patches to source files (default: false, dry-run only)")
        { };
        applyOption.SetDefaultValue(false);

        // wire up all the options with System.CommandLine
        var root = new RootCommand("AutoRepair Step 3 – Patch Generator");
        root.AddOption(violationsOption);
        root.AddOption(targetOption);
        root.AddOption(reportOption);
        root.AddOption(applyOption);

        root.SetHandler(async (FileInfo violations, DirectoryInfo target, FileInfo report, bool apply) =>
        {
            // print a nice header
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║   AutoRepair Step 3 – Patch Generation    ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            // make sure the violations file exists before we try to read it
            if (!violations.Exists)
            {
                Console.Error.WriteLine($"Error: Violations file not found: {violations.FullName}");
                Console.Error.WriteLine("Run Step 2 (analyze_trace.ps1) first to generate violations.json.");
                return;
            }

            // make sure the target project directory exists
            if (!target.Exists)
            {
                Console.Error.WriteLine($"Error: Target directory not found: {target.FullName}");
                return;
            }

            // print out what were going to do
            Console.WriteLine($"Violations: {violations.FullName}");
            Console.WriteLine($"Target:     {target.FullName}");
            Console.WriteLine($"Report:     {report.FullName}");
            Console.WriteLine($"Mode:       {(apply ? "APPLY patches" : "DRY-RUN (report only)")}");
            Console.WriteLine();

            // create the patch engine with all our repair strategies
            // order matters: more specific strategies go first so they get first crack at each violation
            var engine = new PatchEngine(new IRepairStrategy[]
            {
                new OffByOneRepairStrategy(),         // most specific, check before BufferBounds
                new BufferBoundsRepairStrategy(),
                new NullDereferenceRepairStrategy(),
                new InvalidCastRepairStrategy(),
                new IntegerOverflowRepairStrategy(),
                new ExceptionRepairStrategy()          // least specific, generic fallback
            });

            // run the whole pipeline
            var patchReport = await engine.RunAsync(
                violations.FullName,
                target.FullName,
                report.FullName,
                apply);

            // write the report out to json
            report.Directory?.Create();
            await using var stream = report.Create();
            await JsonSerializer.SerializeAsync(stream, patchReport, JsonOptions);

            // print a summary of what happened
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"  Total violations:  {patchReport.TotalViolations}");
            Console.WriteLine($"  Patches generated: {patchReport.PatchesApplied}");
            Console.WriteLine($"  Skipped:           {patchReport.PatchesSkipped}");
            Console.WriteLine($"  Report:            {report.FullName}");
            if (apply)
                Console.WriteLine("  Status:            Patches APPLIED to source files");
            else
                Console.WriteLine("  Status:            Dry-run complete (use --apply to write patches)");
            Console.WriteLine("═══════════════════════════════════════");

        }, violationsOption, targetOption, reportOption, applyOption);

        return await root.InvokeAsync(args); // kick it off
    }
}

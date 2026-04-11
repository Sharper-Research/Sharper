using System.CommandLine;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoRepair.Step3Repair.Analysis;
using AutoRepair.Step3Repair.Models;

namespace AutoRepair.Step3Repair;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true,
        Encoder              = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static async Task<int> Main(string[] args)
    {
        var violationsOpt = new Option<FileInfo>(
            name: "--violations",
            description: "Path to violations.json from Step 2")
        { IsRequired = true };

        var sourceOpt = new Option<DirectoryInfo>(
            name: "--source",
            description: "Root directory of the target source code")
        { IsRequired = true };

        var reportOpt = new Option<FileInfo>(
            name: "--report",
            description: "Destination JSON file for CFG report")
        { IsRequired = true };

        var root = new RootCommand("AutoRepair Step 3 – Task 1: CFG Builder");
        root.AddOption(violationsOpt);
        root.AddOption(sourceOpt);
        root.AddOption(reportOpt);

        root.SetHandler(async (FileInfo vf, DirectoryInfo sd, FileInfo rf) =>
        {
            await RunAsync(vf, sd, rf);
        }, violationsOpt, sourceOpt, reportOpt);

        return await root.InvokeAsync(args);
    }

    private static async Task RunAsync(FileInfo violationsFile, DirectoryInfo sourceDir, FileInfo reportFile)
    {
        if (!violationsFile.Exists)
            throw new FileNotFoundException("Violations file not found.", violationsFile.FullName);
        if (!sourceDir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir.FullName}");

        reportFile.Directory?.Create();

        // 1. Read violations
        await using var inStream = violationsFile.OpenRead();
        var violations = await JsonSerializer.DeserializeAsync<List<ViolationInput>>(inStream, JsonOpts)
                         ?? new List<ViolationInput>();
        Console.WriteLine($"Loaded {violations.Count} violation(s) from {violationsFile.Name}");

        // 2. Build CFGs for each file containing violations
        var locator = new SourceLocator(sourceDir.FullName);
        var builder = new CfgBuilder();
        var methods = new List<MethodCfg>();

        foreach (var group in violations.GroupBy(v => v.File))
        {
            var fullPath = locator.FindFile(group.Key);
            if (fullPath == null)
            {
                Console.WriteLine($"  [skip] Cannot locate source: {group.Key}");
                continue;
            }

            Console.WriteLine($"  Analyzing: {fullPath}");
            var lines = group.Select(v => v.Line).Distinct().ToList();
            methods.AddRange(builder.BuildForFile(fullPath, group.Key, lines));
        }

        // 3. Write report
        var report = new CfgReport(
            GeneratedAt:    DateTime.UtcNow,
            SourceDirectory: sourceDir.FullName,
            TotalViolations: violations.Count,
            MethodsAnalyzed: methods.Count,
            Methods:         methods);

        await using var outStream = reportFile.Create();
        await JsonSerializer.SerializeAsync(outStream, report, JsonOpts);

        // 4. Console summary
        Console.WriteLine($"\nCFG report: {methods.Count} method(s) analyzed → {reportFile.FullName}");
        foreach (var m in methods)
        {
            Console.WriteLine($"  • {m.MethodName} ({m.File}:{m.MethodStartLine}-{m.MethodEndLine})");
            Console.WriteLine($"    Violations at line(s): {string.Join(", ", m.ViolationLines)}");
            Console.WriteLine($"    Nodes: {m.Nodes.Count}, Edges: {m.Edges.Count}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Repair;

namespace AutoRepair.CFGAnalyzer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var parseResult = ParseArguments(args);
        if (parseResult == null)
        {
            Console.WriteLine("Usage: CFGAnalyzer <project-or-solution-path> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --violations <path>             Drive analysis from violations.json (Step 2 output).");
            Console.WriteLine("  --output-dir <path>             Directory for repair_plan.json + patches/.");
            Console.WriteLine("  --apply-patches                 Apply generated patches in-place. Originals are backed up to *.autorepair.bak.");
            Console.WriteLine("  --restore                       Restore files from *.autorepair.bak using the existing repair_plan.json.");
            Console.WriteLine("  --exclude-system-library-targets  Trim System.* targets from the call graph.");
            Console.WriteLine("  --disable-resolver <name>       Disable a call resolver. Names: " + string.Join(", ", Enum.GetNames<CallResolver>()));
            return 1;
        }

        var (path, options) = parseResult.Value;

        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

        try
        {
            using var workspace = MSBuildWorkspace.Create();
            workspace.RegisterWorkspaceFailedHandler(e => Console.WriteLine($"[Workspace Warning] {e.Diagnostic.Message}"));

            Solution? solution = null;
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solution = await workspace.OpenSolutionAsync(path);
            }
            else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var project = await workspace.OpenProjectAsync(path);
                solution = project.Solution;
            }
            else
            {
                Console.WriteLine("Unsupported path. Please provide a .sln or .csproj file.");
                return 2;
            }

            ReportDiagnostics(workspace);

            // Violation-driven mode (Task 2 + Task 3).
            if (!string.IsNullOrEmpty(options.ViolationsPath))
            {
                var outputDir = options.OutputDir
                    ?? Path.Combine(Path.GetDirectoryName(path)!, "Step3_Repair", "reports");

                Console.WriteLine($"\n--- Violation-driven repair ---");
                Console.WriteLine($"  Violations: {options.ViolationsPath}");
                Console.WriteLine($"  Output:     {outputDir}");

                if (options.Restore)
                {
                    var planPath = Path.Combine(outputDir, "repair_plan.json");
                    if (!File.Exists(planPath))
                    {
                        Console.WriteLine($"[Error] No prior repair_plan.json at {planPath}");
                        return 3;
                    }
                    var prior = System.Text.Json.JsonSerializer.Deserialize<RepairPlan>(
                        File.ReadAllText(planPath),
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        });
                    if (prior == null) { Console.WriteLine("[Error] Could not parse repair_plan.json"); return 3; }
                    var restored = PatchSynthesizer.RestoreFromBackups(prior);
                    Console.WriteLine($"  Restored {restored} file(s) from *.autorepair.bak");
                    return 0;
                }

                var plan = await ViolationDrivenRepair.RunAsync(solution, options.ViolationsPath, outputDir);
                PrintPlanSummary(plan);

                if (options.ApplyPatches)
                {
                    var applied = PatchSynthesizer.ApplyInPlace(plan);
                    Console.WriteLine($"\n[Apply] Patched {applied} file(s) in-place. Backups: *.autorepair.bak");
                }
                return 0;
            }

            // Legacy entry-point traversal mode.
            foreach (var project in solution.Projects)
                await AnalyzeAndTraverse(project, options.CallGraphConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            return 4;
        }

        return 0;
    }

    static void PrintPlanSummary(RepairPlan plan)
    {
        Console.WriteLine($"\n--- Repair plan ---");
        Console.WriteLine($"  Total violations: {plan.TotalViolations}");
        foreach (var (k, v) in plan.ByAction.OrderBy(kv => kv.Key))
            Console.WriteLine($"    {k,-25} {v}");

        Console.WriteLine();
        foreach (var item in plan.Items)
        {
            var marker = item.Action == RepairAction.AutoPatchable ? "[PATCH]" : "[skip ]";
            Console.WriteLine($"  {marker} {item.Violation.File}:{item.Violation.Line} ({item.Action})  {item.Violation.ExceptionType}");
            Console.WriteLine($"          in {item.ContainingMember}");
            Console.WriteLine($"          {item.ActionReason}");
            if (item.DiffPath != null)
                Console.WriteLine($"          diff:  {item.DiffPath}");
        }
    }

    static (string Path, CliOptions Options)? ParseArguments(string[] args)
    {
        if (args.Length == 0) return null;

        var options = new CliOptions();
        string? path = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--exclude-system-library-targets")
            {
                options.CallGraphConfig.ExcludeSystemLibraryTargetsExceptConsole = true;
            }
            else if (arg == "--disable-resolver" && i + 1 < args.Length)
            {
                if (Enum.TryParse<CallResolver>(args[++i], ignoreCase: true, out var resolverName))
                    options.CallGraphConfig.DisabledResolvers.Add(resolverName);
                else
                    Console.WriteLine($"[Warning] Unknown resolver '{args[i]}'");
            }
            else if (arg == "--violations" && i + 1 < args.Length)
            {
                options.ViolationsPath = args[++i];
            }
            else if (arg == "--output-dir" && i + 1 < args.Length)
            {
                options.OutputDir = args[++i];
            }
            else if (arg == "--apply-patches")
            {
                options.ApplyPatches = true;
            }
            else if (arg == "--restore")
            {
                options.Restore = true;
            }
            else if (path == null)
            {
                path = arg;
            }
        }

        if (path == null) return null;
        return (path, options);
    }

    static void ReportDiagnostics(MSBuildWorkspace workspace)
    {
        foreach (var diag in workspace.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure))
        {
            Console.WriteLine($"[Workspace Error] {diag.Message}");
        }
    }

    static async Task AnalyzeAndTraverse(Project project, CallGraphConfig config)
    {
        Console.WriteLine($"\n--- Analyzing Project: {project.Name} ---");
        var callGraph = await ProjectAnalyzer.AnalyzeProjectAsync(project, config);

        var entryNodes = callGraph.Nodes.Values
            .Where(n => n.Symbol.Name == "Main" && n.Analysis != null)
            .ToList();

        if (entryNodes.Count == 0 && callGraph.Nodes.Count > 0)
        {
            var firstWithAnalysis = callGraph.Nodes.Values.FirstOrDefault(n => n.Analysis != null);
            if (firstWithAnalysis != null) entryNodes.Add(firstWithAnalysis);
        }

        foreach (var entry in entryNodes)
        {
            Console.WriteLine($"\n=== Call Graph Traversal starting from: {entry.Symbol.ToDisplayString()} ===");
            CallGraphTraverser.TraverseAndGeneratePathConditions(
                entry,
                new List<string>(),
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default),
                Console.Out);
        }
    }

    sealed class CliOptions
    {
        public CallGraphConfig CallGraphConfig { get; } = new()
        {
            DisabledResolvers = new HashSet<CallResolver>()
        };
        public string? ViolationsPath { get; set; }
        public string? OutputDir { get; set; }
        public bool ApplyPatches { get; set; }
        public bool Restore { get; set; }
    }
}

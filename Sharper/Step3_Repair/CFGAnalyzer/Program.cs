using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Sharper.CFGAnalyzer.Analysis;
using Sharper.CFGAnalyzer.Repair;

namespace Sharper.CFGAnalyzer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() is "--verify-model" or "--check-activation") return await PaperModelCommand.RunAsync(args);
        var parseResult = ParseArguments(args);
        if (parseResult == null)
        {
            Console.WriteLine("Usage: CFGAnalyzer <project-or-solution-path> [options]");
            Console.WriteLine("       CFGAnalyzer --verify-model <model.json> [--output <result.json>]");
            Console.WriteLine("       CFGAnalyzer --check-activation <transition.json> [--output <result.json>]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --violations <path>             Drive analysis from violations.json (Step 2 output).");
            Console.WriteLine("  --output-dir <path>             Directory for repair_plan.json + patches/.");
            Console.WriteLine("  --test-project <path>           Validate candidates using a VSTest project.");
            Console.WriteLine("  --validation-root <path>        Complete target checkout to copy into an isolated sandbox.");
            Console.WriteLine("  --reproducer-filter <filter>    VSTest filter selecting the original failure.");
            Console.WriteLine("  --apply-patches                 Apply one test-accepted candidate; preserve *.sharper.bak.");
            Console.WriteLine("  --restore                       Restore files from *.sharper.bak using the existing repair_plan.json.");
            Console.WriteLine("  --exclude-system-library-targets  Trim System.* targets from the call graph.");
            Console.WriteLine("  --disable-resolver <name>       Disable a call resolver. Names: " + string.Join(", ", Enum.GetNames<CallResolver>()));
            return 1;
        }

        var (path, options) = parseResult.Value;
        if ((options.ApplyPatches && options.TestProject == null)
            || (options.TestProject != null && (options.ValidationRoot == null || string.IsNullOrWhiteSpace(options.ReproducerFilter)))
            || (options.TestProject == null && (options.ValidationRoot != null || options.ReproducerFilter != null))
            || (options.TestProject != null && options.ViolationsPath == null)
            || (options.Restore && options.ApplyPatches))
        {
            Console.Error.WriteLine("Validation requires --violations, --test-project, --validation-root and --reproducer-filter. Applying requires validation and cannot be combined with restore.");
            return 1;
        }

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
                    Console.WriteLine($"  Restored {restored} file(s) from *.sharper.bak");
                    return 0;
                }

                var plan = await ViolationDrivenRepair.RunAsync(solution, options.ViolationsPath, outputDir);
                if (options.TestProject != null)
                {
                    var cache = new Dictionary<(string Source, string Candidate), CandidateValidation>();
                    for (var index = 0; index < plan.Items.Count; index++)
                    {
                        var item = plan.Items[index];
                        if (item.PatchedFilePath == null) continue;
                        var key = (item.ContainingFilePath, item.PatchedFilePath);
                        if (!cache.TryGetValue(key, out var validation))
                        {
                            var directory = Path.Combine(outputDir, "validation", Guid.NewGuid().ToString("N"));
                            validation = await CandidateValidator.ValidateAsync(item, options.ValidationRoot!, options.TestProject,
                                options.ReproducerFilter!, directory);
                            cache.Add(key, validation);
                        }
                        plan.Items[index] = item with { Validation = validation };
                    }
                    await File.WriteAllTextAsync(Path.Combine(outputDir, "repair_plan.json"),
                        System.Text.Json.JsonSerializer.Serialize(plan, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        }));
                }
                PrintPlanSummary(plan);

                if (options.TestProject != null && (!plan.Items.Any(item => item.Validation?.Status == "AcceptedByTests")
                    || plan.Items.Any(item => item.Validation is { Status: not "AcceptedByTests" }))) return 5;
                if (options.ApplyPatches)
                {
                    var applied = PatchSynthesizer.ApplyInPlace(plan);
                    Console.WriteLine($"\n[Apply] Patched {applied} file(s) in-place. Backups: *.sharper.bak");
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
        Console.WriteLine($"  Verification: {plan.VerificationStatus} (test acceptance is not semantic proof)");
        foreach (var (k, v) in plan.ByAction.OrderBy(kv => kv.Key))
            Console.WriteLine($"    {k,-25} {v}");

        Console.WriteLine();
        foreach (var item in plan.Items)
        {
            var marker = item.Action == RepairAction.AutoPatchable ? "[PATCH]" : "[skip ]";
            Console.WriteLine($"  {marker} {item.Violation.File}:{item.Violation.Line} ({item.Action})  {item.Violation.ExceptionType}");
            Console.WriteLine($"          in {item.ContainingMember}");
            Console.WriteLine($"          {item.ActionReason}");
            if (item.Validation is { } validation)
                Console.WriteLine($"          Test gate: {validation.Status}: {validation.Reason}");
            if (item.FormulaAnalysis is { } formula)
            {
                Console.WriteLine($"          Backward: {formula.Status}; forward model: {formula.ForwardVerificationStatus}");
                Console.WriteLine($"          Scope: {formula.ForwardVerificationScope}");
            }
            if (item.CandidateModelCheck is { } candidateCheck)
            {
                Console.WriteLine($"          Emitted candidate model: {candidateCheck.Status}");
                Console.WriteLine($"          Candidate scope: {item.CandidateVerificationScope}");
            }
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
            else if (arg == "--test-project" && i + 1 < args.Length) options.TestProject = args[++i];
            else if (arg == "--validation-root" && i + 1 < args.Length) options.ValidationRoot = args[++i];
            else if (arg == "--reproducer-filter" && i + 1 < args.Length) options.ReproducerFilter = args[++i];
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
        public string? TestProject { get; set; }
        public string? ValidationRoot { get; set; }
        public string? ReproducerFilter { get; set; }
    }
}

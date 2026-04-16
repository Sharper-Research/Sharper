using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Traversal;

namespace AutoRepair.CFGAnalyzer;

class Program
{
    static async Task Main(string[] args)
    {
        var parseResult = ParseArguments(args);
        if (parseResult == null)
        {
            Console.WriteLine("Usage: CFGAnalyzer <project-or-solution-path>");
            Console.WriteLine("Example: CFGAnalyzer /abs/path/to/Target.sln");
            Console.WriteLine("Optional flags: --exclude-system-library-targets (keeps System.Console.*)");
            Console.WriteLine("                --disable-resolver <name>  Disable a call resolver by class name.");
            Console.WriteLine($"                  Known names: {string.Join(", ", Enum.GetNames<CallResolver>())}");
            return;
        }

        var (path, options) = parseResult.Value;

        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

        try
        {
            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (s, e) => Console.WriteLine($"[Workspace Warning] {e.Diagnostic.Message}");
            
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var solution = await workspace.OpenSolutionAsync(path);
                ReportDiagnostics(workspace);
                foreach (var project in solution.Projects) await AnalyzeAndTraverse(project, options);
            }
            else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var project = await workspace.OpenProjectAsync(path);
                ReportDiagnostics(workspace);
                await AnalyzeAndTraverse(project, options);
            }
            else
            {
                Console.WriteLine("Unsupported path. Please provide a .sln or .csproj file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static (string Path, CallGraphConfig Config)? ParseArguments(string[] args)
    {
        if (args.Length == 0) return null;

        bool excludeSystemLibraryTargets = false;
        var disabledResolvers = new HashSet<CallResolver>();
        string? path = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--exclude-system-library-targets")
            {
                excludeSystemLibraryTargets = true;
            }
            else if (arg == "--disable-resolver")
            {
                if (i + 1 < args.Length)
                {
                    if (Enum.TryParse<CallResolver>(args[++i], ignoreCase: true, out var resolverName))
                        disabledResolvers.Add(resolverName);
                    else
                        Console.WriteLine($"[Warning] Unknown resolver name '{args[i]}'. Known names: {string.Join(", ", Enum.GetNames<CallResolver>())}");
                }
            }
            else if (path == null)
            {
                path = arg;
            }
        }

        if (path == null) return null;

        return (path, new CallGraphConfig
        {
            ExcludeSystemLibraryTargetsExceptConsole = excludeSystemLibraryTargets,
            DisabledResolvers = disabledResolvers
        });
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
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
        }
    }
}

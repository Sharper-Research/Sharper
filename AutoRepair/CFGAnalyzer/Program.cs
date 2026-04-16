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
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CFGAnalyzer <project-or-solution-path>");
            Console.WriteLine("Example: CFGAnalyzer /abs/path/to/Target.sln");
            return;
        }

        string path = args[0];

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
                foreach (var project in solution.Projects) await AnalyzeAndTraverse(project);
            }
            else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var project = await workspace.OpenProjectAsync(path);
                ReportDiagnostics(workspace);
                await AnalyzeAndTraverse(project);
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

    static void ReportDiagnostics(MSBuildWorkspace workspace)
    {
        foreach (var diag in workspace.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure))
        {
            Console.WriteLine($"[Workspace Error] {diag.Message}");
        }
    }


    static async Task AnalyzeAndTraverse(Project project)
    {
        Console.WriteLine($"\n--- Analyzing Project: {project.Name} ---");
        var callGraph = await ProjectAnalyzer.AnalyzeProjectAsync(project);

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

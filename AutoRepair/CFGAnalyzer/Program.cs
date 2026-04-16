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
            return;
        }

        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

        string path = args[0];
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
        var results = await ProjectAnalyzer.AnalyzeProjectAsync(project);

        var entryPoints = results.Keys.Where(s => s.Name == "Main").ToList();
        if (entryPoints.Count == 0 && results.Count > 0) entryPoints.Add(results.Keys.First());

        foreach (var entry in entryPoints)
        {
            Console.WriteLine($"\n=== Call Graph Traversal starting from: {entry.ToDisplayString()} ===");
            CallGraphTraverser.TraverseAndGeneratePathConditions(
                entry, 
                results, 
                new List<string>(), 
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
        }
    }
}

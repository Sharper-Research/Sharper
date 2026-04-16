using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CFGAnalyzer;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CFGAnalyzer <project-or-solution-path>");
            return;
        }

        string path = args[0];
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var solution = await workspace.OpenSolutionAsync(path);
                foreach (var project in solution.Projects) await AnalyzeAndTraverse(project);
            }
            else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var project = await workspace.OpenProjectAsync(path);
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

    static async Task AnalyzeAndTraverse(Project project)
    {
        Console.WriteLine($"\n--- Analyzing Project: {project.Name} ---");
        var results = await RoslynCfgAnalyzer.AnalyzeProjectAsync(project);

        // Find entry points (e.g., Main)
        var entryPoints = results.Keys.Where(s => s.Name == "Main").ToList();
        if (entryPoints.Count == 0 && results.Count > 0)
        {
            // Fallback: take the first method if no Main is found
            entryPoints.Add(results.Keys.First());
        }

        foreach (var entry in entryPoints)
        {
            Console.WriteLine($"\n=== Call Graph Traversal starting from: {entry.ToDisplayString()} ===");
            RoslynCfgAnalyzer.TraverseAndGeneratePathConditions(
                entry, 
                results, 
                new List<string>(), 
                new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
        }
    }
}

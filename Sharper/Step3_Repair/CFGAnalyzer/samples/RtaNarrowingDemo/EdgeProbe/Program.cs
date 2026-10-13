using Sharper.CFGAnalyzer.Analysis;
using Microsoft.CodeAnalysis.MSBuild;

if (args.Length == 0)
{
    Console.WriteLine("Usage: EdgeProbe <project-path>");
    return;
}

if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
{
    Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
}

string projectPath = args[0];
using var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);
var graph = await ProjectAnalyzer.AnalyzeProjectAsync(project);

var mainEdges = graph.Edges
    .Where(e => e.Caller.Symbol.Name == "Main")
    .Select(e => e.Callee.Symbol.ToDisplayString())
    .Distinct(StringComparer.Ordinal)
    .OrderBy(s => s, StringComparer.Ordinal)
    .ToList();

foreach (var callee in mainEdges)
{
    Console.WriteLine(callee);
}


using System.IO;
using System.Threading.Tasks;
using Sharper.CFGAnalyzer.Analysis;
using Sharper.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace Sharper.CFGAnalyzer.Tests;

public class CallGraphSnapshotTests
{
    [Fact]
    public async Task BranchAndReturnCallsAreDiscoveredWithoutInvokingDelegateDeclarations()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("CallSites", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Fixture.cs", """
            public static class Fixture
            {
                static bool Probe() => true;
                static int Value() => 1;
                public static int Entry()
                {
                    System.Func<int> used = () => Value();
                    System.Func<int> unused = () => Value();
                    if (Probe()) return used();
                    return Value();
                }
            }
            """);
        var graph = await ProjectAnalyzer.AnalyzeProjectAsync(document.Project);
        var entry = Assert.Single(graph.Nodes.Values, node => node.Symbol.Name == "Entry");
        Assert.Equal(3, entry.OutgoingEdges.Count);
        Assert.Contains(entry.OutgoingEdges, edge => edge.Callee.Symbol.Name == "Probe");
        Assert.Contains(entry.OutgoingEdges, edge => edge.Callee.Symbol.Name == "Value");
        Assert.Single(entry.OutgoingEdges, edge => edge.Callee.Symbol.MethodKind == Microsoft.CodeAnalysis.MethodKind.AnonymousFunction);
        Assert.All(entry.OutgoingEdges, edge => Assert.True(edge.CallSite.IsInternal));
        Assert.Equal(2, graph.Nodes.Keys.Count(symbol => symbol.MethodKind == Microsoft.CodeAnalysis.MethodKind.AnonymousFunction));
        var exported = System.Text.Json.JsonSerializer.Deserialize<CallGraphJsonDto>(CallGraphJsonExporter.ToJson(graph))!;
        var identities = exported.Nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(exported.Nodes.Count, identities.Count);
        Assert.All(exported.Edges, edge =>
        {
            Assert.Contains(edge.CallerId, identities);
            Assert.Contains(edge.CalleeId, identities);
        });
        foreach (var newline in new[] { "\n", "\r\n" })
        {
            var source = (await document.GetTextAsync()).ToString().ReplaceLineEndings(newline);
            var normalized = document.WithText(Microsoft.CodeAnalysis.Text.SourceText.From(source));
            var normalizedGraph = await ProjectAnalyzer.AnalyzeProjectAsync(normalized.Project);
            var normalizedExport = System.Text.Json.JsonSerializer.Deserialize<CallGraphJsonDto>(CallGraphJsonExporter.ToJson(normalizedGraph))!;
            Assert.Equal(exported.Nodes.Select(node => node.Id), normalizedExport.Nodes.Select(node => node.Id));
        }
    }

    [Fact]
    public async Task TopLevelEntryOwnsLocalFunctionControlFlowGraph()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("TopLevelCfg", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication))
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Program.cs", "int Local(int number) => number + 1; _ = Local(0);");
        var graph = await ProjectAnalyzer.AnalyzeProjectAsync(document.Project);
        var local = Assert.Single(graph.Nodes.Values, node => node.Symbol.Name == "Local");
        Assert.NotNull(local.Analysis);
        Assert.Contains(graph.Edges, edge => edge.Callee == local && edge.Caller.Analysis != null && edge.CallSite.IsInternal);
    }

    [Fact]
    public async Task NestedFunctionsHaveOwnedControlFlowGraphs()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("NestedCfg", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Fixture.cs", """
            public static class Fixture
            {
                public static int Read(int input)
                {
                    int Local(int value)
                    {
                        int Nested(int number) => number + 1;
                        return Nested(value);
                    }
                    System.Func<int, int> transform = value => Local(value);
                    return transform(input);
                }
            }
            """);
        var graph = await ProjectAnalyzer.AnalyzeProjectAsync(document.Project);
        Assert.NotNull(Assert.Single(graph.Nodes.Values, node => node.Symbol.Name == "Local").Analysis);
        Assert.NotNull(Assert.Single(graph.Nodes.Values, node => node.Symbol.Name == "Nested").Analysis);
        Assert.NotNull(Assert.Single(graph.Nodes.Values,
            node => node.Symbol.MethodKind == Microsoft.CodeAnalysis.MethodKind.AnonymousFunction).Analysis);
    }

    [Theory]
    [InlineData("samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj", "RtaNarrowingDemo.callgraph.verified.json")]
    [InlineData("samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj", "LambdaLocalFunctionDemo.callgraph.verified.json")]
    [InlineData("samples/GenericsDemo/GenericsDemo.csproj", "GenericsDemo.callgraph.verified.json")]
    [InlineData("samples/DelegateDemo/DelegateDemo.csproj", "DelegateDemo.callgraph.verified.json")]
    [InlineData("samples/ExtensionMethodDemo/ExtensionMethodDemo.csproj", "ExtensionMethodDemo.callgraph.verified.json")]
    [InlineData("samples/OperatorOverloadDemo/OperatorOverloadDemo.csproj", "OperatorOverloadDemo.callgraph.verified.json")]
    [InlineData("samples/FunctionPointerDemo/FunctionPointerDemo.csproj", "FunctionPointerDemo.callgraph.verified.json")]
    public async Task CallGraph_json_matches_snapshot(string sampleProjectRelativePath, string snapshotFileName)
    {
        MsBuildRegistration.EnsureRegistered();

        var projectPath = TestPaths.SampleProject(sampleProjectRelativePath);
        var snapshotPath = TestPaths.Snapshot(snapshotFileName);
        var receivedPath = snapshotPath.Replace(".verified.json", ".received.json");

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var actualJson = Normalize(await ProjectAnalyzer.AnalyzeProjectAsJsonAsync(project));

        if (!File.Exists(snapshotPath))
        {
            File.WriteAllText(receivedPath, actualJson);
            Assert.Fail($"Snapshot missing: {snapshotPath}\nWrote received output to: {receivedPath}");
        }

        var expectedJson = Normalize(await File.ReadAllTextAsync(snapshotPath));

        if (!string.Equals(expectedJson, actualJson, System.StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(receivedPath, actualJson);
        }
        else if (File.Exists(receivedPath))
        {
            File.Delete(receivedPath);
        }

        Assert.Equal(expectedJson, actualJson);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";
}

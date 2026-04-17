using System.IO;
using System.Threading.Tasks;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace AutoRepair.CFGAnalyzer.Tests;

public class CallGraphSnapshotTests
{
    [Theory]
    [InlineData("samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj", "RtaNarrowingDemo.callgraph.verified.json")]
    [InlineData("samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj", "LambdaLocalFunctionDemo.callgraph.verified.json")]
    [InlineData("samples/GenericsDemo/GenericsDemo.csproj", "GenericsDemo.callgraph.verified.json")]
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


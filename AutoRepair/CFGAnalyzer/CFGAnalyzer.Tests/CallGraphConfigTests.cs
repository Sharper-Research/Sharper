using System.Threading.Tasks;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace AutoRepair.CFGAnalyzer.Tests;

public class CallGraphConfigTests
{
    [Fact]
    public async Task ExcludeSystemLibraryTargetsExceptConsole_filters_system_targets_but_keeps_console()
    {
        MsBuildRegistration.EnsureRegistered();

        var projectPath = TestPaths.SampleProject("samples/SystemLibraryFilterDemo/SystemLibraryFilterDemo.csproj");
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);

        var json = await ProjectAnalyzer.AnalyzeProjectAsJsonAsync(
            project,
            config: new CallGraphConfig
            {
                ExcludeSystemLibraryTargetsExceptConsole = true
            });

        Assert.Contains("System.Console.WriteLine", json);
        Assert.DoesNotContain("System.String.Trim", json);
    }
}



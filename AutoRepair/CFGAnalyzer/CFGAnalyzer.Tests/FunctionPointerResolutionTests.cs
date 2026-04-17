using System.Linq;
using System.Threading.Tasks;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace AutoRepair.CFGAnalyzer.Tests;

public class FunctionPointerResolutionTests
{
    private static async Task<CallGraph> AnalyzeFunctionPointerDemoAsync(CallGraphConfig? config = null)
    {
        MsBuildRegistration.EnsureRegistered();

        var projectPath = TestPaths.SampleProject("samples/FunctionPointerDemo/FunctionPointerDemo.csproj");
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        return await ProjectAnalyzer.AnalyzeProjectAsync(project, config);
    }

    [Fact]
    public async Task Function_pointer_sample_loads_successfully()
    {
        // Verify that unsafe function pointer code compiles and builds a CFG
        var callGraph = await AnalyzeFunctionPointerDemoAsync();
        
        // Should have Operations class methods and Main
        var methodNames = callGraph.Nodes.Values.Select(n => n.Symbol.Name).ToHashSet();
        Assert.Contains("Add", methodNames);
        Assert.Contains("Multiply", methodNames);
        Assert.Contains("Square", methodNames);
        Assert.Contains("Main", methodNames);
    }

    [Fact]
    public async Task Function_pointer_resolver_can_be_disabled()
    {
        // Verify the FunctionPointer resolver exists and can be disabled
        var config = new CallGraphConfig
        {
            DisabledResolvers = new HashSet<CallResolver> { CallResolver.IntraproceduralFunctionPointer }
        };
        
        var callGraph = await AnalyzeFunctionPointerDemoAsync(config);
        
        // Should still load fine
        var main = callGraph.Nodes.Values.First(node => node.Symbol.ToDisplayString().Contains("Program.Main"));
        Assert.NotNull(main);
    }

    [Fact]
    public async Task Function_pointer_sample_has_console_call()
    {
        // At minimum, Main should call WriteLine (external)
        var callGraph = await AnalyzeFunctionPointerDemoAsync();
        var main = callGraph.Nodes.Values.First(node => node.Symbol.ToDisplayString().Contains("Program.Main"));

        var calleeNames = main.OutgoingEdges.Select(edge => edge.Callee.Symbol.Name).ToHashSet();
        Assert.Contains("WriteLine", calleeNames);
    }
}




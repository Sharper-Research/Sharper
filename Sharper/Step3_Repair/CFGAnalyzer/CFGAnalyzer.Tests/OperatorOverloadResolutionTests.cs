using System.Linq;
using System.Threading.Tasks;
using Sharper.CFGAnalyzer.Analysis;
using Sharper.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace Sharper.CFGAnalyzer.Tests;

public class OperatorOverloadResolutionTests
{
    private static async Task<CallGraph> AnalyzeOperatorDemoAsync(CallGraphConfig? config = null)
    {
        MsBuildRegistration.EnsureRegistered();

        var projectPath = TestPaths.SampleProject("samples/OperatorOverloadDemo/OperatorOverloadDemo.csproj");
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        return await ProjectAnalyzer.AnalyzeProjectAsync(project, config);
    }

    [Fact]
    public async Task Main_resolves_user_defined_operator_and_conversion_targets()
    {
        var callGraph = await AnalyzeOperatorDemoAsync();
        var main = callGraph.Nodes.Values.First(node => node.Symbol.ToDisplayString().Contains("Program.Main"));

        var calleeNames = main.OutgoingEdges.Select(edge => edge.Callee.Symbol.Name).ToHashSet();

        Assert.Contains("op_Addition", calleeNames);
        Assert.Contains("op_UnaryNegation", calleeNames);
        Assert.Contains("op_Equality", calleeNames);
        Assert.Contains("op_Inequality", calleeNames);
        Assert.Contains("op_Implicit", calleeNames);
        Assert.Contains("op_Explicit", calleeNames);
    }

    [Fact]
    public async Task Main_does_not_create_call_edges_for_builtin_numeric_conversions()
    {
        var callGraph = await AnalyzeOperatorDemoAsync();
        var main = callGraph.Nodes.Values.First(node => node.Symbol.ToDisplayString().Contains("Program.Main"));

        var calleeIds = main.OutgoingEdges.Select(edge => edge.Callee.Symbol.ToDisplayString()).ToArray();

        Assert.DoesNotContain(calleeIds, id => id.Contains("System.Double") || id.Contains("System.Int32"));
    }

    [Fact]
    public async Task Disabling_operator_resolver_removes_operator_edges_from_main()
    {
        var callGraph = await AnalyzeOperatorDemoAsync(new CallGraphConfig
        {
            DisabledResolvers = new HashSet<CallResolver> { CallResolver.OperatorOverload }
        });

        var main = callGraph.Nodes.Values.First(node => node.Symbol.ToDisplayString().Contains("Program.Main"));
        var calleeNames = main.OutgoingEdges.Select(edge => edge.Callee.Symbol.Name).ToHashSet();

        Assert.DoesNotContain("op_Addition", calleeNames);
        Assert.DoesNotContain("op_UnaryNegation", calleeNames);
        Assert.DoesNotContain("op_Equality", calleeNames);
        Assert.DoesNotContain("op_Inequality", calleeNames);
        Assert.DoesNotContain("op_Implicit", calleeNames);
        Assert.DoesNotContain("op_Explicit", calleeNames);
        Assert.Contains("WriteLine", calleeNames);
    }
}


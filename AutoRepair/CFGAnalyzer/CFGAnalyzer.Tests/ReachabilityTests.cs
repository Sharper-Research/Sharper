using System.Linq;
using System.Threading.Tasks;
using AutoRepair.CFGAnalyzer.Analysis;
using AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

namespace AutoRepair.CFGAnalyzer.Tests;

/// <summary>
///     Tests for <see cref="CallGraphTraverser.GetReachableCallNodes"/> with both
///     <see cref="ReachabilityDirection.Forward"/> and <see cref="ReachabilityDirection.Backward"/>.
///     Uses the GenericsDemo sample, whose call structure is:
///
///     Main ──► PrintValue&lt;T&gt; ──► Consume&lt;T&gt;
///     Main ──► StringBoxWorkflow..ctor
///     Main ──► StringBoxWorkflow.Run ──► Box&lt;T&gt;..ctor
///                                    ──► Box&lt;T&gt;.Update ──► Box&lt;T&gt;.Log
///                                    ──► MirrorIntoLocalBox ──► Box&lt;T&gt;..ctor
///                                                            ──► Box&lt;T&gt;.Update ──► Box&lt;T&gt;.Log
/// </summary>
public class ReachabilityTests
{
    // ── helpers ────────────────────────────────────────────────────────────

    private static async Task<CallGraph> LoadGenericsCallGraphAsync()
    {
        MsBuildRegistration.EnsureRegistered();

        var projectPath = TestPaths.SampleProject("samples/GenericsDemo/GenericsDemo.csproj");
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        return await ProjectAnalyzer.AnalyzeProjectAsync(project);
    }

    private static CallGraphNode FindNode(CallGraph graph, string methodNameSubstring) =>
        graph.Nodes.Values.First(n => n.Symbol.ToDisplayString().Contains(methodNameSubstring));

    // ── Backward reachability ───────────────────────────────────────────────

    [Fact]
    public async Task Backward_from_leaf_Consume_reaches_PrintValue_and_Main()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var consume = FindNode(callGraph, "Consume");

        var reachable = CallGraphTraverser.GetReachableCallNodes(consume, ReachabilityDirection.Backward);
        var names = reachable.Select(n => n.Symbol.Name).ToHashSet();

        Assert.Contains("PrintValue", names);
        Assert.Contains("Main", names);
    }

    [Fact]
    public async Task Backward_from_leaf_Consume_does_not_include_start_node()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var consume = FindNode(callGraph, "Consume");

        var reachable = CallGraphTraverser.GetReachableCallNodes(consume, ReachabilityDirection.Backward);

        Assert.DoesNotContain(reachable, n => n.Symbol.Name == "Consume");
    }

    [Fact]
    public async Task Backward_from_Log_reaches_Update_Run_and_Main()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var log = FindNode(callGraph, "Box<T>.Log");

        var reachable = CallGraphTraverser.GetReachableCallNodes(log, ReachabilityDirection.Backward);
        var names = reachable.Select(n => n.Symbol.Name).ToHashSet();

        Assert.Contains("Update", names);
        Assert.Contains("Run", names);
        Assert.Contains("Main", names);
    }

    [Fact]
    public async Task Backward_from_Log_does_not_include_sibling_Consume()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var log = FindNode(callGraph, "Box<T>.Log");

        var reachable = CallGraphTraverser.GetReachableCallNodes(log, ReachabilityDirection.Backward);

        Assert.DoesNotContain(reachable, n => n.Symbol.Name == "Consume");
    }

    [Fact]
    public async Task Backward_from_Main_returns_empty_because_nothing_calls_Main()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var main = FindNode(callGraph, "Program.Main");

        var reachable = CallGraphTraverser.GetReachableCallNodes(main, ReachabilityDirection.Backward);

        Assert.Empty(reachable);
    }

    // ── Forward reachability (baseline / contrast) ──────────────────────────

    [Fact]
    public async Task Forward_from_Main_reaches_PrintValue_Consume_Run_Update_Log()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var main = FindNode(callGraph, "Program.Main");

        var reachable = CallGraphTraverser.GetReachableCallNodes(main, ReachabilityDirection.Forward);
        var names = reachable.Select(n => n.Symbol.Name).ToHashSet();

        Assert.Contains("PrintValue", names);
        Assert.Contains("Consume", names);
        Assert.Contains("Run", names);
        Assert.Contains("Update", names);
        Assert.Contains("Log", names);
    }

    [Fact]
    public async Task Forward_from_leaf_Consume_returns_empty()
    {
        var callGraph = await LoadGenericsCallGraphAsync();
        var consume = FindNode(callGraph, "Consume");

        // Consume calls only external targets (Console.WriteLine etc.), so no internal forward nodes
        var reachable = CallGraphTraverser.GetReachableCallNodes(consume, ReachabilityDirection.Forward);
        var internalNames = reachable.Select(n => n.Symbol.Name).ToArray();

        Assert.DoesNotContain("PrintValue", internalNames);
        Assert.DoesNotContain("Main", internalNames);
    }

    // ── Null-guard ──────────────────────────────────────────────────────────

    [Fact]
    public void GetReachableCallNodes_throws_on_null_node()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CallGraphTraverser.GetReachableCallNodes(null!, ReachabilityDirection.Backward));
    }
}


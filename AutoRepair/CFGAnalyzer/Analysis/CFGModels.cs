using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

/// <summary>
/// Represents a specific site within a CFG where a call occurs.
/// </summary>
public record CallSite(
    int BlockOrdinal,
    IMethodSymbol Target,
    bool IsInternal,
    int? CallerLineNumber = null,
    int? CalleeLineNumber = null
);

/// <summary>
/// Contains the dataflow analysis results for a single method.
/// </summary>
public record DataFlowAnalysisResult(
    List<List<string>> PathConstraints,
    DominatorTree? DominatorTree = null
);

/// <summary>
/// Contains the local analysis results for a single method.
/// </summary>
public class MethodAnalysisResult
{
    public ControlFlowGraph Cfg { get; }
    public List<CallSite> CallSites { get; }
    public DataFlowAnalysisResult? DataFlow { get; set; }

    public MethodAnalysisResult(ControlFlowGraph cfg, List<CallSite> callSites)
    {
        Cfg = cfg;
        CallSites = callSites;
    }

    // Compatibility for existing code
    public List<List<string>> PathConstraints => DataFlow?.PathConstraints ?? new();
    public DominatorTree? DominatorTree => DataFlow?.DominatorTree;
}

/// <summary>
/// Represents a node in the Dominator Tree.
/// </summary>
public class DominatorTreeNode
{
    public int BlockOrdinal { get; }
    public DominatorTreeNode? ImmediateDominator { get; internal set; }
    public List<DominatorTreeNode> Children { get; } = new();

    public DominatorTreeNode(int ordinal) => BlockOrdinal = ordinal;
}

/// <summary>
/// Represents the Dominator Tree for a CFG.
/// </summary>
public class DominatorTree
{
    public DominatorTreeNode Root { get; }
    public Dictionary<int, DominatorTreeNode> Nodes { get; } = new();

    public DominatorTree(int rootOrdinal)
    {
        Root = new DominatorTreeNode(rootOrdinal);
        Nodes[rootOrdinal] = Root;
    }

    public DominatorTreeNode GetOrAddNode(int ordinal)
    {
        if (!Nodes.TryGetValue(ordinal, out var node))
        {
            node = new DominatorTreeNode(ordinal);
            Nodes[ordinal] = node;
        }
        return node;
    }
}

/// <summary>
/// Represents a node in the Call Graph, corresponding to a specific method symbol.
/// </summary>
public class CallGraphNode
{
    public IMethodSymbol Symbol { get; }

    /// <summary>
    /// The CFG analysis results for this method, if available (internal methods).
    /// </summary>
    public MethodAnalysisResult? Analysis { get; set; }

    public List<CallGraphEdge> OutgoingEdges { get; } = new();
    public List<CallGraphEdge> IncomingEdges { get; } = new();

    public CallGraphNode(IMethodSymbol symbol)
    {
        Symbol = symbol;
    }

    public override string ToString() => Symbol.ToDisplayString();
}

/// <summary>
/// Represents a directed edge in the Call Graph (a call from one method to another).
/// </summary>
public class CallGraphEdge
{
    public CallGraphNode Caller { get; }
    public CallGraphNode Callee { get; }

    /// <summary>
    /// The specific site within the caller's CFG where the call occurs.
    /// </summary>
    public CallSite CallSite { get; }

    public CallGraphEdge(CallGraphNode caller, CallGraphNode callee, CallSite callSite)
    {
        Caller = caller;
        Callee = callee;
        CallSite = callSite;
    }
}

/// <summary>
/// A collection of nodes and edges representing the interprocedural call structure.
/// </summary>
public class CallGraph
{
    public Dictionary<IMethodSymbol, CallGraphNode> Nodes { get; } = new(SymbolEqualityComparer.Default);
    public List<CallGraphEdge> Edges { get; } = new();

    public CallGraphNode GetOrAddNode(IMethodSymbol symbol)
    {
        if (!Nodes.TryGetValue(symbol, out var node))
        {
            node = new CallGraphNode(symbol);
            Nodes[symbol] = node;
        }
        return node;
    }

    public CallGraphEdge AddEdge(IMethodSymbol callerSymbol, IMethodSymbol calleeSymbol, CallSite site)
    {
        var callerNode = GetOrAddNode(callerSymbol);
        var calleeNode = GetOrAddNode(calleeSymbol);

        var edge = new CallGraphEdge(callerNode, calleeNode, site);
        callerNode.OutgoingEdges.Add(edge);
        calleeNode.IncomingEdges.Add(edge);
        Edges.Add(edge);

        return edge;
    }
}

using Microsoft.CodeAnalysis;

namespace Sharper.CFGAnalyzer.Analysis;

public enum ReachabilityDirection
{
    Forward,
    Backward
}

public static class CallGraphTraverser
{
    public static IReadOnlyList<CallGraphNode> GetReachableCallNodes(
        CallGraphNode methodNode,
        ReachabilityDirection direction)
    {
        if (methodNode == null) throw new ArgumentNullException(nameof(methodNode));

        var reachable = new List<CallGraphNode>();
        var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default) { methodNode.Symbol };
        var pending = new Queue<CallGraphNode>();

        foreach (var adjacent in GetAdjacentNodes(methodNode, direction)) pending.Enqueue(adjacent);

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current.Symbol)) continue;

            reachable.Add(current);

            foreach (var adjacent in GetAdjacentNodes(current, direction)) pending.Enqueue(adjacent);
        }

        return reachable;
    }

    private static IEnumerable<CallGraphNode> GetAdjacentNodes(CallGraphNode node, ReachabilityDirection direction)
    {
        return direction == ReachabilityDirection.Forward
            ? node.OutgoingEdges.Select(edge => edge.Callee)
            : node.IncomingEdges.Select(edge => edge.Caller);
    }

    public static void TraverseAndGeneratePathConditions(
        CallGraphNode startNode,
        List<string> initialPathConstraints,
        HashSet<IMethodSymbol> visitedNodes,
        TextWriter? output = null)
    {
        output ??= Console.Out;

        if (!visitedNodes.Add(startNode.Symbol))
            return;

        output.WriteLine($"Traversing: {startNode.Symbol.ToDisplayString()} ---");

        if (startNode.Analysis?.Cfg == null)
        {
            output.WriteLine("  (No CFG or Call Sites found for this node)");
            return;
        }

        // 1. Print Path Constraints
        output.WriteLine("  Path Constraints:");
        if (startNode.Analysis.DataFlow?.PathConstraints is { Count: > 0 } constraints)
            foreach (var path in constraints)
                output.WriteLine($"    - {string.Join(" && ", path)}");
        else
            output.WriteLine("    (No path constraints found)");

        // 2. Print Call Sites
        output.WriteLine("  Call Sites:");
        if (startNode.Analysis.CallSites.Any())
            foreach (var site in startNode.Analysis.CallSites)
                output.WriteLine($"    - {site.Target.ToDisplayString()} (Block: {site.BlockOrdinal})");
        else
            output.WriteLine("    (No call sites found)");

        // 3. Recursively traverse children (callees) — reuse stored dominator sets
        var domSets = startNode.Analysis.DataFlow?.DomSets
                      ?? DominatorSets.Empty;

        foreach (var edge in startNode.OutgoingEdges)
        {
            if (!edge.CallSite.IsInternal) continue;

            var callSitePaths = domSets.Count == 0
                ? new List<IReadOnlyList<string>> { new List<string>() }
                : (IEnumerable<IReadOnlyList<string>>)FlowAnalysis.GetPathsToBlockAcyclic(
                    startNode.Analysis.Cfg, edge.CallSite.BlockOrdinal, domSets);

            var callSitePathsList = callSitePaths.ToList();
            if (callSitePathsList.Count == 0) callSitePathsList.Add(new List<string>());

            foreach (var callSitePath in callSitePathsList)
            {
                var nextPrefixContext = new List<string>(initialPathConstraints);
                nextPrefixContext.AddRange(callSitePath);

                TraverseAndGeneratePathConditions(
                    edge.Callee,
                    nextPrefixContext,
                    new HashSet<IMethodSymbol>(visitedNodes, SymbolEqualityComparer.Default),
                    output);
            }
        }
    }
}
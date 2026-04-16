using Microsoft.CodeAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public static class CallGraphTraverser
{
    public static void TraverseAndGeneratePathConditions(
        CallGraphNode startNode, 
        List<string> initialPathConstraints, 
        HashSet<IMethodSymbol> visitedNodes)
    {
        if (startNode == null || !visitedNodes.Add(startNode.Symbol))
            return;

        Console.WriteLine($"Traversing: {startNode.Symbol.ToDisplayString()} ---");

        if (startNode.Analysis?.Cfg == null || startNode.Analysis.CallSites == null)
        {
            Console.WriteLine("  (No CFG or Call Sites found for this node)");
            return;
        }

        // 1. Print Path Constraints
        Console.WriteLine($"  Path Constraints:");
        if (startNode.Analysis.DataFlow?.PathConstraints != null && startNode.Analysis.DataFlow.PathConstraints.Any())
        {
            foreach (var path in startNode.Analysis.DataFlow.PathConstraints)
            {
                Console.WriteLine($"    - {string.Join(" && ", path)}");
            }
        }
        else
        {
            Console.WriteLine("    (No path constraints found)");
        }

        // 2. Print Call Sites
        Console.WriteLine($"  Call Sites:");
        if (startNode.Analysis.CallSites.Any())
        {
            foreach (var site in startNode.Analysis.CallSites)
            {
                Console.WriteLine($"    - {site.Target.ToDisplayString()} (Block: {site.BlockOrdinal})");
            }
        }
        else
        {
            Console.WriteLine("    (No call sites found)");
        }

        // 3. Recursively traverse children (callees)
        var dominator = new Dominator();
        var domSets = startNode.Analysis.Cfg.Blocks.Length > 0
            ? dominator.ComputeDominatorSets(startNode.Analysis.Cfg)
            : new Dictionary<int, HashSet<int>>();

        foreach (var edge in startNode.OutgoingEdges)
        {
            if (!edge.CallSite.IsInternal)
            {
                continue;
            }

            var callSitePaths = domSets.Count == 0
                ? new List<List<string>> { new() }
                : FlowAnalysis.GetPathsToBlockAcyclic(startNode.Analysis.Cfg, edge.CallSite.BlockOrdinal, domSets);

            if (callSitePaths.Count == 0)
            {
                callSitePaths.Add(new List<string>());
            }

            foreach (var callSitePath in callSitePaths)
            {
                var nextPrefixContext = new List<string>(initialPathConstraints);
                nextPrefixContext.AddRange(callSitePath);

                TraverseAndGeneratePathConditions(
                    edge.Callee,
                    nextPrefixContext,
                    new HashSet<IMethodSymbol>(visitedNodes, SymbolEqualityComparer.Default));
            }
        }
    }
}

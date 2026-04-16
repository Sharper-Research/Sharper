using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations; // Add this using directive
using Microsoft.CodeAnalysis.CSharp.Syntax; // Add this using directive
using AutoRepair.CFGAnalyzer.Models;
using AutoRepair.CFGAnalyzer.Analysis;
using CFGAnalyzer.Analysis;

namespace AutoRepair.CFGAnalyzer.Traversal;

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
        var domSets = startNode.Analysis.DataFlow?.DominatorTree != null 
            ? FlowAnalysis.ComputeDominatorSets(startNode.Analysis.Cfg) // Recomputing, but this is for example
            : new Dictionary<int, HashSet<int>>(); // Fallback if no domSets

        foreach (var site in startNode.Analysis.CallSites)
        {
            if (site.IsInternal) // Only traverse internal calls for now
            {
                // Need to find the actual CallGraphNode for the target symbol
                // This assumes the CallGraph is fully built and nodes are available.
                // In a real scenario, you'd pass the CallGraph or a lookup mechanism.
                // For simplicity here, we'll assume we can find it or simulate it.
                
                // Placeholder: In a real implementation, you'd get the CalleeNode from the CallGraph
                // For demonstration, let's just print a message indicating recursion.
                Console.WriteLine($"    (Recursively traversing call to: {site.Target.ToDisplayString()})");
                
                // To actually recurse, you'd need the full CallGraph object here.
                // Example: TraverseAndGeneratePathConditions(callGraph.Nodes[site.Target], newPathConstraints, visitedNodes);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using AutoRepair.CFGAnalyzer.Models;
using AutoRepair.CFGAnalyzer.Flow;

namespace AutoRepair.CFGAnalyzer.Traversal;

public static class CallGraphTraverser
{
    public static void TraverseAndGeneratePathConditions(
        CallGraphNode node, 
        List<string> prefixContext,
        HashSet<IMethodSymbol> visited)
    {
        if (visited.Contains(node.Symbol)) return;
        visited.Add(node.Symbol);

        if (node.Analysis == null) return;

        string currentContext = prefixContext.Count > 0 ? string.Join(" && ", prefixContext) : "true";
        Console.WriteLine($"\n[Method] {node.Symbol.ToDisplayString()}");
        Console.WriteLine($"  Inherited Context: {currentContext}");

        foreach (var path in node.Analysis.PathConstraints)
        {
            string fullPath = path.Count > 0 ? string.Join(" && ", path) : "true";
            Console.WriteLine($"  Local Path: {fullPath}");
        }

        foreach (var edge in node.OutgoingEdges)
        {
            if (edge.CallSite.IsInternal)
            {
                var pathsToCall = Flow.FlowAnalysis.GetPathsToBlockAcyclic(node.Analysis.Cfg, edge.CallSite.BlockOrdinal);
                foreach (var pathToCall in pathsToCall)
                {
                    var newContext = new List<string>(prefixContext);
                    newContext.AddRange(pathToCall);
                    
                    TraverseAndGeneratePathConditions(edge.Callee, newContext, new HashSet<IMethodSymbol>(visited, SymbolEqualityComparer.Default));
                }
            }
            else
            {
                Console.WriteLine($"  External Call: {edge.Callee.Symbol.ToDisplayString()} from Block {edge.CallSite.BlockOrdinal}");
            }
        }
    }
}

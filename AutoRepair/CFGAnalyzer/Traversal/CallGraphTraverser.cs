using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using AutoRepair.CFGAnalyzer.Models;
using AutoRepair.CFGAnalyzer.Flow;

namespace AutoRepair.CFGAnalyzer.Traversal;

public static class CallGraphTraverser
{
    public static void TraverseAndGeneratePathConditions(
        IMethodSymbol entrySymbol, 
        Dictionary<IMethodSymbol, MethodAnalysisResult> results, 
        List<string> prefixContext,
        HashSet<IMethodSymbol> visited)
    {
        if (visited.Contains(entrySymbol)) return;
        visited.Add(entrySymbol);

        if (!results.TryGetValue(entrySymbol, out var result)) return;

        string currentContext = prefixContext.Count > 0 ? string.Join(" && ", prefixContext) : "true";
        Console.WriteLine($"\n[Method] {entrySymbol.ToDisplayString()}");
        Console.WriteLine($"  Inherited Context: {currentContext}");

        foreach (var path in result.PathConstraints)
        {
            string fullPath = path.Count > 0 ? string.Join(" && ", path) : "true";
            Console.WriteLine($"  Local Path: {fullPath}");
        }

        foreach (var call in result.CallSites)
        {
            if (call.IsInternal)
            {
                var pathsToCall = Flow.FlowAnalysis.GetPathsToBlockAcyclic(result.Cfg, call.BlockOrdinal);
                foreach (var pathToCall in pathsToCall)
                {
                    var newContext = new List<string>(prefixContext);
                    newContext.AddRange(pathToCall);
                    
                    TraverseAndGeneratePathConditions(call.Target, results, newContext, new HashSet<IMethodSymbol>(visited, SymbolEqualityComparer.Default));
                }
            }
            else
            {
                Console.WriteLine($"  External Call: {call.Target.ToDisplayString()} from Block {call.BlockOrdinal}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoRepair.CFGAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public class CallGraphBuilder
{
    private readonly List<ICallResolver> _resolvers;
    private readonly CallGraphConfig _config;

    public CallGraphBuilder(IEnumerable<ICallResolver> resolvers, CallGraphConfig? config = null)
    {
        _resolvers = resolvers.OrderByDescending(r => r.Priority).ToList();
        _config = config ?? new CallGraphConfig();
    }

    public void BuildEdges(CallGraph callGraph, Dictionary<IMethodSymbol, MethodAnalysisResult> results)
    {
        foreach (var kvp in results)
        {
            var symbol = kvp.Key;
            var analysis = kvp.Value;
            var node = callGraph.GetOrAddNode(symbol);

            foreach (var site in analysis.CallSites)
            {
                callGraph.AddEdge(symbol, site.Target, site);
            }
        }
    }

    public async Task<List<CallSite>> DiscoverCallSitesAsync(
        ControlFlowGraph cfg,
        Dictionary<IMethodSymbol, ControlFlowGraph> internalCfgs,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var callSites = new List<CallSite>();
        foreach (var block in cfg.Blocks)
        {
            foreach (var op in block.Operations)
            {
                foreach (var descendant in op.DescendantsAndSelf())
                {
                    var targets = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
                    foreach (var resolver in _resolvers)
                    {
                        var resolvedTargets = await resolver.ResolveAsync(descendant, project, cancellationToken);
                        foreach (var resolved in resolvedTargets)
                        {
                            targets.Add(resolved);
                        }

                        if (targets.Count > 0)
                        {
                            break;
                        }
                    }

                    foreach (var target in targets)
                    {
                        if (ShouldSkipTarget(target, internalCfgs))
                        {
                            continue;
                        }

                        var site = new CallSite(block.Ordinal, target, internalCfgs.ContainsKey(target));
                        callSites.Add(site);
                    }
                }
            }
        }
        return callSites;
    }

    private bool ShouldSkipTarget(IMethodSymbol target, Dictionary<IMethodSymbol, ControlFlowGraph> internalCfgs)
    {
        if (!_config.ExcludeSystemLibraryTargetsExceptConsole)
        {
            return false;
        }

        if (internalCfgs.ContainsKey(target))
        {
            return false;
        }

        return IsSystemLibraryTarget(target) && !IsConsoleTarget(target);
    }

    private static bool IsSystemLibraryTarget(IMethodSymbol target)
    {
        var ns = target.ContainingNamespace?.ToDisplayString();
        if (!string.IsNullOrEmpty(ns) && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal)))
        {
            return true;
        }

        var assemblyName = target.ContainingAssembly?.Name;
        return !string.IsNullOrEmpty(assemblyName) &&
               (assemblyName == "System.Runtime" || assemblyName.StartsWith("System.", StringComparison.Ordinal));
    }

    private static bool IsConsoleTarget(IMethodSymbol target)
    {
        return target.ContainingType?.ToDisplayString() == "System.Console";
    }
}

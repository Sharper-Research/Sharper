using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using AutoRepair.CFGAnalyzer.Analysis.CallResolvers;

namespace AutoRepair.CFGAnalyzer.Analysis;

public class CallGraphBuilder
{
    private readonly CallGraphConfig _config;
    private readonly List<ICallResolver> _resolvers;

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

            foreach (var site in analysis.CallSites) callGraph.AddEdge(symbol, site.Target, site);
        }
    }

    public async Task<List<CallSite>> DiscoverCallSitesAsync(
        ControlFlowGraph cfg,
        Dictionary<IMethodSymbol, ControlFlowGraph> internalCfgs,
        Project project,
        CancellationToken cancellationToken = default)
    {
        // Notify CFG-aware resolvers of the current method's graph so they can
        // pre-compute per-CFG state (e.g. intraprocedural delegate assignments).
        foreach (var resolver in _resolvers)
            if (resolver is ICfgAwareCallResolver cfgAware)
                cfgAware.SetCfg(cfg);

        var callSites = new List<CallSite>();
        var targets = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var block in cfg.Blocks)
        foreach (var descendant in block.Operations.SelectMany(op => op.DescendantsAndSelf()))
        {
            targets.Clear();
            foreach (var resolver in _resolvers)
            {
                var resolvedTargets = await resolver.ResolveAsync(descendant, project, cancellationToken);
                foreach (var resolved in resolvedTargets) targets.Add(resolved);

                if (targets.Count > 0) break;
            }

            callSites.AddRange(from target in targets
                let internalTarget = ResolveInternalTarget(target,
                    internalCfgs)
                let effectiveTarget = internalTarget ?? target
                where !ShouldSkipTarget(effectiveTarget,
                    internalCfgs)
                let callerLineNumber = GetCallerLineNumber(descendant)
                let calleeLineNumber = GetCalleeLineNumber(effectiveTarget)
                select new CallSite(block.Ordinal,
                    effectiveTarget,
                    internalTarget != null,
                    callerLineNumber,
                    calleeLineNumber));
        }

        return callSites;
    }

    private static IMethodSymbol? ResolveInternalTarget(
        IMethodSymbol target,
        Dictionary<IMethodSymbol, ControlFlowGraph> internalCfgs)
    {
        if (internalCfgs.ContainsKey(target)) return target;

        var originalDefinition = target.OriginalDefinition;
        if (internalCfgs.ContainsKey(originalDefinition)) return originalDefinition;

        var constructedFrom = target.ConstructedFrom;
        if (constructedFrom != null && internalCfgs.ContainsKey(constructedFrom)) return constructedFrom;

        return null;
    }

    private static int? GetCallerLineNumber(IOperation operation)
    {
        var span = operation.Syntax.GetLocation().GetLineSpan();
        if (!span.IsValid) return null;

        return span.StartLinePosition.Line + 1;
    }

    private static int? GetCalleeLineNumber(IMethodSymbol target)
    {
        var sourceLocation = target.Locations.FirstOrDefault(location => location.IsInSource);
        if (sourceLocation == null) return null;

        var span = sourceLocation.GetLineSpan();
        if (!span.IsValid) return null;

        return span.StartLinePosition.Line + 1;
    }

    private bool ShouldSkipTarget(IMethodSymbol target, Dictionary<IMethodSymbol, ControlFlowGraph> internalCfgs)
    {
        if (!_config.ExcludeSystemLibraryTargetsExceptConsole) return false;

        if (internalCfgs.ContainsKey(target)) return false;

        return IsSystemLibraryTarget(target) && !IsConsoleTarget(target);
    }

    private static bool IsSystemLibraryTarget(IMethodSymbol target)
    {
        var ns = target.ContainingNamespace?.ToDisplayString();
        if (!string.IsNullOrEmpty(ns) &&
            (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal))) return true;

        var assemblyName = target.ContainingAssembly?.Name;
        return !string.IsNullOrEmpty(assemblyName) &&
               (assemblyName == "System.Runtime" || assemblyName.StartsWith("System.", StringComparison.Ordinal));
    }

    private static bool IsConsoleTarget(IMethodSymbol target)
    {
        return target.ContainingType?.ToDisplayString() == "System.Console";
    }
}
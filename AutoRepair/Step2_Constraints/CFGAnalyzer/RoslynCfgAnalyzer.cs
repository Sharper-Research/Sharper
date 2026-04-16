using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace CFGAnalyzer;

public record CallSite(
    int BlockOrdinal,
    IMethodSymbol Target,
    bool IsInternal
);

public record MethodAnalysisResult(
    ControlFlowGraph Cfg,
    List<List<string>> PathConstraints,
    List<CallSite> CallSites
);

public static class RoslynCfgAnalyzer
{
    public static async Task<Dictionary<IMethodSymbol, MethodAnalysisResult>> AnalyzeProjectAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);

        var results = new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);
        var methodCfgs = new Dictionary<IMethodSymbol, ControlFlowGraph>(SymbolEqualityComparer.Default);

        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            var members = root.DescendantNodes().Where(n => n is MethodDeclarationSyntax or ConstructorDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax);

            foreach (var member in members)
            {
                var operation = semanticModel.GetOperation(member);
                IMethodSymbol? symbol = semanticModel.GetDeclaredSymbol(member) as IMethodSymbol;
                
                ControlFlowGraph? cfg = null;
                if (operation is IBlockOperation blockOp) cfg = ControlFlowGraph.Create(blockOp);
                else if (operation is IMethodBodyOperation mb) cfg = ControlFlowGraph.Create(mb);
                else if (operation is IConstructorBodyOperation cb) cfg = ControlFlowGraph.Create(cb);
                else if (operation is ILocalFunctionOperation local && local.Body != null) cfg = ControlFlowGraph.Create(local.Body);

                if (cfg != null && symbol != null)
                {
                    methodCfgs[symbol] = cfg;
                }
            }
        }

        foreach (var kvp in methodCfgs)
        {
            var symbol = kvp.Key;
            var cfg = kvp.Value;

            var callSites = new List<CallSite>();
            foreach (var block in cfg.Blocks)
            {
                foreach (var op in block.Operations)
                {
                    foreach (var descendant in op.DescendantsAndSelf())
                    {
                        IMethodSymbol? target = descendant switch
                        {
                            IInvocationOperation inv => inv.TargetMethod,
                            IObjectCreationOperation oc => oc.Constructor,
                            IPropertyReferenceOperation pr => pr.Property.GetMethod ?? pr.Property.SetMethod,
                            _ => null
                        };

                        if (target != null)
                        {
                            callSites.Add(new CallSite(block.Ordinal, target, methodCfgs.ContainsKey(target)));
                        }
                    }
                }
            }

            var pathConstraints = GetPathConstraintsAcyclic(cfg);
            results[symbol] = new MethodAnalysisResult(cfg, pathConstraints, callSites);
        }

        return results;
    }

    public static Dictionary<int, HashSet<int>> ComputeDominatorSets(ControlFlowGraph cfg)
    {
        int n = cfg.Blocks.Length;
        var dom = new HashSet<int>[n];
        var allNodes = Enumerable.Range(0, n).ToHashSet();
        
        for (int i = 0; i < n; i++) dom[i] = new HashSet<int>(allNodes);
        dom[0] = new HashSet<int> { 0 };

        var preds = new List<int>[n];
        for (int i = 0; i < n; i++) preds[i] = new List<int>();
        foreach (var block in cfg.Blocks)
        {
            if (block.ConditionalSuccessor != null && block.ConditionalSuccessor.Destination != null)
                preds[block.ConditionalSuccessor.Destination.Ordinal].Add(block.Ordinal);
            if (block.FallThroughSuccessor != null && block.FallThroughSuccessor.Destination != null)
                preds[block.FallThroughSuccessor.Destination.Ordinal].Add(block.Ordinal);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 1; i < n; i++)
            {
                var newDom = new HashSet<int>(allNodes);
                if (preds[i].Count > 0)
                {
                    bool first = true;
                    foreach (int p in preds[i])
                    {
                        if (first) { newDom = new HashSet<int>(dom[p]); first = false; }
                        else newDom.IntersectWith(dom[p]);
                    }
                }
                else newDom = new HashSet<int>();
                
                newDom.Add(i);

                if (!dom[i].SetEquals(newDom))
                {
                    dom[i] = newDom;
                    changed = true;
                }
            }
        }
        
        var result = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < n; i++) result[i] = dom[i];
        return result;
    }

    public static List<List<string>> GetPathConstraintsAcyclic(ControlFlowGraph cfg)
    {
        var domSets = ComputeDominatorSets(cfg);
        var allPaths = new List<List<string>>();
        if (cfg.Blocks.Length == 0) return allPaths;

        ExplorePathAcyclic(cfg.Blocks[0], new List<string>(), domSets, allPaths);
        return allPaths;
    }

    private static void ExplorePathAcyclic(BasicBlock block, List<string> currentPath, Dictionary<int, HashSet<int>> domSets, List<List<string>> allPaths)
    {
        bool hasForwardSuccessor = false;

        if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null && block.BranchValue != null)
        {
            string condition = block.BranchValue.Syntax.ToString().Replace("\n", " ").Trim();
            
            var destA = block.ConditionalSuccessor.Destination;
            var destB = block.FallThroughSuccessor.Destination;

            bool isBackA = destA != null && domSets[block.Ordinal].Contains(destA.Ordinal);
            bool isBackB = destB != null && domSets[block.Ordinal].Contains(destB.Ordinal);

            if (!isBackA && destA != null)
            {
                var pathA = new List<string>(currentPath);
                pathA.Add(block.ConditionKind == ControlFlowConditionKind.WhenTrue ? condition : $"!({condition})");
                ExplorePathAcyclic(destA, pathA, domSets, allPaths);
                hasForwardSuccessor = true;
            }

            if (!isBackB && destB != null)
            {
                var pathB = new List<string>(currentPath);
                pathB.Add(block.ConditionKind == ControlFlowConditionKind.WhenTrue ? $"!({condition})" : condition);
                ExplorePathAcyclic(destB, pathB, domSets, allPaths);
                hasForwardSuccessor = true;
            }
        }
        else if (block.FallThroughSuccessor != null && block.FallThroughSuccessor.Destination != null)
        {
            var dest = block.FallThroughSuccessor.Destination;
            if (!domSets[block.Ordinal].Contains(dest.Ordinal))
            {
                ExplorePathAcyclic(dest, currentPath, domSets, allPaths);
                hasForwardSuccessor = true;
            }
        }

        if (!hasForwardSuccessor)
        {
            allPaths.Add(new List<string>(currentPath));
        }
    }

    public static List<List<string>> GetPathsToBlockAcyclic(ControlFlowGraph cfg, int targetBlockOrdinal)
    {
        var domSets = ComputeDominatorSets(cfg);
        var allPaths = new List<List<string>>();
        if (cfg.Blocks.Length == 0) return allPaths;

        ExplorePathToBlockAcyclic(cfg.Blocks[0], targetBlockOrdinal, new List<string>(), domSets, allPaths);
        return allPaths;
    }

    private static void ExplorePathToBlockAcyclic(BasicBlock block, int targetOrdinal, List<string> currentPath, Dictionary<int, HashSet<int>> domSets, List<List<string>> allPaths)
    {
        if (block.Ordinal == targetOrdinal)
        {
            allPaths.Add(new List<string>(currentPath));
            return;
        }

        if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null && block.BranchValue != null)
        {
            string condition = block.BranchValue.Syntax.ToString().Replace("\n", " ").Trim();
            var destA = block.ConditionalSuccessor.Destination;
            var destB = block.FallThroughSuccessor.Destination;

            bool isBackA = destA != null && domSets[block.Ordinal].Contains(destA.Ordinal);
            bool isBackB = destB != null && domSets[block.Ordinal].Contains(destB.Ordinal);

            if (!isBackA && destA != null)
            {
                var pathA = new List<string>(currentPath);
                pathA.Add(block.ConditionKind == ControlFlowConditionKind.WhenTrue ? condition : $"!({condition})");
                ExplorePathToBlockAcyclic(destA, targetOrdinal, pathA, domSets, allPaths);
            }

            if (!isBackB && destB != null)
            {
                var pathB = new List<string>(currentPath);
                pathB.Add(block.ConditionKind == ControlFlowConditionKind.WhenTrue ? $"!({condition})" : condition);
                ExplorePathToBlockAcyclic(destB, targetOrdinal, pathB, domSets, allPaths);
            }
        }
        else if (block.FallThroughSuccessor != null && block.FallThroughSuccessor.Destination != null)
        {
            var dest = block.FallThroughSuccessor.Destination;
            if (!domSets[block.Ordinal].Contains(dest.Ordinal))
                ExplorePathToBlockAcyclic(dest, targetOrdinal, currentPath, domSets, allPaths);
        }
    }

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
                var pathsToCall = GetPathsToBlockAcyclic(result.Cfg, call.BlockOrdinal);
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

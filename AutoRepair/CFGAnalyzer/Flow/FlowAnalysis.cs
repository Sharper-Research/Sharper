using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis;

using AutoRepair.CFGAnalyzer.Models;

namespace AutoRepair.CFGAnalyzer.Flow;

public static class FlowAnalysis
{
    public static DominatorTree BuildDominatorTree(ControlFlowGraph cfg)
    {
        var domSets = ComputeDominatorSets(cfg);
        var tree = new DominatorTree(0);

        // For each node n (except root), find its immediate dominator
        // IDom(n) = d such that d dominates n, d != n, and d is dominated by all other dominators of n
        foreach (var kvp in domSets)
        {
            int n = kvp.Key;
            if (n == 0) continue;

            var dominators = kvp.Value;
            int iDom = -1;

            foreach (int d in dominators)
            {
                if (d == n) continue;

                // Check if d is dominated by all other nodes in Dom(n) - {n}
                bool isIDom = true;
                foreach (int otherD in dominators)
                {
                    if (otherD == n || otherD == d) continue;
                    
                    // if otherD does not dominate d, then d cannot be the IDom
                    if (!domSets[d].Contains(otherD))
                    {
                        isIDom = false;
                        break;
                    }
                }

                if (isIDom)
                {
                    iDom = d;
                    break;
                }
            }

            if (iDom != -1)
            {
                var childNode = tree.GetOrAddNode(n);
                var parentNode = tree.GetOrAddNode(iDom);
                
                childNode.ImmediateDominator = parentNode;
                parentNode.Children.Add(childNode);
            }
        }

        return tree;
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
}

using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public class DataFlowAnalyzer
{
    public DataFlowAnalysisResult Analyze(ControlFlowGraph cfg)
    {
        if (cfg.Blocks.Length == 0) return new DataFlowAnalysisResult(new List<List<string>>());

        var dominator = new Dominator();
        var (domSets, dominatorTree) = dominator.Build(cfg);
        var pathConstraints = FlowAnalysis.GetPathConstraintsAcyclic(cfg, domSets);

        return new DataFlowAnalysisResult(pathConstraints, dominatorTree);
    }
}

internal class Dominator
{
    public Dictionary<int, HashSet<int>> ComputeDominatorSets(ControlFlowGraph cfg)
    {
        var n = cfg.Blocks.Length;
        var dom = new HashSet<int>[n];
        var allNodes = Enumerable.Range(0, n).ToHashSet();

        for (var i = 0; i < n; i++) dom[i] = new HashSet<int>(allNodes);
        dom[0] = new HashSet<int> { 0 };

        var preds = new List<int>[n];
        for (var i = 0; i < n; i++) preds[i] = new List<int>();
        foreach (var block in cfg.Blocks)
        {
            if (block.ConditionalSuccessor != null && block.ConditionalSuccessor.Destination != null)
                preds[block.ConditionalSuccessor.Destination.Ordinal].Add(block.Ordinal);
            if (block.FallThroughSuccessor != null && block.FallThroughSuccessor.Destination != null)
                preds[block.FallThroughSuccessor.Destination.Ordinal].Add(block.Ordinal);
        }

        var changed = true;
        while (changed)
        {
            changed = false;
            for (var i = 1; i < n; i++)
            {
                var newDom = new HashSet<int>(allNodes);
                if (preds[i].Count > 0)
                {
                    var first = true;
                    foreach (var p in preds[i])
                        if (first)
                        {
                            newDom = new HashSet<int>(dom[p]);
                            first = false;
                        }
                        else
                        {
                            newDom.IntersectWith(dom[p]);
                        }
                }
                else
                {
                    newDom = new HashSet<int>();
                }

                newDom.Add(i);

                if (!dom[i].SetEquals(newDom))
                {
                    dom[i] = newDom;
                    changed = true;
                }
            }
        }

        var result = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < n; i++) result[i] = dom[i];
        return result;
    }

    public DominatorTree BuildDominatorTree(ControlFlowGraph cfg, Dictionary<int, HashSet<int>> domSets)
    {
        var tree = new DominatorTree(0);

        foreach (var kvp in domSets)
        {
            var n = kvp.Key;
            if (n == 0) continue;

            var dominators = kvp.Value;
            var iDom = -1;

            foreach (var d in dominators)
            {
                if (d == n) continue;

                var isIDom = true;
                foreach (var otherD in dominators)
                {
                    if (otherD == n || otherD == d) continue;

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

    public (Dictionary<int, HashSet<int>> DominatorSets, DominatorTree DominatorTree) Build(ControlFlowGraph cfg)
    {
        var domSets = ComputeDominatorSets(cfg);
        var dominatorTree = BuildDominatorTree(cfg, domSets);
        return (domSets, dominatorTree);
    }
}
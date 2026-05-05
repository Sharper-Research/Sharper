using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public class DataFlowAnalyzer
{
    public DataFlowAnalysisResult Analyze(ControlFlowGraph cfg)
    {
        if (cfg.Blocks.Length == 0) return new DataFlowAnalysisResult([]);

        var domSets = Dominator.ComputeDominatorSets(cfg);
        var dominatorSets = new DominatorSets(domSets);
        var dominatorTree = Dominator.BuildDominatorTree(domSets);
        var pathConstraints = FlowAnalysis.GetPathConstraintsAcyclic(cfg, dominatorSets);

        return new DataFlowAnalysisResult(pathConstraints, dominatorTree, dominatorSets);
    }

    private static class Dominator
    {
        public static Dictionary<int, HashSet<int>> ComputeDominatorSets(ControlFlowGraph cfg)
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
                if (block.ConditionalSuccessor?.Destination != null)
                    preds[block.ConditionalSuccessor.Destination.Ordinal].Add(block.Ordinal);
                if (block.FallThroughSuccessor?.Destination != null)
                    preds[block.FallThroughSuccessor.Destination.Ordinal].Add(block.Ordinal);
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 1; i < n; i++)
                {
                    HashSet<int> newDom;
                    if (preds[i].Count > 0)
                    {
                        newDom = new HashSet<int>(dom[preds[i][0]]);
                        for (var p = 1; p < preds[i].Count; p++)
                            newDom.IntersectWith(dom[preds[i][p]]);
                    }
                    else
                    {
                        newDom = new HashSet<int>();
                    }

                    newDom.Add(i);

                    if (dom[i].SetEquals(newDom)) continue;
                    dom[i] = newDom;
                    changed = true;
                }
            }

            var result = new Dictionary<int, HashSet<int>>();
            for (var i = 0; i < n; i++) result[i] = dom[i];
            return result;
        }

        public static DominatorTree BuildDominatorTree(Dictionary<int, HashSet<int>> domSets)
        {
            var tree = new DominatorTree(0);

            foreach (var (n, dominators) in domSets)
            {
                if (n == 0) continue;

                // The immediate dominator is the strict dominator with the largest dominator set
                // (i.e., the closest ancestor in the dominator chain). O(n log n) vs O(n²).
                var iDom = dominators
                    .Where(d => d != n)
                    .OrderByDescending(d => domSets[d].Count)
                    .DefaultIfEmpty(-1)
                    .First();

                if (iDom == -1) continue;

                var childNode = tree.GetOrAddNode(n);
                var parentNode = tree.GetOrAddNode(iDom);
                childNode.ImmediateDominator = parentNode;
                parentNode.Children.Add(childNode);
            }

            return tree;
        }
    }
}
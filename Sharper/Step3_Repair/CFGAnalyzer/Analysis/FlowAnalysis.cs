using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Sharper.CFGAnalyzer.Analysis;

public static class FlowAnalysis
{
    /// <summary>
    ///     Explores acyclic paths to generate path constraints.
    /// </summary>
    /// <param name="cfg">The control flow graph to explore.</param>
    /// <param name="domSets">Pre-computed dominator sets used to suppress back-edges.</param>
    /// <param name="maxPaths">Maximum number of paths to collect. Defaults to 1000.</param>
    public static IReadOnlyList<IReadOnlyList<string>> GetPathConstraintsAcyclic(
        ControlFlowGraph cfg,
        DominatorSets domSets,
        int maxPaths = 1000)
    {
        var allPaths = new List<IReadOnlyList<string>>();
        if (cfg.Blocks.Length == 0) return allPaths;

        ExplorePathAcyclic(cfg.Blocks[0], [], domSets, allPaths, maxPaths);
        return allPaths;
    }

    private static void ExplorePathAcyclic(
        BasicBlock block,
        List<string> currentPath,
        DominatorSets domSets,
        List<IReadOnlyList<string>> allPaths,
        int maxPaths)
    {
        if (allPaths.Count >= maxPaths) return;

        var hasForwardSuccessor = false;

        if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null && block.BranchValue != null)
        {
            var condition = block.BranchValue.Syntax.ToString().Replace("\n", " ").Trim();
            var destA = block.ConditionalSuccessor.Destination;
            var destB = block.FallThroughSuccessor.Destination;

            var isBackA = destA != null && domSets.ContainsBlock(destA.Ordinal) &&
                          domSets.Dominates(block.Ordinal, destA.Ordinal);
            var isBackB = destB != null && domSets.ContainsBlock(destB.Ordinal) &&
                          domSets.Dominates(block.Ordinal, destB.Ordinal);

            if (!isBackA && destA != null)
            {
                var pathA = new List<string>(currentPath)
                {
                    block.ConditionKind == ControlFlowConditionKind.WhenTrue ? condition : $"!({condition})"
                };
                ExplorePathAcyclic(destA, pathA, domSets, allPaths, maxPaths);
                hasForwardSuccessor = true;
            }

            if (!isBackB && destB != null)
            {
                var pathB = new List<string>(currentPath)
                {
                    block.ConditionKind == ControlFlowConditionKind.WhenTrue ? $"!({condition})" : condition
                };
                ExplorePathAcyclic(destB, pathB, domSets, allPaths, maxPaths);
                hasForwardSuccessor = true;
            }
        }
        else if (block.FallThroughSuccessor?.Destination != null)
        {
            var dest = block.FallThroughSuccessor.Destination;
            if (domSets.ContainsBlock(dest.Ordinal) && !domSets.Dominates(block.Ordinal, dest.Ordinal))
            {
                ExplorePathAcyclic(dest, currentPath, domSets, allPaths, maxPaths);
                hasForwardSuccessor = true;
            }
        }

        if (!hasForwardSuccessor) allPaths.Add(new List<string>(currentPath));
    }

    /// <summary>
    ///     Explores acyclic paths to a specific target block, generating path constraints.
    /// </summary>
    /// <param name="cfg">The control flow graph to explore.</param>
    /// <param name="targetBlockOrdinal">The ordinal of the block to find paths to.</param>
    /// <param name="domSets">Pre-computed dominator sets used to suppress back-edges.</param>
    /// <param name="maxPaths">Maximum number of paths to collect. Defaults to 1000.</param>
    public static IReadOnlyList<IReadOnlyList<string>> GetPathsToBlockAcyclic(
        ControlFlowGraph cfg,
        int targetBlockOrdinal,
        DominatorSets domSets,
        int maxPaths = 1000)
    {
        var allPaths = new List<IReadOnlyList<string>>();
        if (cfg.Blocks.Length == 0) return allPaths;

        ExplorePathToBlockAcyclic(cfg.Blocks[0], targetBlockOrdinal, [], domSets, allPaths, maxPaths);
        return allPaths;
    }

    private static void ExplorePathToBlockAcyclic(
        BasicBlock block,
        int targetOrdinal,
        List<string> currentPath,
        DominatorSets domSets,
        List<IReadOnlyList<string>> allPaths,
        int maxPaths)
    {
        while (true)
        {
            if (allPaths.Count >= maxPaths) return;

            if (block.Ordinal == targetOrdinal)
            {
                allPaths.Add(new List<string>(currentPath));
                return;
            }

            if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null && block.BranchValue != null)
            {
                var condition = block.BranchValue.Syntax.ToString().Replace("\n", " ").Trim();
                var destA = block.ConditionalSuccessor.Destination;
                var destB = block.FallThroughSuccessor.Destination;

                var isBackA = destA != null && domSets.ContainsBlock(destA.Ordinal) && domSets.Dominates(block.Ordinal, destA.Ordinal);
                var isBackB = destB != null && domSets.ContainsBlock(destB.Ordinal) && domSets.Dominates(block.Ordinal, destB.Ordinal);

                if (!isBackA && destA != null)
                {
                    var pathA = new List<string>(currentPath)
                    {
                        block.ConditionKind == ControlFlowConditionKind.WhenTrue ? condition : $"!({condition})"
                    };
                    ExplorePathToBlockAcyclic(destA, targetOrdinal, pathA, domSets, allPaths, maxPaths);
                }

                if (isBackB || destB == null) return;
                currentPath = new List<string>(currentPath)
                {
                    block.ConditionKind == ControlFlowConditionKind.WhenTrue ? $"!({condition})" : condition
                };
                block = destB;
                continue;
            }

            if (block.FallThroughSuccessor?.Destination != null)
            {
                var dest = block.FallThroughSuccessor.Destination;
                if (domSets.ContainsBlock(dest.Ordinal) && !domSets.Dominates(block.Ordinal, dest.Ordinal))
                {
                    block = dest;
                    continue;
                }
            }

            break;
        }
    }
}
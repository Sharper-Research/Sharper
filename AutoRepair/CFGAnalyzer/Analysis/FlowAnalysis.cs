using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public static class FlowAnalysis
{

    /// <summary>
    /// Explores acyclic paths to generate path constraints.
    /// </summary>
    public static List<List<string>> GetPathConstraintsAcyclic(ControlFlowGraph cfg, Dictionary<int, HashSet<int>> domSets)
    {
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
            string condition = block.BranchValue.Syntax.ToString().Replace("\\n", " ").Trim();
            var destA = block.ConditionalSuccessor.Destination;
            var destB = block.FallThroughSuccessor.Destination;

            bool isBackA = destA != null && domSets.ContainsKey(destA.Ordinal) && domSets[block.Ordinal].Contains(destA.Ordinal);
            bool isBackB = destB != null && domSets.ContainsKey(destB.Ordinal) && domSets[block.Ordinal].Contains(destB.Ordinal);

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
            if (dest != null && domSets.ContainsKey(dest.Ordinal) && !domSets[block.Ordinal].Contains(dest.Ordinal))
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

    /// <summary>
    /// Explores acyclic paths to a specific target block, generating path constraints.
    /// </summary>
    public static List<List<string>> GetPathsToBlockAcyclic(ControlFlowGraph cfg, int targetBlockOrdinal, Dictionary<int, HashSet<int>> domSets)
    {
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

        if (block.ConditionalSuccessor != null && block.FallThroughSuccessor != null && block.BranchValue != null) {
        string condition = block.BranchValue.Syntax.ToString().Replace("\\n", " ").Trim();
            var destA = block.ConditionalSuccessor.Destination;
            var destB = block.FallThroughSuccessor.Destination;

            bool isBackA = destA != null && domSets.ContainsKey(destA.Ordinal) && domSets[block.Ordinal].Contains(destA.Ordinal);
            bool isBackB = destB != null && domSets.ContainsKey(destB.Ordinal) && domSets[block.Ordinal].Contains(destB.Ordinal);

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
            if (dest != null && domSets.ContainsKey(dest.Ordinal) && !domSets[block.Ordinal].Contains(dest.Ordinal))
                ExplorePathToBlockAcyclic(dest, targetOrdinal, currentPath, domSets, allPaths);
        }
    }
}

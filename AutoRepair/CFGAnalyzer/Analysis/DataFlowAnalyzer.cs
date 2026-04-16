using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using AutoRepair.CFGAnalyzer.Models;
using CFGAnalyzer.Analysis;

// Add this using directive

namespace AutoRepair.CFGAnalyzer.Analysis;

public class DataFlowAnalyzer
{
    public DataFlowAnalysisResult Analyze(ControlFlowGraph cfg)
    {
        if (cfg.Blocks.Length == 0)
        {
            return new DataFlowAnalysisResult(new List<List<string>>(), null);
        }

        var domSets = FlowAnalysis.ComputeDominatorSets(cfg);
        var dominatorTree = FlowAnalysis.BuildDominatorTree(cfg, domSets); 
        var pathConstraints = FlowAnalysis.GetPathConstraintsAcyclic(cfg, domSets);

        return new DataFlowAnalysisResult(pathConstraints, dominatorTree);
    }
}

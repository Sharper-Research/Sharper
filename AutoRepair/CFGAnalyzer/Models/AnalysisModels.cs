using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace AutoRepair.CFGAnalyzer.Models;

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

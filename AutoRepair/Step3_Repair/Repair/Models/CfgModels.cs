namespace AutoRepair.Step3Repair.Models;

/// <summary>A single node in the control flow graph.</summary>
public sealed record CfgNode(
    int Id,
    string Kind,       // entry, exit, statement, branch, join, loop-header, loop-exit, return, throw, ...
    int StartLine,
    int EndLine,
    string Code);

/// <summary>A directed edge between two CFG nodes.</summary>
public sealed record CfgEdge(
    int FromId,
    int ToId,
    string Label);     // sequential, true, false, back-edge, return, throw, exception, break, continue, ...

/// <summary>CFG for a single method/accessor that contains at least one violation.</summary>
public sealed record MethodCfg(
    string MethodName,
    string File,
    int MethodStartLine,
    int MethodEndLine,
    List<int> ViolationLines,
    List<CfgNode> Nodes,
    List<CfgEdge> Edges);

/// <summary>Top-level report containing all method CFGs.</summary>
public sealed record CfgReport(
    DateTime GeneratedAt,
    string SourceDirectory,
    int TotalViolations,
    int MethodsAnalyzed,
    List<MethodCfg> Methods);

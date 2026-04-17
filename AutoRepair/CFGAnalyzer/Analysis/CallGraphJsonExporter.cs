using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace AutoRepair.CFGAnalyzer.Analysis;

public static class CallGraphJsonExporter
{
    public static CallGraphJsonDto ToDto(CallGraph callGraph, string? projectName = null)
    {
        var nodes = callGraph.Nodes.Values
            .OrderBy(node => node.Symbol.ToDisplayString(), StringComparer.Ordinal)
            .Select(ToNodeDto)
            .ToList();

        var edges = callGraph.Edges
            .OrderBy(edge => edge.Caller.Symbol.ToDisplayString(), StringComparer.Ordinal)
            .ThenBy(edge => edge.Callee.Symbol.ToDisplayString(), StringComparer.Ordinal)
            .ThenBy(edge => edge.CallSite.BlockOrdinal)
            .Select(ToEdgeDto)
            .ToList();

        return new CallGraphJsonDto(projectName, nodes, edges);
    }

    public static string ToJson(CallGraph callGraph, string? projectName = null, bool indented = true)
    {
        var dto = ToDto(callGraph, projectName);
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = indented
        });
    }

    private static CallGraphNodeJsonDto ToNodeDto(CallGraphNode node)
    {
        var symbol = node.Symbol;
        return new CallGraphNodeJsonDto(
            Id: GetSymbolId(symbol),
            DisplayName: symbol.ToDisplayString(),
            MethodName: symbol.Name,
            ContainingType: symbol.ContainingType?.ToDisplayString(),
            ContainingNamespace: symbol.ContainingNamespace?.ToDisplayString(),
            AssemblyName: symbol.ContainingAssembly?.Name,
            Analysis: node.Analysis != null ? ToAnalysisDto(node.Analysis) : null);
    }

    private static CallGraphAnalysisJsonDto ToAnalysisDto(MethodAnalysisResult analysis)
    {
        var callSites = analysis.CallSites
            .OrderBy(site => site.BlockOrdinal)
            .ThenBy(site => site.Target.ToDisplayString(), StringComparer.Ordinal)
            .Select(site => new CallSiteJsonDto(
                BlockOrdinal: site.BlockOrdinal,
                TargetId: GetSymbolId(site.Target),
                TargetDisplayName: site.Target.ToDisplayString(),
                IsInternal: site.IsInternal,
                CallerLineNumber: site.CallerLineNumber,
                CalleeLineNumber: site.CalleeLineNumber))
            .ToList();

        DominatorTreeJsonDto? dominatorTree = null;
        if (analysis.DominatorTree != null)
        {
            var dominatorNodes = analysis.DominatorTree.Nodes.Values
                .OrderBy(node => node.BlockOrdinal)
                .Select(node => new DominatorTreeNodeJsonDto(
                    BlockOrdinal: node.BlockOrdinal,
                    ImmediateDominatorOrdinal: node.ImmediateDominator?.BlockOrdinal,
                    ChildOrdinals: node.Children.Select(child => child.BlockOrdinal).OrderBy(x => x).ToList()))
                .ToList();

            dominatorTree = new DominatorTreeJsonDto(
                RootOrdinal: analysis.DominatorTree.Root.BlockOrdinal,
                Nodes: dominatorNodes);
        }

        return new CallGraphAnalysisJsonDto(
            PathConstraints: analysis.PathConstraints.Select(path => path.ToList()).ToList(),
            CallSites: callSites,
            DominatorTree: dominatorTree);
    }

    private static CallGraphEdgeJsonDto ToEdgeDto(CallGraphEdge edge)
    {
        return new CallGraphEdgeJsonDto(
            CallerId: GetSymbolId(edge.Caller.Symbol),
            CallerDisplayName: edge.Caller.Symbol.ToDisplayString(),
            CalleeId: GetSymbolId(edge.Callee.Symbol),
            CalleeDisplayName: edge.Callee.Symbol.ToDisplayString(),
            BlockOrdinal: edge.CallSite.BlockOrdinal,
            IsInternal: edge.CallSite.IsInternal,
            CallerLineNumber: edge.CallSite.CallerLineNumber,
            CalleeLineNumber: edge.CallSite.CalleeLineNumber);
    }

    private static string GetSymbolId(IMethodSymbol symbol)
    {
        return string.Join("::",
            symbol.ContainingAssembly?.Name ?? "<unknown-assembly>",
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
    }
}

public record CallGraphJsonDto(
    string? ProjectName,
    List<CallGraphNodeJsonDto> Nodes,
    List<CallGraphEdgeJsonDto> Edges);

public record CallGraphNodeJsonDto(
    string Id,
    string DisplayName,
    string MethodName,
    string? ContainingType,
    string? ContainingNamespace,
    string? AssemblyName,
    CallGraphAnalysisJsonDto? Analysis);

public record CallGraphAnalysisJsonDto(
    List<List<string>> PathConstraints,
    List<CallSiteJsonDto> CallSites,
    DominatorTreeJsonDto? DominatorTree);

public record CallSiteJsonDto(
    int BlockOrdinal,
    string TargetId,
    string TargetDisplayName,
    bool IsInternal,
    int? CallerLineNumber,
    int? CalleeLineNumber);

public record DominatorTreeJsonDto(
    int RootOrdinal,
    List<DominatorTreeNodeJsonDto> Nodes);

public record DominatorTreeNodeJsonDto(
    int BlockOrdinal,
    int? ImmediateDominatorOrdinal,
    List<int> ChildOrdinals);

public record CallGraphEdgeJsonDto(
    string CallerId,
    string CallerDisplayName,
    string CalleeId,
    string CalleeDisplayName,
    int BlockOrdinal,
    bool IsInternal,
    int? CallerLineNumber,
    int? CalleeLineNumber);


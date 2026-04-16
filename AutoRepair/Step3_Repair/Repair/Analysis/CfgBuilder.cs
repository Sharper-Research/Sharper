using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using AutoRepair.Step3Repair.Models;

namespace AutoRepair.Step3Repair.Analysis;

/// <summary>
/// Builds a Control Flow Graph for each method/accessor whose line range
/// overlaps with at least one violation line.  Uses Roslyn's built-in
/// <see cref="ControlFlowGraph"/> API instead of hand-rolled syntax walking.
/// </summary>
public sealed class CfgBuilder
{
    private CSharpCompilation? _compilation;
    private readonly Dictionary<string, (SyntaxTree Tree, SemanticModel Model)> _models = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the Roslyn compilation with all source files under <paramref name="sourceDirectory"/>.
    /// Must be called once before <see cref="BuildForFile"/>.
    /// </summary>
    public void Initialize(string sourceDirectory)
    {
        var files = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !Path.GetFileName(f).Equals("TraceLogger.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var trees = files.Select(f =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)).ToList();

        var refs = GetRuntimeReferences();

        _compilation = CSharpCompilation.Create("analysis", trees, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        foreach (var tree in trees)
            _models[tree.FilePath] = (tree, _compilation.GetSemanticModel(tree));
    }

    // ───────────────────────── public API ─────────────────────────

    public List<MethodCfg> BuildForFile(string fullPath, string relativePath, List<int> violationLines)
    {
        if (_compilation is null)
        {
            // Fallback: initialize with just this file's directory
            Initialize(Path.GetDirectoryName(fullPath)!);
        }

        if (!_models.TryGetValue(fullPath, out var entry))
        {
            // File wasn't in the compilation (e.g. generated file). Parse standalone.
            var source = File.ReadAllText(fullPath);
            var tree = CSharpSyntaxTree.ParseText(source, path: fullPath);
            var comp = _compilation!.AddSyntaxTrees(tree);
            entry = (tree, comp.GetSemanticModel(tree));
        }

        var (syntaxTree, model) = entry;
        var root = syntaxTree.GetRoot();
        var result = new List<MethodCfg>();

        foreach (var member in root.DescendantNodes())
        {
            string? name;
            SyntaxNode? body;

            switch (member)
            {
                case MethodDeclarationSyntax m:
                    name = m.Identifier.Text;
                    body = (SyntaxNode?)m.Body ?? m.ExpressionBody?.Expression;
                    break;
                case ConstructorDeclarationSyntax c:
                    name = c.Identifier.Text + "..ctor";
                    body = c.Body;
                    break;
                case AccessorDeclarationSyntax a:
                    name = AccessorName(a);
                    body = (SyntaxNode?)a.Body ?? a.ExpressionBody?.Expression;
                    break;
                default:
                    continue;
            }

            if (body is null) continue;

            var span = member.GetLocation().GetLineSpan();
            int startLine = span.StartLinePosition.Line + 1;
            int endLine = span.EndLinePosition.Line + 1;

            var relevant = violationLines.Where(l => l >= startLine && l <= endLine).ToList();
            if (relevant.Count == 0) continue;

            try
            {
                // ControlFlowGraph.Create needs the method/constructor/accessor
                // declaration itself so Roslyn gets the root IOperation node
                var cfg = ControlFlowGraph.Create(member, model);
                if (cfg is null) continue;
                var (nodes, edges) = MapToModel(cfg, startLine, endLine, name!);

                result.Add(new MethodCfg(
                    MethodName: name!,
                    File: relativePath,
                    MethodStartLine: startLine,
                    MethodEndLine: endLine,
                    ViolationLines: relevant,
                    Nodes: nodes,
                    Edges: edges));
            }
            catch (ArgumentException)
            {
                // Roslyn couldn't build CFG (e.g. incomplete semantic info). Skip method.
            }
        }

        return result;
    }

    // ──────────────── map Roslyn CFG to our model ─────────────────

    private static (List<CfgNode> Nodes, List<CfgEdge> Edges) MapToModel(
        ControlFlowGraph cfg, int methodStart, int methodEnd, string methodName)
    {
        var nodes = new List<CfgNode>();
        var edges = new List<CfgEdge>();

        foreach (var block in cfg.Blocks)
        {
            var kind = DetermineKind(block, cfg);
            var (blockStart, blockEnd, code) = GetBlockInfo(block, methodStart, methodEnd, methodName);

            nodes.Add(new CfgNode(block.Ordinal, kind, blockStart, blockEnd, Trunc(code, 150)));

            // Fall-through successor
            if (block.FallThroughSuccessor is { Destination: { } fallDest })
            {
                var label = block.FallThroughSuccessor.Semantics switch
                {
                    ControlFlowBranchSemantics.Return => "return",
                    ControlFlowBranchSemantics.Throw => "throw",
                    ControlFlowBranchSemantics.Rethrow => "throw",
                    ControlFlowBranchSemantics.ProgramTermination => "throw",
                    _ => block.ConditionalSuccessor != null ? "false" : "sequential"
                };

                // Detect back-edge (successor has lower ordinal)
                if (fallDest.Ordinal < block.Ordinal && label == "sequential")
                    label = "back-edge";

                edges.Add(new CfgEdge(block.Ordinal, fallDest.Ordinal, label));
            }

            // Conditional successor
            if (block.ConditionalSuccessor is { Destination: { } condDest })
            {
                var label = block.ConditionKind == ControlFlowConditionKind.WhenTrue
                    ? "true" : "false";

                if (condDest.Ordinal < block.Ordinal)
                    label = "back-edge";

                edges.Add(new CfgEdge(block.Ordinal, condDest.Ordinal, label));
            }
        }

        return (nodes, edges);
    }

    private static string DetermineKind(BasicBlock block, ControlFlowGraph cfg)
    {
        if (block.Kind == BasicBlockKind.Entry) return "entry";
        if (block.Kind == BasicBlockKind.Exit) return "exit";

        // Check enclosing region
        var region = block.EnclosingRegion;
        while (region != null)
        {
            switch (region.Kind)
            {
                case ControlFlowRegionKind.Catch: return "catch";
                case ControlFlowRegionKind.Finally: return "finally";
                case ControlFlowRegionKind.Filter: return "filter";
            }
            region = region.EnclosingRegion;
        }

        // Check for branch (has conditional successor)
        if (block.ConditionalSuccessor != null)
        {
            // Check if this is a loop header (has back-edge predecessor)
            foreach (var pred in block.Predecessors)
            {
                if (pred.Source.Ordinal > block.Ordinal)
                    return "loop-header";
            }
            return "branch";
        }

        // Check exit semantics
        if (block.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Return)
            return "return";
        if (block.FallThroughSuccessor?.Semantics is ControlFlowBranchSemantics.Throw
            or ControlFlowBranchSemantics.Rethrow)
            return "throw";

        return "statement";
    }

    private static (int Start, int End, string Code) GetBlockInfo(
        BasicBlock block, int methodStart, int methodEnd, string methodName)
    {
        if (block.Kind == BasicBlockKind.Entry)
            return (methodStart, methodStart, $"Enter {methodName}");
        if (block.Kind == BasicBlockKind.Exit)
            return (methodEnd, methodEnd, $"Exit {methodName}");

        int minLine = int.MaxValue, maxLine = int.MinValue;
        var codeBuilder = new System.Text.StringBuilder();

        foreach (var op in block.Operations)
        {
            var loc = op.Syntax.GetLocation().GetLineSpan();
            var start = loc.StartLinePosition.Line + 1;
            var end = loc.EndLinePosition.Line + 1;
            if (start < minLine) minLine = start;
            if (end > maxLine) maxLine = end;

            if (codeBuilder.Length > 0) codeBuilder.Append("; ");
            codeBuilder.Append(Collapse(op.Syntax.ToString()));
        }

        // Include branch value in code
        if (block.BranchValue != null)
        {
            var bLoc = block.BranchValue.Syntax.GetLocation().GetLineSpan();
            var bStart = bLoc.StartLinePosition.Line + 1;
            var bEnd = bLoc.EndLinePosition.Line + 1;
            if (bStart < minLine) minLine = bStart;
            if (bEnd > maxLine) maxLine = bEnd;

            if (codeBuilder.Length > 0) codeBuilder.Append("; ");
            codeBuilder.Append(Collapse(block.BranchValue.Syntax.ToString()));
        }

        if (minLine == int.MaxValue) minLine = methodStart;
        if (maxLine == int.MinValue) maxLine = minLine;

        return (minLine, maxLine, codeBuilder.Length > 0 ? codeBuilder.ToString() : "(empty)");
    }

    // ───────────────────── utilities ──────────────────────────

    private static List<MetadataReference> GetRuntimeReferences()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var refs = new List<MetadataReference>();
        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
        {
            try { refs.Add(MetadataReference.CreateFromFile(dll)); }
            catch { /* skip native/invalid DLLs */ }
        }
        return refs;
    }

    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    private static string Trunc(string text, int max) => text.Length > max ? text[..(max - 3)] + "..." : text;

    private static string AccessorName(AccessorDeclarationSyntax a)
    {
        var prop = a.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        return $"{prop?.Identifier.Text ?? "property"}.{a.Keyword.Text}";
    }
}

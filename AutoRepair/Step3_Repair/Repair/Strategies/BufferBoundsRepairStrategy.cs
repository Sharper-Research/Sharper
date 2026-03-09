using AutoRepair.Step3Repair.Models;        // for ConstraintViolation, PatchResult
using Microsoft.CodeAnalysis;                // Roslyn base types
using Microsoft.CodeAnalysis.CSharp;         // for CSharpSyntaxTree
using Microsoft.CodeAnalysis.CSharp.Syntax;  // for ElementAccessExpressionSyntax etc.

namespace AutoRepair.Step3Repair.Strategies;

// fixes buffer-bounds violations by wrapping array accesses in bounds checks
// handles BufferInBounds violations (the generic out-of-range ones)
public sealed class BufferBoundsRepairStrategy : IRepairStrategy
{
    // only handles BufferInBounds violations
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("BufferInBounds(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        // find the syntax node on the violation line
        var node = FindNodeAtLine(root, targetLine);
        if (node is null)
            return null;

        // walk up to the enclosing statement
        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement is null)
            return null;

        // find all array element accesses like buffer[i] in this statement
        var elementAccesses = statement.DescendantNodes()
            .OfType<ElementAccessExpressionSyntax>()
            .ToList();

        if (elementAccesses.Count == 0)
            return null; // no array accesses, nothing we can do

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        // build a bounds check condition for each array access
        var guards = new List<string>();
        foreach (var access in elementAccesses)
        {
            var arrayExpr = access.Expression.ToString();     // the array itself
            var indexArgs = access.ArgumentList.Arguments;
            if (indexArgs.Count != 1) continue;               // only handle single dimension

            var indexExpr = indexArgs[0].ToString();           // the index being used
            guards.Add($"{indexExpr} >= 0 && {indexExpr} < {arrayExpr}.Length"); // bounds check
        }

        if (guards.Count == 0)
            return null;

        // wrap the original statement in an if with the bounds check
        var condition = string.Join(" && ", guards);
        var patchedCode =
            $"{indentation}if ({condition})\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {statement.ToString().Trim()}\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "BufferBoundsRepair",
            Description: $"Added bounds check guard for array access",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    // finds the biggest syntax node that starts on the given line
    private static SyntaxNode? FindNodeAtLine(SyntaxNode root, int line)
    {
        return root.DescendantNodes()
            .Where(n => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == line)
            .OrderByDescending(n => n.Span.Length)
            .FirstOrDefault();
    }

    // gets the whitespace indentation at the start of a node
    private static string GetIndentation(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia().ToString();
        var lastNewLine = leadingTrivia.LastIndexOf('\n');
        return lastNewLine >= 0 ? leadingTrivia[(lastNewLine + 1)..] : leadingTrivia;
    }
}

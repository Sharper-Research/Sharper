using AutoRepair.Step3Repair.Models;        // for ConstraintViolation, PatchResult
using Microsoft.CodeAnalysis;                // Roslyn base types
using Microsoft.CodeAnalysis.CSharp;         // for SyntaxKind
using Microsoft.CodeAnalysis.CSharp.Syntax;  // for syntax node types like ForStatementSyntax

namespace AutoRepair.Step3Repair.Strategies;

// fixes off-by-one errors where index == array.Length
// two strategies: first tries to find a loop with <= and fix it to <
// if that doesnt work, falls back to wrapping in a bounds check
public sealed class OffByOneRepairStrategy : IRepairStrategy
{
    // only handles violations that start with "OffByOne("
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("OffByOne(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        // strategy 1: look for a loop condition near this line that uses <= and should use <
        var loopFix = TryFixLoopCondition(root, sourceCode, targetLine, violation);
        if (loopFix is not null)
            return loopFix; // found it, done

        // strategy 2: if no loop found, fall back to adding a bounds-check if guard
        return TryAddBoundsGuard(root, targetLine, violation);
    }

    // searches for while/for loops that contain the violation line
    // and tries to find a <= comparison in the condition to change to <
    private static PatchResult? TryFixLoopCondition(SyntaxNode root, string sourceCode, int targetLine, ConstraintViolation violation)
    {
        // find all while and for loops in the file
        var loops = root.DescendantNodes()
            .Where(n => n is WhileStatementSyntax or ForStatementSyntax)
            .ToList();

        foreach (var loop in loops)
        {
            // figure out what lines this loop spans
            var loopSpan = loop.GetLocation().GetLineSpan();
            var loopStartLine = loopSpan.StartLinePosition.Line + 1; // roslyn uses 0-based lines
            var loopEndLine = loopSpan.EndLinePosition.Line + 1;

            // the violation line needs to be inside this loop for it to be relevant
            if (targetLine < loopStartLine || targetLine > loopEndLine)
                continue;

            // get the loop condition depending on whether its a while or for
            ExpressionSyntax? condition = loop switch
            {
                WhileStatementSyntax w => w.Condition,
                ForStatementSyntax f => f.Condition,
                _ => null
            };

            if (condition is null) continue;

            // look for <= comparisons in the condition
            var leExpressions = condition.DescendantNodesAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .Where(b => b.IsKind(SyntaxKind.LessThanOrEqualExpression))
                .ToList();

            if (leExpressions.Count == 0) continue; // no <= found

            foreach (var leExpr in leExpressions)
            {
                var originalCondition = leExpr.ToString();
                var fixedCondition = $"{leExpr.Left} < {leExpr.Right}"; // change <= to <

                var condLine = leExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                return new PatchResult(
                    File: violation.File,
                    Line: condLine,
                    RuleName: "OffByOneRepair",
                    Description: $"Changed loop condition from '<=' to '<' to fix off-by-one error",
                    OriginalCode: originalCondition,
                    PatchedCode: fixedCondition,
                    Violation: violation);
            }
        }

        return null; // no loop with <= found
    }

    // fallback: wraps the array access in a bounds-check if statement
    // like: if (index >= 0 && index < array.Length) { ...original statement... }
    private static PatchResult? TryAddBoundsGuard(SyntaxNode root, int targetLine, ConstraintViolation violation)
    {
        // find the biggest syntax node that starts on the violation line
        var node = root.DescendantNodes()
            .Where(n => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == targetLine)
            .OrderByDescending(n => n.Span.Length)
            .FirstOrDefault();

        if (node is null) return null;

        // walk up to the enclosing statement
        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement is null) return null;

        // find array access expressions like buffer[i]
        var elementAccesses = statement.DescendantNodes()
            .OfType<ElementAccessExpressionSyntax>()
            .ToList();

        if (elementAccesses.Count == 0) return null; // no array accesses to guard

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        // build the guard condition for each array access
        var guards = new List<string>();
        foreach (var access in elementAccesses)
        {
            var arrayExpr = access.Expression.ToString();     // the array variable
            var indexArgs = access.ArgumentList.Arguments;
            if (indexArgs.Count != 1) continue;               // only single-dimension arrays
            var indexExpr = indexArgs[0].ToString();           // the index expression
            guards.Add($"{indexExpr} >= 0 && {indexExpr} < {arrayExpr}.Length");
        }

        if (guards.Count == 0) return null;

        // combine all guards and wrap the original statement
        var condition = string.Join(" && ", guards);
        var patchedCode =
            $"{indentation}if ({condition})\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {statement.ToString().Trim()}\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "OffByOneRepair",
            Description: "Added bounds check to prevent off-by-one array access",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    // helper to figure out the indentation of a syntax node
    // looks at the whitespace before the node to match the formatting
    private static string GetIndentation(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia().ToString();
        var lastNewLine = leadingTrivia.LastIndexOf('\n');
        return lastNewLine >= 0 ? leadingTrivia[(lastNewLine + 1)..] : leadingTrivia;
    }
}

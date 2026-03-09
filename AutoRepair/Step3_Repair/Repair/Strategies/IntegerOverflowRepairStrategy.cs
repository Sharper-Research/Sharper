using AutoRepair.Step3Repair.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoRepair.Step3Repair.Strategies;

/// <summary>
/// Repairs integer-overflow violations by wrapping arithmetic operations in a
/// <c>checked</c> context or promoting operands to a wider type.
/// </summary>
public sealed class IntegerOverflowRepairStrategy : IRepairStrategy
{
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("NoOverflow(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        // Extract operator and type from bindings
        violation.Bindings.TryGetValue("operator", out var opObj);
        violation.Bindings.TryGetValue("resultType", out var resultTypeObj);

        var op = opObj?.ToString() ?? "+";
        var resultType = resultTypeObj?.ToString() ?? "Int32";

        var node = FindNodeAtLine(root, targetLine);
        if (node is null)
            return null;

        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement is null)
            return null;

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        // Strategy: Determine if we should promote to long or wrap in checked
        if (resultType.Equals("Int32", StringComparison.OrdinalIgnoreCase))
        {
            // For Int32 overflow: promote to long for the operation
            return PromoteToLong(violation, statement, targetLine, indentation, originalCode, op);
        }

        // For Int64 or other types: wrap in checked context
        return WrapInChecked(violation, statement, targetLine, indentation, originalCode, resultType);
    }

    private static PatchResult PromoteToLong(
        ConstraintViolation violation, StatementSyntax statement,
        int targetLine, string indentation, string originalCode, string op)
    {
        // Find binary expressions with the overflow operator
        var binaryExprs = statement.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(b => GetOperator(b) == op)
            .ToList();

        if (binaryExprs.Count == 0)
        {
            // Compound assignment (+=, *=, etc.)
            var assignments = statement.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .ToList();

            if (assignments.Count > 0)
            {
                var patchedCode =
                    $"{indentation}checked\n" +
                    $"{indentation}{{\n" +
                    $"{indentation}    {statement.ToString().Trim()}\n" +
                    $"{indentation}}}";

                return new PatchResult(
                    File: violation.File,
                    Line: targetLine,
                    RuleName: "IntegerOverflowRepair",
                    Description: $"Wrapped compound assignment in checked context to detect overflow",
                    OriginalCode: originalCode,
                    PatchedCode: patchedCode,
                    Violation: violation);
            }
        }

        // Wrap the statement in a checked block for safety
        var checkedCode =
            $"{indentation}checked\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {statement.ToString().Trim()}\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "IntegerOverflowRepair",
            Description: $"Wrapped arithmetic ({op}) in checked context to prevent silent overflow",
            OriginalCode: originalCode,
            PatchedCode: checkedCode,
            Violation: violation);
    }

    private static PatchResult WrapInChecked(
        ConstraintViolation violation, StatementSyntax statement,
        int targetLine, string indentation, string originalCode, string resultType)
    {
        var patchedCode =
            $"{indentation}checked\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {statement.ToString().Trim()}\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "IntegerOverflowRepair",
            Description: $"Wrapped {resultType} arithmetic in checked context to throw on overflow",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    private static string? GetOperator(BinaryExpressionSyntax binary)
    {
        return binary.Kind() switch
        {
            SyntaxKind.AddExpression => "+",
            SyntaxKind.SubtractExpression => "-",
            SyntaxKind.MultiplyExpression => "*",
            SyntaxKind.DivideExpression => "/",
            _ => null
        };
    }

    private static SyntaxNode? FindNodeAtLine(SyntaxNode root, int line)
    {
        return root.DescendantNodes()
            .Where(n => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == line)
            .OrderByDescending(n => n.Span.Length)
            .FirstOrDefault();
    }

    private static string GetIndentation(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia().ToString();
        var lastNewLine = leadingTrivia.LastIndexOf('\n');
        return lastNewLine >= 0 ? leadingTrivia[(lastNewLine + 1)..] : leadingTrivia;
    }
}

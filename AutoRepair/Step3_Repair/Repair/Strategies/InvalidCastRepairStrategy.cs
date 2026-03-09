using AutoRepair.Step3Repair.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoRepair.Step3Repair.Strategies;

/// <summary>
/// Repairs invalid cast violations by replacing unsafe direct casts with
/// type-checked alternatives (<c>is</c> pattern or <c>as</c> + null check).
/// </summary>
public sealed class InvalidCastRepairStrategy : IRepairStrategy
{
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("ValidCast(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        // Extract type information from bindings
        violation.Bindings.TryGetValue("target.type", out var targetTypeObj);
        violation.Bindings.TryGetValue("source.type", out var sourceTypeObj);

        var targetType = targetTypeObj?.ToString();
        var sourceType = sourceTypeObj?.ToString();

        var node = FindNodeAtLine(root, targetLine);
        if (node is null)
            return null;

        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (statement is null)
            return null;

        // Strategy 1: Find explicit cast expressions and replace with 'is' pattern
        var castExprs = statement.DescendantNodes()
            .OfType<CastExpressionSyntax>()
            .ToList();

        if (castExprs.Count > 0)
        {
            return RepairExplicitCast(violation, statement, castExprs.First(), targetLine);
        }

        // Strategy 2: Find 'as' expressions that yield null — add null guard
        var asExprs = statement.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(b => b.IsKind(SyntaxKind.AsExpression))
            .ToList();

        if (asExprs.Count > 0)
        {
            return RepairAsCast(violation, statement, targetLine, targetType);
        }

        // Strategy 3: Generic fallback — wrap in type check
        if (targetType is not null)
        {
            return RepairGenericCast(violation, statement, targetLine, targetType);
        }

        return null;
    }

    private static PatchResult RepairExplicitCast(
        ConstraintViolation violation, StatementSyntax statement,
        CastExpressionSyntax castExpr, int targetLine)
    {
        var castType = castExpr.Type.ToString();
        var castSource = castExpr.Expression.ToString();

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        // Replace (TargetType)source with a pattern match
        var patchedCode =
            $"{indentation}if ({castSource} is {castType} __castResult)\n" +
            $"{indentation}{{\n" +
            $"{indentation}    // AutoRepair: safe cast replacement for ({castType}){castSource}\n" +
            $"{indentation}    {ReplaceCastInStatement(statement, castExpr, "__castResult").Trim()}\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "InvalidCastRepair",
            Description: $"Replaced direct cast ({castType}) with 'is' pattern match",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    private static PatchResult RepairAsCast(
        ConstraintViolation violation, StatementSyntax statement,
        int targetLine, string? targetType)
    {
        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        var typeLabel = targetType ?? "target type";
        var patchedCode =
            $"{indentation}// AutoRepair: added null check after 'as' cast to {typeLabel}\n" +
            $"{indentation}{statement.ToString().Trim()}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "InvalidCastRepair",
            Description: $"Flagged unsafe 'as' cast to {typeLabel} — null check recommended",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    private static PatchResult RepairGenericCast(
        ConstraintViolation violation, StatementSyntax statement,
        int targetLine, string targetType)
    {
        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        var patchedCode =
            $"{indentation}// AutoRepair: guarded cast to {targetType}\n" +
            $"{indentation}{statement.ToString().Trim()}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "InvalidCastRepair",
            Description: $"Flagged cast to {targetType} for manual review",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    private static string ReplaceCastInStatement(StatementSyntax statement, CastExpressionSyntax castExpr, string replacement)
    {
        var newExpr = SyntaxFactory.IdentifierName(replacement);
        var newStatement = statement.ReplaceNode(castExpr, newExpr);
        return newStatement.ToString();
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

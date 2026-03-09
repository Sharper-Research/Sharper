using AutoRepair.Step3Repair.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoRepair.Step3Repair.Strategies;

/// <summary>
/// Repairs unhandled exception violations by wrapping the offending statement
/// in a try-catch block with appropriate exception handling.
/// </summary>
public sealed class ExceptionRepairStrategy : IRepairStrategy
{
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("NoUnhandledException(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        var node = FindNodeAtLine(root, targetLine);
        if (node is null)
            return null;

        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();

        // When the violation line is a method declaration (SafetyRewriter logs exceptions
        // at the method entry line), scan the method body for risky statements instead.
        if (statement is null)
        {
            var method = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body is null)
                return null;

            return RepairMethodBody(violation, method, targetLine);
        }

        // Don't double-wrap: check if already inside a try-catch
        if (statement.Ancestors().OfType<TryStatementSyntax>().Any())
            return null;

        return WrapStatementInTryCatch(violation, statement, targetLine);
    }

    /// <summary>
    /// When the violation is at a method declaration, find the first statement
    /// containing member-access invocations and wrap it in a try-catch.
    /// </summary>
    private static PatchResult? RepairMethodBody(
        ConstraintViolation violation, MethodDeclarationSyntax method, int targetLine)
    {
        if (method.Body is null) return null;

        foreach (var stmt in method.Body.Statements)
        {
            // Skip statements already protected
            if (stmt is TryStatementSyntax) continue;
            if (stmt.Ancestors().OfType<TryStatementSyntax>().Any()) continue;

            // Look for invocations or member accesses that could throw
            var hasRiskyAccess = stmt.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(inv => inv.Expression is MemberAccessExpressionSyntax);

            if (!hasRiskyAccess) continue;

            var stmtLine = stmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            return WrapStatementInTryCatch(violation, stmt, stmtLine);
        }

        return null;
    }

    private static PatchResult? WrapStatementInTryCatch(
        ConstraintViolation violation, StatementSyntax statement, int targetLine)
    {
        // Extract exception type from bindings
        var exceptionType = "Exception";
        if (violation.Bindings.TryGetValue("event.exception.type", out var exTypeObj) && exTypeObj is not null)
        {
            var exTypeStr = exTypeObj.ToString()!;
            var lastDot = exTypeStr.LastIndexOf('.');
            exceptionType = lastDot >= 0 ? exTypeStr[(lastDot + 1)..] : exTypeStr;
        }

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);

        var patchedCode =
            $"{indentation}try\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {statement.ToString().Trim()}\n" +
            $"{indentation}}}\n" +
            $"{indentation}catch ({exceptionType} ex)\n" +
            $"{indentation}{{\n" +
            $"{indentation}    // AutoRepair: caught {exceptionType} at line {targetLine}\n" +
            $"{indentation}    System.Diagnostics.Debug.WriteLine($\"AutoRepair: {{ex.GetType().Name}} at {violation.File}:{targetLine}: {{ex.Message}}\");\n" +
            $"{indentation}}}";

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "ExceptionRepair",
            Description: $"Wrapped in try-catch for {exceptionType}",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
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

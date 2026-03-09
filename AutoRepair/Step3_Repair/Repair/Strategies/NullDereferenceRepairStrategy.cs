using AutoRepair.Step3Repair.Models;        // for ConstraintViolation, PatchResult
using Microsoft.CodeAnalysis;                // Roslyn base types
using Microsoft.CodeAnalysis.CSharp;         // for CSharpSyntaxTree
using Microsoft.CodeAnalysis.CSharp.Syntax;  // for MemberAccessExpressionSyntax etc.

namespace AutoRepair.Step3Repair.Strategies;

// fixes null dereference violations by inserting null-check guards
// around member accesses that could throw NullReferenceException
public sealed class NullDereferenceRepairStrategy : IRepairStrategy
{
    // only handles NotNull violations
    public bool CanHandle(ConstraintViolation violation) =>
        violation.Predicate.StartsWith("NotNull(", StringComparison.OrdinalIgnoreCase);

    public PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree)
    {
        var root = tree.GetRoot();
        var targetLine = violation.Line;

        var node = FindNodeAtLine(root, targetLine);
        if (node is null)
            return null;

        // try to find the enclosing statement on this line
        var statement = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();

        // if the violation line is a method declaration (the instrumenter logs exceptions
        // at the method entry line), we need to scan the whole method body instead
        if (statement is null)
        {
            var method = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body is null)
                return null;

            return RepairMethodBody(violation, method, targetLine);
        }

        return RepairStatement(violation, statement, targetLine);
    }

    // scans a method body to find the first statement that dereferences something
    // that might be null, then wraps it in a null check
    // skips method parameters since they are less likely to be null
    private static PatchResult? RepairMethodBody(
        ConstraintViolation violation, MethodDeclarationSyntax method, int targetLine)
    {
        if (method.Body is null)
            return null;

        // collect parameter names so we can skip them as receivers
        // parameters are usually not null in normal usage
        var paramNames = method.ParameterList.Parameters
            .Select(p => p.Identifier.Text)
            .ToHashSet();

        foreach (var stmt in method.Body.Statements)
        {
            // find all member access expressions like obj.Something
            var memberAccesses = stmt.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .ToList();

            // also check invocations on member access like obj.Method()
            var invocations = stmt.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is MemberAccessExpressionSyntax)
                .Select(inv => (MemberAccessExpressionSyntax)inv.Expression)
                .ToList();

            var allAccesses = memberAccesses.Union(invocations).ToList();
            if (allAccesses.Count == 0) continue; // no member accesses, skip

            // for each member access, find the leftmost receiver (the thing that could be null)
            // like o.GetType().GetMethod() -> the root receiver is "o"
            var receivers = allAccesses
                .Select(ma => GetRootReceiver(ma.Expression))
                .Where(r => r is not null
                    && !IsLiteral(r)               // skip "this", "base", string literals
                    && !IsTypeNameOrNamespace(r)    // skip Console, System, etc.
                    && !paramNames.Contains(r))     // skip method parameters
                .Distinct()
                .ToList();

            if (receivers.Count == 0) continue; // nothing that could be null

            var stmtLine = stmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var originalCode = stmt.ToFullString().TrimEnd();
            var indentation = GetIndentation(stmt);
            var condition = string.Join(" && ", receivers.Select(r => $"{r} is not null"));

            var patchedCode = BuildNullGuard(stmt, indentation, condition);

            return new PatchResult(
                File: violation.File,
                Line: stmtLine,
                RuleName: "NullDereferenceRepair",
                Description: $"Added null check for: {string.Join(", ", receivers)}",
                OriginalCode: originalCode,
                PatchedCode: patchedCode,
                Violation: violation);
        }

        return null; // couldnt find anything to fix
    }

    // walks the expression tree leftward to find the root identifier
    // example: o.GetType().GetMethod("blah") walks left until it finds "o"
    // example: method.Invoke(null) walks left until it finds "method"
    private static string? GetRootReceiver(ExpressionSyntax expr)
    {
        while (true)
        {
            switch (expr)
            {
                case MemberAccessExpressionSyntax ma:
                    expr = ma.Expression;   // go left
                    continue;
                case InvocationExpressionSyntax inv:
                    expr = inv.Expression;  // go left through invocations
                    continue;
                case IdentifierNameSyntax id:
                    return id.Identifier.Text; // found the root name
                default:
                    return expr.ToString(); // fallback to whatever it is
            }
        }
    }

    // handles the case where we have a direct statement (not a method body scan)
    private static PatchResult? RepairStatement(
        ConstraintViolation violation, StatementSyntax statement, int targetLine)
    {
        // find all member access expressions in this statement
        var memberAccesses = statement.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .ToList();

        // also find invocations on member accesses
        var invocations = statement.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression is MemberAccessExpressionSyntax)
            .Select(inv => (MemberAccessExpressionSyntax)inv.Expression)
            .ToList();

        var allAccesses = memberAccesses.Union(invocations).ToList();

        if (allAccesses.Count == 0)
            return null; // nothing to guard

        // get the receiver expressions and filter out ones that cant be null
        var receivers = allAccesses
            .Select(ma => ma.Expression.ToString())
            .Distinct()
            .Where(r => !IsLiteral(r) && !IsTypeNameOrNamespace(r))
            .ToList();

        if (receivers.Count == 0)
            return null;

        var originalCode = statement.ToFullString().TrimEnd();
        var indentation = GetIndentation(statement);
        var condition = string.Join(" && ", receivers.Select(r => $"{r} is not null"));

        var patchedCode = BuildNullGuard(statement, indentation, condition);

        return new PatchResult(
            File: violation.File,
            Line: targetLine,
            RuleName: "NullDereferenceRepair",
            Description: $"Added null check for: {string.Join(", ", receivers)}",
            OriginalCode: originalCode,
            PatchedCode: patchedCode,
            Violation: violation);
    }

    // builds the actual if-guard around a statement
    // special handling for local variable declarations: we need to declare the variable
    // before the if block so it stays in scope for the rest of the method
    private static string BuildNullGuard(StatementSyntax stmt, string indentation, string condition)
    {
        // special case: if the statement is a local variable declaration like "var x = obj.Something()"
        // we need to split it: declare x with a default value first, then assign inside the if
        if (stmt is LocalDeclarationStatementSyntax localDecl)
        {
            var variable = localDecl.Declaration.Variables.First();
            var typeName = localDecl.Declaration.Type.ToString();
            var varName = variable.Identifier.Text;

            // figure out a reasonable default value based on the type
            var defaultValue = typeName switch
            {
                "bool" => "false",
                "int" or "long" or "short" or "byte" or "float" or "double" or "decimal" => "0",
                "char" => "'\\0'",
                _ when typeName == "var" => "default",  // cant know the type, use default
                _ => "default"                          // reference types get default (null)
            };

            // get the right-hand side of the assignment
            var initializer = variable.Initializer?.Value.ToString() ?? "";

            // declare with default, then conditionally assign
            return
                $"{indentation}{typeName} {varName} = {defaultValue};\n" +
                $"{indentation}if ({condition})\n" +
                $"{indentation}{{\n" +
                $"{indentation}    {varName} = {initializer};\n" +
                $"{indentation}}}";
        }

        // normal case: just wrap the statement in if (x is not null) { ... }
        return
            $"{indentation}if ({condition})\n" +
            $"{indentation}{{\n" +
            $"{indentation}    {stmt.ToString().Trim()}\n" +
            $"{indentation}}}";
    }

    // checks if an expression is a literal (this, base, string, number)
    // these cant be null so we dont need to guard them
    private static bool IsLiteral(string expr) =>
        expr is "this" or "base" ||
        expr.StartsWith('"') ||
        expr.All(char.IsDigit);

    // heuristic: if it starts with uppercase and is all letters/digits/dots
    // its probably a type name or namespace (like Console, System, Array)
    // these are static and cant be null
    private static bool IsTypeNameOrNamespace(string expr) =>
        expr.Length > 0 && char.IsUpper(expr[0]) && expr.All(c => char.IsLetterOrDigit(c) || c == '.');

    // finds the biggest syntax node starting on the given line
    private static SyntaxNode? FindNodeAtLine(SyntaxNode root, int line)
    {
        return root.DescendantNodes()
            .Where(n => n.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == line)
            .OrderByDescending(n => n.Span.Length)
            .FirstOrDefault();
    }

    // extracts whitespace indentation from the leading trivia of a node
    private static string GetIndentation(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia().ToString();
        var lastNewLine = leadingTrivia.LastIndexOf('\n');
        return lastNewLine >= 0 ? leadingTrivia[(lastNewLine + 1)..] : leadingTrivia;
    }
}

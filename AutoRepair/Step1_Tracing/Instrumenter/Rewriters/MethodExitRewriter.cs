using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Method Exit
    //
    // ControlFlowRewriter already records method entry. Nothing recorded the matching exit: the
    // existing "Return" event only fires on an explicit `return` statement, so a void method that
    // falls off the end, or any method an exception propagates out of, was never recorded as left.
    // A trace could therefore show a function being entered and never leaving.
    //
    // This rewriter closes the pair by wrapping the body in try/finally. `finally` is the only
    // construct that runs on every exit path - explicit return, fall-through, and in-flight
    // exception - so the exit event cannot be missed. A catch/rethrow alongside it records *why*
    // the method was left ("normal" vs "exception") without altering behaviour.
    //
    // It MUST run last in the rewriter pipeline. Running earlier would let the other rewriters
    // instrument the handler this one injects, producing events that describe the instrumenter
    // rather than the program under test.
    public class MethodExitRewriter : TraceRewriterBase
    {
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Visit children first so nested local functions are wrapped before this body is,
            // and so nothing this pass injects is ever itself revisited.
            var updated = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
            if (updated == null || updated.Body == null) return node;

            // `return ref x;` is illegal inside a try block.
            if (updated.ReturnType is RefTypeSyntax) return updated;

            var body = WrapBody(updated.Body, node.Identifier.Text, GetLineNumber(node));
            return body == null ? updated : updated.WithBody(body);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var updated = base.VisitConstructorDeclaration(node) as ConstructorDeclarationSyntax;
            if (updated == null || updated.Body == null) return node;

            var body = WrapBody(updated.Body, node.Identifier.Text, GetLineNumber(node));
            return body == null ? updated : updated.WithBody(body);
        }

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var updated = base.VisitLocalFunctionStatement(node) as LocalFunctionStatementSyntax;
            if (updated == null || updated.Body == null) return node;

            if (updated.ReturnType is RefTypeSyntax) return updated;

            var body = WrapBody(updated.Body, node.Identifier.Text, GetLineNumber(node));
            return body == null ? updated : updated.WithBody(body);
        }

        // Returns the rewritten body, or null to leave the member untouched.
        private BlockSyntax? WrapBody(BlockSyntax body, string methodName, int line)
        {
            var statements = body.Statements;

            // Count the entry/parameter preamble ControlFlowRewriter put at the top.
            int prefix = 0;
            while (prefix < statements.Count && statements[prefix].HasAnnotation(MethodPreamble))
            {
                prefix++;
            }

            // No entry event means no pair to complete. Leaving the body alone keeps the
            // call-depth counter balanced: only an upgraded LogEnter increments it.
            int entryIndex = -1;
            for (int i = 0; i < prefix; i++)
            {
                if (statements[i].HasAnnotation(MethodEntryLog))
                {
                    entryIndex = i;
                    break;
                }
            }
            if (entryIndex < 0) return null;

            var preamble = statements.Take(prefix).ToList();
            preamble[entryIndex] = CreateEnterLog(methodName, line);

            var guarded = statements.Skip(prefix).ToList();

            // Nothing but the preamble: emit the pair directly, no try needed.
            if (guarded.Count == 0)
            {
                preamble.Add(CreateExitLog(methodName, line, "\"normal\""));
                return body.WithStatements(SyntaxFactory.List(preamble));
            }

            // Each wrapper needs its own reason variable: a local function body is a nested scope,
            // so reusing one name would collide with the enclosing method's (CS0136).
            var reasonVar = "__autoRepairExit_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var tryStatement = SyntaxFactory.TryStatement()
                .WithBlock(SyntaxFactory.Block(guarded))
                .WithFinally(SyntaxFactory.FinallyClause(
                    SyntaxFactory.Block(CreateExitLog(methodName, line, reasonVar))));

            // C# forbids a catch clause in a try block containing `yield return` / `yield break`,
            // so an iterator gets the finally only. Its exit is still always recorded; the reason
            // just stays "normal" because there is no handler to observe an exception from.
            if (!ContainsYield(SyntaxFactory.Block(guarded)))
            {
                tryStatement = tryStatement.WithCatches(
                    SyntaxFactory.SingletonList(CreateReasonCatchClause(reasonVar)));
            }

            var newStatements = preamble
                .Append(SyntaxFactory.ParseStatement($"string {reasonVar} = \"normal\";\n"))
                .Append(tryStatement);

            return body.WithStatements(SyntaxFactory.List(newStatements));
        }

        // catch (System.Exception) { reason = "exception"; throw; }
        //
        // `throw;` rethrows preserving the original stack trace, so this observes the exception
        // without handling it - the method still fails exactly as it did before instrumentation.
        private static CatchClauseSyntax CreateReasonCatchClause(string reasonVar)
        {
            var template = SyntaxFactory.ParseStatement("try { } catch (System.Exception) { }") as TryStatementSyntax;
            if (template == null || template.Catches.Count == 0)
            {
                throw new InvalidOperationException("Failed to create catch template.");
            }

            return template.Catches[0].WithBlock(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement($"{reasonVar} = \"exception\";\n"),
                SyntaxFactory.ParseStatement("throw;\n")));
        }

        private StatementSyntax CreateEnterLog(string methodName, int line)
        {
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.LogEnter(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {line}, \"{EscapeLiteral(methodName)}\");\n");
        }

        // reasonExpr is either a quoted literal or the name of the reason local.
        private StatementSyntax CreateExitLog(string methodName, int line, string reasonExpr)
        {
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.LogExit(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {line}, \"{EscapeLiteral(methodName)}\", {reasonExpr});\n");
        }

        private static string EscapeLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool ContainsYield(SyntaxNode node)
        {
            // Don't descend into nested function-like bodies - yield-affinity is per-method.
            foreach (var descendant in node.DescendantNodes(d =>
                d is BlockSyntax || d is StatementSyntax || d is ExpressionSyntax))
            {
                if (descendant is YieldStatementSyntax) return true;
            }
            return false;
        }
    }
}

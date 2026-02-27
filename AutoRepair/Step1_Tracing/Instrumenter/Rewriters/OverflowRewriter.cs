using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{

    // ── FUTURE WORK: Narrowing-conversion overflow ──────────────────────────
    // This rewriter currently covers arithmetic overflow (+, -, *, +=, -=, *=)
    // by wrapping operations in checked() blocks.
    //
    // NOT covered: narrowing / truncation conversions such as:
    //   long bigValue = long.MaxValue;  int x = (int)bigValue;
    // These do not throw OverflowException in an unchecked context and silently
    // truncate.  
    // 
    // Jyoti mentioned this is intentional:
    //   "For assignment truncation, we are likely to run into many false
    //    positives as this is an intended developer behaviour."
    //
    // If we ever need to detect truncation (e.g. opt-in mode):
    //   1. In VisitCastExpression, detect narrowing numeric casts (e.g. long→int,
    //      int→short, double→float) using the semantic model.
    //   2. Replace them with a TraceLogger.LogNarrowingCast(id, file, line,
    //      sourceValue, targetType) that wraps the cast in checked() and logs
    //      when OverflowException is thrown.
    //   3. Add a corresponding NarrowingOverflowRule in Step2_Constraints.
    // ─────────────────────────────────────────────────────────────────────────
    public class OverflowRewriter : TraceRewriterBase
    {
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (base.VisitBinaryExpression(node) is not BinaryExpressionSyntax updatedNode) return node;

            // Instrument +, -, *
            string? op = updatedNode.Kind() switch
            {
                SyntaxKind.AddExpression => "+",
                SyntaxKind.SubtractExpression => "-",
                SyntaxKind.MultiplyExpression => "*",
                _ => null
            };

            if (op == null) return updatedNode;

            var leftStr = updatedNode.Left.ToString();
            var rightStr = updatedNode.Right.ToString();

            if (leftStr.Contains("TraceLogger") || rightStr.Contains("TraceLogger"))
                return updatedNode;

            if (IsLikelyStringExpression(updatedNode.Left) || IsLikelyStringExpression(updatedNode.Right))
                return updatedNode;

            if (IsInsideCheckedOrUnchecked(node))
                return updatedNode;

            var line = GetLineNumber(node);
            var uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var escapedOp = EscapeLiteral(op);
            var code = $"global::AutoRepairTracing.TraceLogger.LogCheckedBinary(" +
                       $"\"{uniqueId}\", \"{escapedFileName}\", {line}, " +
                       $"{leftStr}, {rightStr}, \"{escapedOp}\")";

            return SyntaxFactory.ParseExpression(code);
        }

        // Compound assignments: +=, -=, *=
        public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (base.VisitAssignmentExpression(node) is not AssignmentExpressionSyntax updatedNode) return node;

            string? op = updatedNode.Kind() switch
            {
                SyntaxKind.AddAssignmentExpression => "+",
                SyntaxKind.SubtractAssignmentExpression => "-",
                SyntaxKind.MultiplyAssignmentExpression => "*",
                _ => null
            };

            if (op == null) return updatedNode;

            var leftStr = updatedNode.Left.ToString();
            var rightStr = updatedNode.Right.ToString();
            if (leftStr.Contains("TraceLogger") || rightStr.Contains("TraceLogger"))
                return updatedNode;

            if (IsLikelyStringExpression(updatedNode.Left) || IsLikelyStringExpression(updatedNode.Right))
                return updatedNode;

            if (IsInsideCheckedOrUnchecked(node))
                return updatedNode;

            var line = GetLineNumber(node);
            var uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var code = $"global::AutoRepairTracing.TraceLogger.LogCheckedBinary(" +
                       $"\"{uniqueId}\", \"{escapedFileName}\", {line}, " +
                       $"{leftStr}, {rightStr}, \"{op}\")";

            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                updatedNode.Left,
                SyntaxFactory.ParseExpression(code));
        }

        private static bool IsLikelyStringExpression(ExpressionSyntax expr)
        {
            // String literals: "hello Manish"
            if (expr is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
                return true;

            // Interpolated strings: $"..."
            if (expr is InterpolatedStringExpressionSyntax)
                return true;

            // String.Concat, string.Format, ..
            var text = expr.ToString();
            if (text.StartsWith("string.", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("String.", StringComparison.Ordinal))
                return true;

            return false;
        }

        private static bool IsInsideCheckedOrUnchecked(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is CheckedExpressionSyntax || parent is CheckedStatementSyntax)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        private static string EscapeLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

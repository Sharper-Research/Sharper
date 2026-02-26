using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    /// <summary>
    /// Handles:
    ///   1. Explicit casts  –> (T)expr   ->  (T)LogCastSource(id, file, line, (object)(expr), "T")
    ///   2. "as" casts      –> expr as T ->  LogAsCast&lt;T&gt;(id, file, line, (object)(expr), "T")
    ///
    /// Numeric/value-type predefined casts (int, double, etc.) are skipped because boxing
    /// the source would change the semantics of numeric conversions.  Reference-type predefined
    /// casts (string, object) ARE instrumented.
    /// </summary>

    public class CastingRewriter : TraceRewriterBase
    {
        // - 1. Explicit casts
        public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
        {
            var updatedNode = base.VisitCastExpression(node) as CastExpressionSyntax;
            if (updatedNode == null) return node;

            if (ShouldSkipCast(updatedNode.Type))
                return updatedNode;

            var line = GetLineNumber(node);
            var targetType = updatedNode.Type.ToString();
            var sourceExpr = updatedNode.Expression.ToString();

            if (sourceExpr.Contains("TraceLogger"))
                return updatedNode;

            var uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var escapedTargetType = EscapeLiteral(targetType);

            var code = $"({targetType})global::AutoRepairTracing.TraceLogger.LogCastSource(" +
                       $"\"{uniqueId}\", \"{escapedFileName}\", {line}, " +
                       $"(object)({sourceExpr}), \"{escapedTargetType}\")";

            return SyntaxFactory.ParseExpression(code);
        }

        // 2. "as" casts: expr as T
        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var updatedNode = base.VisitBinaryExpression(node) as BinaryExpressionSyntax;
            if (updatedNode == null) return node;

            if (!updatedNode.IsKind(SyntaxKind.AsExpression))
                return updatedNode;


            if (updatedNode.Right is NullableTypeSyntax)
                return updatedNode;

            var line = GetLineNumber(node);
            var targetType = updatedNode.Right.ToString();
            var sourceExpr = updatedNode.Left.ToString();

            if (sourceExpr.Contains("TraceLogger"))
                return updatedNode;

            var uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var escapedTargetType = EscapeLiteral(targetType);

            var code = $"global::AutoRepairTracing.TraceLogger.LogAsCast<{targetType}>(" +
                       $"\"{uniqueId}\", \"{escapedFileName}\", {line}, " +
                       $"(object)({sourceExpr}), \"{escapedTargetType}\")";

            return SyntaxFactory.ParseExpression(code);
        }

        private static bool ShouldSkipCast(TypeSyntax type)
        {
            if (type is PredefinedTypeSyntax predefined)
            {
                if (predefined.Keyword.IsKind(SyntaxKind.StringKeyword) ||
                    predefined.Keyword.IsKind(SyntaxKind.ObjectKeyword))
                    return false;
                return true;
            }

            return false;
        }

        private static string EscapeLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

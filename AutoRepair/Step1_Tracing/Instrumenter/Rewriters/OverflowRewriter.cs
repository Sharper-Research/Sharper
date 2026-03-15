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

            // Skip when either operand is a char literal — the '+' is likely
            // string/char concatenation, not arithmetic (e.g. '?' + value).
            if (IsLikelyCharConcatenation(updatedNode.Left, updatedNode.Right))
                return updatedNode;

            // Skip enum arithmetic — LogCheckedBinary's generic catch-all
            // returns TLeft instead of the underlying type, which breaks
            // expressions like (E - E) that should produce int.
            if (IsLikelyEnumOperation(updatedNode.Left, updatedNode.Right, node))
                return updatedNode;

            if (IsInsideCheckedOrUnchecked(node))
                return updatedNode;

            if (IsInsideConstantContext(node))
                return updatedNode;

            // Skip pointer arithmetic (pointer types cannot be generic type arguments)
            if (IsInsideUnsafeContext(node))
                return updatedNode;

            // Skip when either operand contains an out-variable declaration
            // (out var x) — wrapping in LogCheckedBinary would put the
            // declaration in an argument context where it's not allowed (CS8185).
            if (updatedNode.DescendantNodes().OfType<DeclarationExpressionSyntax>().Any())
                return updatedNode;

            // Skip DateTime/TimeSpan subtraction — the result is TimeSpan,
            // not a numeric type, and wrapping breaks member access like .TotalSeconds (CS1061).
            if (op == "-" && (IsLikelyDateTimeExpression(updatedNode.Left) || IsLikelyDateTimeExpression(updatedNode.Right)))
                return updatedNode;

            var line = GetLineNumber(node);
            var uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\\\", "\\\\\\\\");
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

            if (IsLikelyCharConcatenation(updatedNode.Left, updatedNode.Right))
                return updatedNode;

            if (IsLikelyEnumOperation(updatedNode.Left, updatedNode.Right, node))
                return updatedNode;

            if (IsInsideCheckedOrUnchecked(node))
                return updatedNode;

            if (IsInsideConstantContext(node))
                return updatedNode;

            if (IsInsideUnsafeContext(node))
                return updatedNode;

            // Skip when RHS contains an out-variable declaration (CS8185).
            if (updatedNode.Right.DescendantNodesAndSelf()
                .OfType<DeclarationExpressionSyntax>().Any())
                return updatedNode;

            // Skip compound assignments where the target type is narrower than int
            // (LogCheckedBinary returns int for byte/sbyte/short/ushort) or where
            // unsigned/signed mismatch causes overload ambiguity (ulong += int).
            if (IsLikelyNarrowTypedCompound(updatedNode.Left, node))
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

            // Method calls that return strings: .ToString(), .Substring(), etc.
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is MemberAccessExpressionSyntax ma)
            {
                var methodName = ma.Name.Identifier.Text;
                if (methodName == "ToString" || methodName == "Substring"
                    || methodName == "Trim" || methodName == "ToLower"
                    || methodName == "ToUpper" || methodName == "Replace"
                    || methodName == "Format" || methodName == "Concat"
                    || methodName == "Join" || methodName == "PadLeft"
                    || methodName == "PadRight")
                    return true;
            }

            // Variable whose declaration type is 'string'
            if (expr is IdentifierNameSyntax id)
            {
                var current = expr.Parent;
                while (current != null)
                {
                    if (current is BlockSyntax block)
                    {
                        foreach (var stmt in block.Statements)
                        {
                            if (stmt is LocalDeclarationStatementSyntax localDecl)
                            {
                                foreach (var variable in localDecl.Declaration.Variables)
                                {
                                    if (variable.Identifier.Text == id.Identifier.Text)
                                    {
                                        var typeText = localDecl.Declaration.Type.ToString();
                                        return typeText == "string" || typeText == "String" || typeText == "System.String";
                                    }
                                }
                            }
                        }
                    }
                    if (current is BaseMethodDeclarationSyntax method)
                    {
                        foreach (var param in method.ParameterList.Parameters)
                        {
                            if (param.Identifier.Text == id.Identifier.Text && param.Type != null)
                            {
                                var typeText = param.Type.ToString();
                                return typeText == "string" || typeText == "String" || typeText == "System.String";
                            }
                        }
                    }
                    if (current is TypeDeclarationSyntax) break;
                    current = current.Parent;
                }
            }

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

        /// <summary>
        /// Returns true when the node is inside a constant context (const field,
        /// const local, enum member, attribute argument, etc.) where runtime
        /// calls are not allowed.
        /// </summary>
        private static bool IsInsideConstantContext(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is FieldDeclarationSyntax fieldDecl && fieldDecl.Modifiers.Any(SyntaxKind.ConstKeyword))
                    return true;
                if (parent is LocalDeclarationStatementSyntax localDecl && localDecl.Modifiers.Any(SyntaxKind.ConstKeyword))
                    return true;
                if (parent is EnumMemberDeclarationSyntax)
                    return true;
                if (parent is AttributeArgumentSyntax)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>
        /// Returns true when the node is inside an unsafe block or method.
        /// Pointer arithmetic cannot be passed as generic type arguments.
        /// </summary>
        private static bool IsInsideUnsafeContext(SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is UnsafeStatementSyntax)
                    return true;
                if (parent is BaseMethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (parent is TypeDeclarationSyntax typeDecl && typeDecl.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>
        /// Returns true when either operand is a char literal, indicating
        /// the '+' is string/char concatenation rather than arithmetic.
        /// </summary>
        private static bool IsLikelyCharConcatenation(ExpressionSyntax left, ExpressionSyntax right)
        {
            return (left is LiteralExpressionSyntax l && l.IsKind(SyntaxKind.CharacterLiteralExpression))
                || (right is LiteralExpressionSyntax r && r.IsKind(SyntaxKind.CharacterLiteralExpression));
        }

        private static string EscapeLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Returns true when either operand of a binary or compound-assignment
        /// expression appears to be enum-typed.  Enum values do not have
        /// implicit conversions to int, so LogCheckedBinary's specific
        /// overloads won't match; the generic catch-all would change the
        /// result type.
        /// </summary>
        private static bool IsLikelyEnumOperation(ExpressionSyntax left, ExpressionSyntax right, SyntaxNode original)
        {
            return IsLikelyEnumExpression(left, original) || IsLikelyEnumExpression(right, original);
        }

        private static bool IsLikelyEnumExpression(ExpressionSyntax expr, SyntaxNode context)
        {
            // Member access like EnumType.Value (e.g. RegexNodeKind.One)
            // or obj.EnumProperty (e.g. node.Kind)
            if (expr is MemberAccessExpressionSyntax memberAccess)
            {
                // Check the base type name: RegexNodeKind.Value
                if (memberAccess.Expression is IdentifierNameSyntax typeId
                    && LooksLikeEnumTypeName(typeId.Identifier.Text))
                    return true;
                // Check the member name: node.Kind, node.Options, etc.
                if (LooksLikeEnumTypeName(memberAccess.Name.Identifier.Text))
                    return true;
            }

            // Variable/parameter whose declaration type looks like an enum
            if (expr is IdentifierNameSyntax identifier)
            {
                var varName = identifier.Identifier.Text;
                // First check the name itself — properties like 'Kind' are likely enums
                if (LooksLikeEnumTypeName(varName))
                    return true;
                var declType = FindLocalDeclType(varName, context);
                if (declType != null && LooksLikeEnumTypeName(declType))
                    return true;
            }

            return false;
        }

        private static string? FindLocalDeclType(string varName, SyntaxNode context)
        {
            var current = context.Parent;
            while (current != null)
            {
                if (current is BlockSyntax block)
                {
                    foreach (var stmt in block.Statements)
                    {
                        if (stmt is LocalDeclarationStatementSyntax localDecl)
                        {
                            foreach (var variable in localDecl.Declaration.Variables)
                            {
                                if (variable.Identifier.Text == varName)
                                    return localDecl.Declaration.Type.ToString();
                            }
                        }
                    }
                }
                if (current is BaseMethodDeclarationSyntax method)
                {
                    foreach (var param in method.ParameterList.Parameters)
                    {
                        if (param.Identifier.Text == varName && param.Type != null)
                            return param.Type.ToString();
                    }
                }
                if (current is LocalFunctionStatementSyntax localFunc)
                {
                    foreach (var param in localFunc.ParameterList.Parameters)
                    {
                        if (param.Identifier.Text == varName && param.Type != null)
                            return param.Type.ToString();
                    }
                }
                if (current is TypeDeclarationSyntax) break;
                current = current.Parent;
            }
            return null;
        }

        private static bool LooksLikeEnumTypeName(string typeName)
        {
            return typeName.EndsWith("Kind") || typeName.EndsWith("Flags")
                || typeName.EndsWith("Options") || typeName.EndsWith("Mode")
                || typeName.EndsWith("State") || typeName.EndsWith("Style")
                || typeName.EndsWith("Status") || typeName.EndsWith("Level")
                || typeName.EndsWith("Category") || typeName.EndsWith("Enum");
        }

        /// <summary>
        /// Returns true when the target of a compound assignment (+=, -=, *=)
        /// is a small integer type whose LogCheckedBinary overload returns int
        /// (causing CS0266), or an unsigned type that causes overload ambiguity
        /// with signed operands (CS0034).
        /// </summary>
        private static bool IsLikelyNarrowTypedCompound(ExpressionSyntax left, SyntaxNode context)
        {
            string? declType = null;

            if (left is IdentifierNameSyntax id)
            {
                declType = FindLocalDeclType(id.Identifier.Text, context);
            }
            else if (left is ElementAccessExpressionSyntax el)
            {
                // For arr[i] compound assignments, check the array's declared type
                if (el.Expression is IdentifierNameSyntax arrId)
                {
                    var arrType = FindLocalDeclType(arrId.Identifier.Text, context);
                    if (arrType != null)
                        declType = arrType.Replace("[]", "").Trim();
                }
            }
            else if (left is MemberAccessExpressionSyntax)
            {
                // Can't determine field types without semantic model — skip
                return false;
            }

            if (declType == null) return false;
            return declType is "byte" or "Byte" or "sbyte" or "SByte"
                or "short" or "Int16" or "ushort" or "UInt16"
                or "ulong" or "UInt64";
        }

        /// <summary>
        /// Returns true when the expression looks like a DateTime or TimeSpan value.
        /// DateTime subtraction yields TimeSpan, not a numeric type, so wrapping it
        /// in LogCheckedBinary would change the result type and break member access
        /// like .TotalSeconds (CS1061).
        /// </summary>
        private static bool IsLikelyDateTimeExpression(ExpressionSyntax expr)
        {
            var text = expr.ToString();
            if (text.Contains("DateTime") || text.Contains("DateTimeOffset")
                || text.Contains("TimeSpan") || text.Contains("DateOnly") || text.Contains("TimeOnly"))
                return true;
            // Common DateTime properties/methods
            if (text.EndsWith(".UtcNow") || text.EndsWith(".Now") || text.EndsWith(".Today")
                || text.EndsWith(".Date") || text.EndsWith(".Ticks"))
                return true;
            // Variable whose declared type is DateTime/TimeSpan
            if (expr is IdentifierNameSyntax id)
            {
                var declType = FindLocalDeclType(id.Identifier.Text, expr);
                if (declType != null && (declType.Contains("DateTime") || declType.Contains("TimeSpan")
                    || declType.Contains("DateOnly") || declType.Contains("TimeOnly")))
                    return true;
            }
            return false;
        }
    }
}

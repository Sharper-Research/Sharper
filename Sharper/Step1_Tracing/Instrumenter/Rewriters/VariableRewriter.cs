using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Variable Values
    // Traces: assignments, ++/--, locals, index access, denominators.
    public class VariableRewriter : TraceRewriterBase
    {
        private ExpressionSyntax CreateValueLogExpression(string varName, ExpressionSyntax valueExpr, int line)
        {
            var literal = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(varName));
            return CreateValueLogExpression(literal, valueExpr, line);
        }

        private ExpressionSyntax CreateValueLogExpression(ExpressionSyntax varNameExpression, ExpressionSyntax valueExpr, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var valueCode = valueExpr.ToString();
            var varNameCode = varNameExpression.ToString();
            var code = $"global::SharperTracing.TraceLogger.LogValue(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Variable Update\", {valueCode}, {varNameCode})";
            return SyntaxFactory.ParseExpression(code);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var shouldSnapshotArray = node.Expression is AssignmentExpressionSyntax
            {
                Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver }
            } ? receiver.ToString() : null;

            var visitedNode = base.VisitExpressionStatement(node) as ExpressionStatementSyntax;
            if (visitedNode == null) return node;

            if (shouldSnapshotArray != null)
            {
                return SyntaxFactory.Block(visitedNode, CreateArraySnapshotStatement(shouldSnapshotArray, line));
            }

            return visitedNode;
        }

        public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // Don't instrument assignments inside object/collection initializers
            if (node.Parent is InitializerExpressionSyntax)
                return base.VisitAssignmentExpression(node);

            // Skip event handler += / -= (right side is delegate/lambda — returns void, can't wrap in LogValue<T>)
            if ((node.IsKind(SyntaxKind.AddAssignmentExpression) || node.IsKind(SyntaxKind.SubtractAssignmentExpression)) &&
                (node.Right is AnonymousMethodExpressionSyntax || node.Right is LambdaExpressionSyntax || node.Right is ParenthesizedLambdaExpressionSyntax))
                return base.VisitAssignmentExpression(node);

            // Also skip += / -= on member access left sides (event subscriptions like obj.Event += handler)
            // — these return void and cannot be wrapped in LogValue<T>.
            if ((node.IsKind(SyntaxKind.AddAssignmentExpression) || node.IsKind(SyntaxKind.SubtractAssignmentExpression)) &&
                node.Left is MemberAccessExpressionSyntax)
                return base.VisitAssignmentExpression(node);

            var line = GetLineNumber(node);
            var updatedNode = base.VisitAssignmentExpression(node) as AssignmentExpressionSyntax;
            if (updatedNode == null) return node;

            // A label must never re-evaluate the assignment target (for example list[i++]).
            var varName = node.Left.ToString();
            return CreateValueLogExpression(varName, updatedNode, line);
        }

        // Places where C# requires a compile-time constant; a logging call there would not compile.
        private static bool IsInConstantContext(SyntaxNode node) =>
            node.Ancestors().Any(ancestor => ancestor is AttributeArgumentSyntax or ParameterSyntax
                or EnumMemberDeclarationSyntax or SwitchLabelSyntax or PatternSyntax
                || ancestor is FieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.ConstKeyword)
                || ancestor is LocalDeclarationStatementSyntax local && local.IsConst);

        public override SyntaxNode? VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var updated = (ElementAccessExpressionSyntax)base.VisitElementAccessExpression(node)!;
            if (IsInConstantContext(node)) return updated;
            var arguments = updated.ArgumentList.Arguments.Select(argument =>
                argument.WithExpression(CreateValueLogExpression(
                    $"index:{node.Expression}", argument.Expression, GetLineNumber(node))));
            return updated.WithArgumentList(updated.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(arguments)));
        }

        public override SyntaxNode? VisitInterpolation(InterpolationSyntax node)
        {
            // Inside $"{...}" a ':' starts a format specifier, so wrap rewritten holes (global::...) in parentheses.
            var updated = (InterpolationSyntax)base.VisitInterpolation(node)!;
            return updated.Expression.ToString() == node.Expression.ToString() || updated.Expression is ParenthesizedExpressionSyntax
                ? updated
                : updated.WithExpression(SyntaxFactory.ParenthesizedExpression(updated.Expression));
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var updated = (BinaryExpressionSyntax)base.VisitBinaryExpression(node)!;
            return node.IsKind(SyntaxKind.DivideExpression) && !IsInConstantContext(node)
                ? updated.WithRight(CreateValueLogExpression("denominator", updated.Right, GetLineNumber(node)))
                : updated;
        }

        public override SyntaxNode? VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            var updatedNode = base.VisitPrefixUnaryExpression(node) as PrefixUnaryExpressionSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.IsKind(SyntaxKind.PreIncrementExpression) || updatedNode.IsKind(SyntaxKind.PreDecrementExpression))
            {
                var line = GetLineNumber(node);
                var varName = updatedNode.Operand.ToString();
                return CreateValueLogExpression(varName, updatedNode, line);
            }

            return updatedNode;
        }

        public override SyntaxNode? VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            var updatedNode = base.VisitPostfixUnaryExpression(node) as PostfixUnaryExpressionSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.IsKind(SyntaxKind.PostIncrementExpression) || updatedNode.IsKind(SyntaxKind.PostDecrementExpression))
            {
                var line = GetLineNumber(node);
                var varName = updatedNode.Operand.ToString();
                return CreateValueLogExpression(varName, updatedNode, line);
            }

            return updatedNode;
        }

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var newStatements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                var visited = Visit(statement) as StatementSyntax;
                if (visited != null)
                {
                    newStatements.Add(visited);

                    // 11. Local Declarations (int x = 5)
                    if (statement is LocalDeclarationStatementSyntax localDecl)
                    {
                        var line = GetLineNumber(localDecl);
                        foreach (var variable in localDecl.Declaration.Variables)
                        {
                            if (variable.Initializer != null)
                            {
                                var log = CreateVariableLogStatement(variable.Identifier.Text, variable.Identifier.Text, line);
                                newStatements.Add(log);
                            }
                        }
                    }
                }
            }
            return node.WithStatements(SyntaxFactory.List(newStatements));
        }

        public override SyntaxNode? VisitSwitchSection(SwitchSectionSyntax node)
        {
            var newStatements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                var visited = Visit(statement) as StatementSyntax;
                if (visited != null)
                {
                    newStatements.Add(visited);

                    // 11. Local Declarations (int x = 5)
                    if (statement is LocalDeclarationStatementSyntax localDecl)
                    {
                        var line = GetLineNumber(localDecl);
                        foreach (var variable in localDecl.Declaration.Variables)
                        {
                            if (variable.Initializer != null)
                            {
                                var log = CreateVariableLogStatement(variable.Identifier.Text, variable.Identifier.Text, line);
                                newStatements.Add(log);
                            }
                        }
                    }
                }
            }
            return node.WithStatements(SyntaxFactory.List(newStatements));
        }

        private StatementSyntax CreateArraySnapshotStatement(string arrayExpression, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var code = $"global::SharperTracing.TraceLogger.LogArray(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Array Snapshot\", {arrayExpression}, \"{arrayExpression}\");";
            return SyntaxFactory.ParseStatement(code + "\n");
        }
    }
}

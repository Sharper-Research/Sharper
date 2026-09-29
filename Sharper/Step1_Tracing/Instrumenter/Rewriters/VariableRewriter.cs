using System;
using System.Collections.Generic;
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
            var shouldSnapshotArray = node.Expression is AssignmentExpressionSyntax originalAssignment &&
                                      originalAssignment.Left is ElementAccessExpressionSyntax elementAccessOriginal
                                          ? elementAccessOriginal.Expression.ToString()
                                          : null;

            var visitedNode = base.VisitExpressionStatement(node) as ExpressionStatementSyntax;
            if (visitedNode == null) return node;

            string? varName = null;
            bool isDirectUpdate = false;
            var logsBefore = new List<StatementSyntax>();
            var logsAfter = new List<StatementSyntax>();

            // 12. Assignments (x = 5;)
            // 14. Compound Assignments (+=)
            if (visitedNode.Expression is AssignmentExpressionSyntax assignment)
            {
                varName = assignment.Left.ToString();
                isDirectUpdate = true;
            }
            // 13. Post/Pre Increment/Decrement (i++)
            else if (visitedNode.Expression is PostfixUnaryExpressionSyntax postfix)
            {
                varName = postfix.Operand.ToString();
                isDirectUpdate = true;
            }
            else if (visitedNode.Expression is PrefixUnaryExpressionSyntax prefix)
            {
                if (prefix.Kind() == SyntaxKind.PreIncrementExpression || prefix.Kind() == SyntaxKind.PreDecrementExpression)
                {
                    varName = prefix.Operand.ToString();
                    isDirectUpdate = true;
                }
            }

            if (varName != null && !isDirectUpdate)
            {
                logsAfter.Add(CreateVariableLogStatement(varName, varName, line));
            }

            // Index expressions (arr[i])
            var elementAccesses = visitedNode.Expression
                .DescendantNodes()
                .OfType<ElementAccessExpressionSyntax>();

            foreach (var elementAccess in elementAccesses)
            {
                foreach (var arg in elementAccess.ArgumentList.Arguments)
                {
                    string indexName = $"index:{elementAccess.Expression}";
                    logsBefore.Add(CreateVariableLogStatement(indexName, arg.Expression.ToString(), line));
                }
            }

            // Denominators before division
            var divisions = visitedNode.Expression
                .DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .Where(b => b.IsKind(SyntaxKind.DivideExpression));

            foreach (var division in divisions)
            {
                logsBefore.Add(CreateVariableLogStatement("denominator", division.Right.ToString(), line));
            }

            if (shouldSnapshotArray != null)
            {
                logsAfter.Add(CreateArraySnapshotStatement(shouldSnapshotArray, line));
            }

            if (logsBefore.Count > 0 || logsAfter.Count > 0)
            {
                var statements = new List<StatementSyntax>();
                statements.AddRange(logsBefore);
                statements.Add(visitedNode);
                statements.AddRange(logsAfter);
                return SyntaxFactory.Block(statements);
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

            if (updatedNode.Left is ElementAccessExpressionSyntax elementAccess)
            {
                var varNameExpr = BuildIndexedNameExpression(elementAccess);
                return CreateValueLogExpression(varNameExpr, updatedNode, line);
            }

            var varName = updatedNode.Left.ToString();
            return CreateValueLogExpression(varName, updatedNode, line);
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

        private ExpressionSyntax BuildIndexedNameExpression(ElementAccessExpressionSyntax elementAccess)
        {
            var baseName = elementAccess.Expression.ToString();
            var indexExpressions = elementAccess.ArgumentList.Arguments
                .Select(arg => arg.Expression.ToString())
                .ToList();

            if (indexExpressions.Count == 0)
            {
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(baseName));
            }

            var concatArgs = new List<string>
            {
                $"\"{baseName}[\""
            };

            for (int i = 0; i < indexExpressions.Count; i++)
            {
                concatArgs.Add(indexExpressions[i]);
                if (i < indexExpressions.Count - 1)
                {
                    concatArgs.Add("\",\""); // separator between indices
                }
            }

            concatArgs.Add("\"]\"");
            var expression = $"string.Concat({string.Join(", ", concatArgs)})";
            return SyntaxFactory.ParseExpression(expression);
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

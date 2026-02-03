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
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var valueCode = valueExpr.ToString();
            var code = $"global::AutoRepairTracing.TraceLogger.LogValue(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Variable Update\", {valueCode}, \"{varName}\")";
            return SyntaxFactory.ParseExpression(code);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var line = GetLineNumber(node);
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
            var line = GetLineNumber(node);
            var updatedNode = base.VisitAssignmentExpression(node) as AssignmentExpressionSyntax;
            if (updatedNode == null) return node;

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
    }
}
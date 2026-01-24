using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Step 2: Variable Values
    public class VariableRewriter : TraceRewriterBase
    {
        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var visitedNode = base.VisitExpressionStatement(node) as ExpressionStatementSyntax;
            if (visitedNode == null) return node;

            string? varName = null;

            // 12. Assignments (x = 5;)
            // 14. Compound Assignments (+=)
            if (visitedNode.Expression is AssignmentExpressionSyntax assignment)
            {
                varName = assignment.Left.ToString();
            }
            // 13. Post/Pre Increment/Decrement (i++)
            else if (visitedNode.Expression is PostfixUnaryExpressionSyntax postfix)
            {
                varName = postfix.Operand.ToString();
            }
            else if (visitedNode.Expression is PrefixUnaryExpressionSyntax prefix)
            {
                if (prefix.Kind() == SyntaxKind.PreIncrementExpression || prefix.Kind() == SyntaxKind.PreDecrementExpression)
                {
                    varName = prefix.Operand.ToString();
                }
            }

            if (varName != null)
            {
                var log = CreateVariableLogStatement(varName, varName, line);
                return SyntaxFactory.Block(visitedNode, log);
            }

            return visitedNode;
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
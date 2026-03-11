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
            var code = $"global::AutoRepairTracing.TraceLogger.LogValue(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Variable Update\", {valueCode}, {varNameCode})";
            return SyntaxFactory.ParseExpression(code);
        }

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var visitedNode = base.VisitExpressionStatement(node) as ExpressionStatementSyntax;
            if (visitedNode == null) return node;

            var logsBefore = new List<StatementSyntax>();

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

            if (logsBefore.Count > 0)
            {
                var statements = new List<StatementSyntax>();
                statements.AddRange(logsBefore);
                statements.Add(visitedNode);
                return SyntaxFactory.Block(statements);
            }

            return visitedNode;
        }

        public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // Don't wrap assignments that are members of object/collection initializers.
            // e.g. new Option { Description = "..." } — instrumenting those turns a
            // valid object initializer into broken collection-initializer syntax.
            if (node.Parent is InitializerExpressionSyntax)
                return base.VisitAssignmentExpression(node);

            var line = GetLineNumber(node);
            var updatedNode = base.VisitAssignmentExpression(node) as AssignmentExpressionSyntax;
            if (updatedNode == null) return node;

            // Skip destructuring assignments: var (_, distance) = tuple
            // The left side is a DeclarationExpressionSyntax which is not a valid
            // standalone expression — wrapping it in LogValue() causes CS8185.
            if (updatedNode.Left is DeclarationExpressionSyntax)
                return updatedNode;

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
                    if (statement is LocalDeclarationStatementSyntax localDecl &&
                        !IsRefStructType(localDecl.Declaration.Type))
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
                    if (statement is LocalDeclarationStatementSyntax localDecl &&
                        !IsRefStructType(localDecl.Declaration.Type))
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

        // Ref structs (e.g. Span<T>, ReadOnlySpan<T>) cannot be boxed to object?
        // for logging. Detect them by name heuristic.
        private static bool IsRefStructType(TypeSyntax type)
        {
            var name = type.ToString();
            return name.StartsWith("Span<") ||
                   name.StartsWith("ReadOnlySpan<") ||
                   name == "Span" ||
                   name == "ReadOnlySpan";
        }
    }
}

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Control Flow Decision
    // Traces: method entry, if/else, loops, switch, break/continue/return.
    public class ControlFlowRewriter : TraceRewriterBase
    {
        private BlockSyntax InjectEntryLog(BlockSyntax body, string name, int line)
        {
            var log = CreateLogStatement($"Enter Method '{name}'", line);
            return body.WithStatements(body.Statements.Insert(0, log));
        }

        private BlockSyntax CreateBodyForExpression(ExpressionSyntax expression, bool returnsVoid, string name, int line)
        {
            var log = CreateLogStatement($"Enter Method '{name}'", line);
            StatementSyntax tail = returnsVoid
                ? SyntaxFactory.ExpressionStatement(expression)
                : SyntaxFactory.ReturnStatement(expression);
            return SyntaxFactory.Block(log, tail);
        }

        // 1. Method Entry
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.Body != null)
            {
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                bool returnsVoid = updatedNode.ReturnType is PredefinedTypeSyntax predefined
                    && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, returnsVoid, node.Identifier.Text, line);
                return updatedNode
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(newBody);
            }
            return updatedNode;
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitConstructorDeclaration(node) as ConstructorDeclarationSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.Body != null)
            {
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, true, node.Identifier.Text, line);
                return updatedNode
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(newBody);
            }

            return updatedNode;
        }

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitLocalFunctionStatement(node) as LocalFunctionStatementSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.Body != null)
            {
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                bool returnsVoid = updatedNode.ReturnType is PredefinedTypeSyntax predefined
                    && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, returnsVoid, node.Identifier.Text, line);
                return updatedNode
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(newBody);
            }

            return updatedNode;
        }

        // 2. If Statements (and Else)
        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            var line = GetLineNumber(node.Statement);
            var updatedNode = base.VisitIfStatement(node) as IfStatementSyntax;
            if (updatedNode == null) return node;

            updatedNode = updatedNode.WithStatement(
                InstrumentStatement(updatedNode.Statement, "If-True Branch", line)
            );

            if (node.Else != null && updatedNode.Else != null)
            {
                var elseLine = GetLineNumber(node.Else.Statement);
                updatedNode = updatedNode.WithElse(
                    updatedNode.Else.WithStatement(
                        InstrumentStatement(updatedNode.Else.Statement, "Else Branch", elseLine)
                    )
                );
            }
            return updatedNode;
        }

        // 3. While Loops
        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitWhileStatement(node) as WhileStatementSyntax;
            if (updatedNode == null) return node;

            return updatedNode.WithStatement(
                InstrumentStatement(updatedNode.Statement, "While Loop", line)
            );
        }

        // 4. Do-While Loops
        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitDoStatement(node) as DoStatementSyntax;
            if (updatedNode == null) return node;

            return updatedNode.WithStatement(
                InstrumentStatement(updatedNode.Statement, "Do-While Loop", line)
            );
        }

        // 5. For Loops
        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitForStatement(node) as ForStatementSyntax;
            if (updatedNode == null) return node;

            return updatedNode.WithStatement(
                InstrumentStatement(updatedNode.Statement, "For Loop", line)
            );
        }

        // 6. ForEach Loops
        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitForEachStatement(node) as ForEachStatementSyntax;
            if (updatedNode == null) return node;

            return updatedNode.WithStatement(
                InstrumentStatement(updatedNode.Statement, "ForEach Loop", line)
            );
        }

        // 7. Switch Cases
        public override SyntaxNode? VisitSwitchSection(SwitchSectionSyntax node)
        {
            var line = GetLineNumber(node);
            var labels = string.Join(", ", node.Labels.Select(l => l.ToString()));
            var updatedNode = base.VisitSwitchSection(node) as SwitchSectionSyntax;
            if (updatedNode == null) return node;

            var log = CreateLogStatement($"Switch Case [{labels}]", line);
            return updatedNode.WithStatements(updatedNode.Statements.Insert(0, log));
        }

        // 8. Break Statements
        public override SyntaxNode? VisitBreakStatement(BreakStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var log = CreateLogStatement("Break", line);
            return SyntaxFactory.Block(log, node);
        }

        // 9. Continue Statements
        public override SyntaxNode? VisitContinueStatement(ContinueStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var log = CreateLogStatement("Continue", line);

            return SyntaxFactory.Block(log, node);
        }

        // 10. Return Statements
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var log = CreateLogStatement("Return", line);
            return SyntaxFactory.Block(log, node);
        }
    }
}
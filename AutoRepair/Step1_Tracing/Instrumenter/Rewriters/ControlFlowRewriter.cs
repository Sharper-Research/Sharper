using System;
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
        private BlockSyntax InjectEntryLog(BlockSyntax body, string name, int line, ParameterListSyntax? parameters = null)
        {
            var inserts = new List<StatementSyntax>
            {
                CreateLogStatement($"Enter Method '{name}'", line)
            };
            inserts.AddRange(CreateParameterArrayLogs(parameters, line, name));
            return body.WithStatements(body.Statements.InsertRange(0, inserts));
        }

        private BlockSyntax CreateBodyForExpression(ExpressionSyntax expression, bool returnsVoid, string name, int line, ParameterListSyntax? parameters = null)
        {
            var statements = new List<StatementSyntax>
            {
                CreateLogStatement($"Enter Method '{name}'", line)
            };
            statements.AddRange(CreateParameterArrayLogs(parameters, line, name));
            StatementSyntax tail = returnsVoid
                ? SyntaxFactory.ExpressionStatement(expression)
                : SyntaxFactory.ReturnStatement(
                    SyntaxFactory.Token(SyntaxKind.ReturnKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space),
                    expression,
                    SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            statements.Add(tail);
            return SyntaxFactory.Block(statements);
        }

        // 1. Method Entry
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.Body != null)
            {
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line, updatedNode.ParameterList);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                bool returnsVoid = updatedNode.ReturnType is PredefinedTypeSyntax predefined
                    && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, returnsVoid, node.Identifier.Text, line, updatedNode.ParameterList);
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
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line, updatedNode.ParameterList);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, true, node.Identifier.Text, line, updatedNode.ParameterList);
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
                var newBody = InjectEntryLog(updatedNode.Body, node.Identifier.Text, line, updatedNode.ParameterList);
                return updatedNode.WithBody(newBody);
            }

            if (updatedNode.ExpressionBody != null)
            {
                bool returnsVoid = updatedNode.ReturnType is PredefinedTypeSyntax predefined
                    && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword);
                var newBody = CreateBodyForExpression(updatedNode.ExpressionBody.Expression, returnsVoid, node.Identifier.Text, line, updatedNode.ParameterList);
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

        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var aliasLogs = new List<StatementSyntax>();
            if (node.Expression is InvocationExpressionSyntax invocation)
            {
                aliasLogs.AddRange(CreateArrayArgumentLogs(invocation, line));
            }

            var updated = base.VisitExpressionStatement(node) as ExpressionStatementSyntax;
            if (updated == null)
            {
                return node;
            }

            if (aliasLogs.Count > 0)
            {
                aliasLogs.Add(updated);
                return SyntaxFactory.Block(aliasLogs);
            }

            return updated;
        }

        // 10. Return Statements
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            var line = GetLineNumber(node);
            var log = CreateLogStatement("Return", line);
            return SyntaxFactory.Block(log, node);
        }


        private IEnumerable<StatementSyntax> CreateParameterArrayLogs(ParameterListSyntax? parameters, int line, string methodName)
        {
            if (parameters == null) yield break;

            for (int i = 0; i < parameters.Parameters.Count; i++)
            {
                var parameter = parameters.Parameters[i];
                if (parameter.Type is ArrayTypeSyntax)
                {
                    var paramName = parameter.Identifier.Text;
                    if (string.IsNullOrWhiteSpace(paramName))
                    {
                        continue;
                    }

                    string uniqueId = Guid.NewGuid().ToString();
                    var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                    var aliasLabel = $"{methodName}#arg{i}:{paramName}";
                    var code = $"global::AutoRepairTracing.TraceLogger.LogArray(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Array Parameter\", {paramName}, \"{aliasLabel}\");";
                    yield return SyntaxFactory.ParseStatement(code + "\n");
                    string aliasId = Guid.NewGuid().ToString();
                    var aliasCode = $"global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter(\"{aliasId}\", \"{escapedFileName}\", {line}, \"{methodName}\", {i}, {paramName}, \"{paramName}\");";
                    yield return SyntaxFactory.ParseStatement(aliasCode + "\n");
                }
            }
        }

        private IEnumerable<StatementSyntax> CreateArrayArgumentLogs(InvocationExpressionSyntax invocation, int line)
        {
            var expressionText = invocation.Expression.ToString();
            if (expressionText.Contains("TraceLogger") || expressionText.Contains("Debug."))
            {
                yield break;
            }

            var argumentList = invocation.ArgumentList;
            if (argumentList == null || argumentList.Arguments.Count == 0)
            {
                yield break;
            }

            var methodName = GetInvocationName(invocation);
            for (int i = 0; i < argumentList.Arguments.Count; i++)
            {
                var arg = argumentList.Arguments[i];

                // Skip out/ref arguments — declaration expressions are not valid standalone expressions.
                if (arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) ||
                    arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) ||
                    arg.RefKindKeyword.IsKind(SyntaxKind.InKeyword) ||
                    arg.Expression is DeclarationExpressionSyntax)
                    continue;

                var argExpr = arg.Expression.ToString();
                var argLiteral = EscapeString(argExpr);
                string uniqueId = Guid.NewGuid().ToString();
                var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                var code = $"global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"{methodName}\", {i}, {argExpr}, \"{argLiteral}\");";
                yield return SyntaxFactory.ParseStatement(code + "\n");
            }
        }

        private string GetInvocationName(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.Text;
            }

            if (invocation.Expression is MemberAccessExpressionSyntax member && member.Name is IdentifierNameSyntax memberIdentifier)
            {
                return memberIdentifier.Identifier.Text;
            }

            return "UnknownMethod";
        }

        private string EscapeString(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Step 1: Control Flow Decision
    public class ControlFlowRewriter : TraceRewriterBase
    {
        // 1. Method Entry
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var line = GetLineNumber(node);
            var updatedNode = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
            if (updatedNode == null) return node;

            if (updatedNode.Body != null)
            {
                var log = CreateLogStatement($"Enter Method '{node.Identifier.Text}'", line);
                var newBody = updatedNode.Body.WithStatements(updatedNode.Body.Statements.Insert(0, log));
                return updatedNode.WithBody(newBody);
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

        
    }
}
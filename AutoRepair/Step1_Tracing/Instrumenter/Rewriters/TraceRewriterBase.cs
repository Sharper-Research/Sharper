using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Base class
    public abstract class TraceRewriterBase : CSharpSyntaxRewriter
    {
        protected StatementSyntax CreateLogStatement(string context, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            return SyntaxFactory.ParseStatement($"TraceLogger.Log(\"{uniqueId}\", \"Program.cs\", {line}, \"{context}\");\n");
        }

        protected StatementSyntax CreateVariableLogStatement(string varName, string valueExpr, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            var code = $"TraceLogger.Log(\"{uniqueId}\", \"Program.cs\", {line}, \"Variable Update\", {valueExpr}, \"{varName}\");";
            return SyntaxFactory.ParseStatement(code + "\n");
        }

        protected StatementSyntax InstrumentStatement(StatementSyntax statement, string logContext, int line)
        {
            var logNode = CreateLogStatement(logContext, line);

            if (statement is BlockSyntax block)
            {
                return block.WithStatements(block.Statements.Insert(0, logNode));
            }
            else
            {
                return SyntaxFactory.Block(logNode, statement);
            }
        }

        protected int GetLineNumber(SyntaxNode node)
        {
            var annotation = node.GetAnnotations(LineNumberAnnotator.AnnotationKind).FirstOrDefault();
            if (annotation != null && int.TryParse(annotation.Data, out int line))
            {
                return line;
            }
            return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        }
    }
}
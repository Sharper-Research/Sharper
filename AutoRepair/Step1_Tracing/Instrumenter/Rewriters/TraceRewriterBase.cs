using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Base rewriter utilities for tracing.
    // Provides consistent log/line helpers used by all rewriters.
    public abstract class TraceRewriterBase : CSharpSyntaxRewriter
    {
        public string TargetFileName { get; set; } = "UnknownFile.cs";

        protected StatementSyntax CreateLogStatement(string context, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var escapedContext = context.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
            return SyntaxFactory.ParseStatement($"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"{escapedContext}\");\n");
        }

        protected StatementSyntax CreateVariableLogStatement(string varName, string valueExpr, int line)
        {
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var code = $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Variable Update\", {valueExpr}, \"{varName}\");";
            return SyntaxFactory.ParseStatement(code + "\n");
        }

        protected IEnumerable<StatementSyntax> CreateArrayAliasLogs(ArgumentListSyntax argumentList, IReadOnlyList<ParameterSyntax> parameters, int line)
        {
            if (argumentList.Arguments.Count != parameters.Count)
            {
                yield break;
            }

            for (int i = 0; i < argumentList.Arguments.Count; i++)
            {
                var parameter = parameters[i];
                if (parameter.Type is ArrayTypeSyntax)
                {
                    var argumentExpression = argumentList.Arguments[i].Expression.ToString();
                    var parameterName = parameter.Identifier.Text;
                    string uniqueId = Guid.NewGuid().ToString();
                    var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                    var logAlias = $"global::AutoRepairTracing.TraceLogger.LogArray(\"{uniqueId}\", \"{escapedFileName}\", {line}, \"Array Alias\", {argumentExpression}, \"{parameterName}\");";
                    yield return SyntaxFactory.ParseStatement(logAlias + "\n");
                }
            }
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

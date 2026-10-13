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
        // Annotation added to BlockSyntax nodes that were created by the instrumenter
        // (as statement wrappers) rather than existing in the original source.
        // VisitBlock flattens these back into the parent block to prevent out-var scoping issues.
        protected static readonly SyntaxAnnotation InstrBlock = new SyntaxAnnotation("AutoRepair.InstrBlock");

        // Annotation added to the method-entry / parameter log statements that ControlFlowRewriter
        // injects at the top of a method body. MethodExitRewriter (which runs last) keeps these
        // outside the try/finally it wraps the body in, so the entry event is recorded even if the
        // body throws before reaching the first real statement.
        protected static readonly SyntaxAnnotation MethodPreamble = new SyntaxAnnotation("AutoRepair.MethodPreamble");

        // Narrower marker for the single "Enter Method" log inside that preamble.
        // MethodExitRewriter swaps this statement for the depth-tracking LogEnter call, but only
        // when it has successfully installed the matching exit log - so an entry event that
        // increments the call-depth counter always has an exit event that decrements it.
        protected static readonly SyntaxAnnotation MethodEntryLog = new SyntaxAnnotation("AutoRepair.MethodEntryLog");

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

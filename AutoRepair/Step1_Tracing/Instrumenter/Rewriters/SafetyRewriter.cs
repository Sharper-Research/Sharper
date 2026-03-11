using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Safety and Error Handling
    public class SafetyRewriter : TraceRewriterBase
    {
        // Types to Watch:
        // - Main Method: Global catch-all.
        // - Catch Blocks: Log exceptions.
        // - Loops: Watchdog counters.
        // - Null Dereferences: Check for null before accessing members
        // - NEW: "Add to Google Doc (Pseudocode_V2)"
        // Pointer Checks: Dereferences and NullReferenceExceptions.

        // Step 2 (for crash coverage) - TRACE IDEAS ONLY:
        // - Exception type + message + file/line when an exception is thrown/caught.
        // - Null checks (optional): log when a variable is null before a dereference.
        // - Divide-by-zero checks: log denominator value before division.
        // - Checked arithmetic overflow: log when overflow occurs.
        // - ObjectDisposed: log when Dispose called; log usage after dispose.
        // - File/Path errors: log file paths before IO calls.
        // - Task cancellation/timeout: log cancellation tokens + timeouts.

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var statements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                var visited = Visit(statement) as StatementSyntax ?? statement;
                statements.Add(visited);

                if (visited is LocalDeclarationStatementSyntax localDecl)
                {
                    var line = GetLineNumber(statement);
                    statements.AddRange(CreateNullAssignmentLogs(localDecl, line));
                }
            }

            return node.WithStatements(SyntaxFactory.List(statements));
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var updatedNode = base.VisitMethodDeclaration(node) as MethodDeclarationSyntax;
            if (updatedNode == null || updatedNode.Body == null)
            {
                return node;
            }

            if (IsAlreadyWrapped(updatedNode.Body))
            {
                return updatedNode;
            }

            // C# forbids 'yield return/break' inside a try block with a catch clause.
            // Skip wrapping iterator methods entirely.
            if (updatedNode.Body.DescendantNodes().OfType<YieldStatementSyntax>().Any())
            {
                return updatedNode;
            }

            var lineNumber = GetLineNumber(node);
            var tryStatement = SyntaxFactory.TryStatement()
                .WithBlock(updatedNode.Body)
                .WithCatches(SyntaxFactory.SingletonList<CatchClauseSyntax>(CreateMethodCatchClause(lineNumber)));

            return updatedNode.WithBody(SyntaxFactory.Block(tryStatement));
        }

        public override SyntaxNode? VisitCatchClause(CatchClauseSyntax node)
        {
            var updatedNode = base.VisitCatchClause(node) as CatchClauseSyntax;
            if (updatedNode == null)
            {
                return node;
            }

            var declaration = updatedNode.Declaration ??
                SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName("System.Exception"), SyntaxFactory.Identifier("__autoRepairCaughtEx"));

            // If the catch has a type but no variable name (e.g. catch (FormatException)),
            // add a generated identifier so we can reference the exception.
            if (declaration.Identifier.IsMissing || string.IsNullOrEmpty(declaration.Identifier.Text))
            {
                // Add a leading space so the type and identifier don't get concatenated
                // (e.g. 'ExceptionFoo' instead of 'Exception Foo').
                declaration = declaration.WithIdentifier(
                    SyntaxFactory.Identifier("__autoRepairCaughtEx")
                        .WithLeadingTrivia(SyntaxFactory.Space));
            }

            updatedNode = updatedNode.WithDeclaration(declaration);

            if (updatedNode.Block == null)
            {
                return updatedNode;
            }

            var exIdentifier = declaration.Identifier.Text;
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var lineNumber = GetLineNumber(node);

            var logStatements = CreateExceptionCatchLogs(exIdentifier, escapedFileName, lineNumber);
            var newStatements = updatedNode.Block.Statements.InsertRange(0, logStatements);
            return updatedNode.WithBlock(updatedNode.Block.WithStatements(newStatements));
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            // IMPLEMENT: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
            const int LOOP_LIMIT = 10000;
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var lineNumber = GetLineNumber(node);
            
            // Create counter variable declaration: int __loopCounter_guid = 0;
            var counterVarName = $"__loopCounter_{uniqueId.Replace("-", "_").Substring(0, 8)}";
            var counterDeclaration = SyntaxFactory.ParseStatement(
                $"int {counterVarName} = 0;"
            );
            
            // Create counter increment and check: __loopCounter++; if (__loopCounter > LIMIT) { Log warning; break; }
            var body = node.Statement;
            var incrementAndCheck = SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName}++")
                ),
                SyntaxFactory.IfStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName} > {LOOP_LIMIT}"),
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.ParseExpression(
                                $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"Infinite Loop Warning: exceeded {LOOP_LIMIT} iterations\", {counterVarName}, \"loopCounter\")"
                            )
                        ),
                        SyntaxFactory.BreakStatement()
                    )
                )
            );
            
            // Wrap the loop body with counter increment and check
            BlockSyntax wrappedBody;
            if (body is BlockSyntax blockBody)
            {
                wrappedBody = blockBody.WithStatements(
                    incrementAndCheck.Statements.AddRange(blockBody.Statements)
                );
            }
            else
            {
                wrappedBody = SyntaxFactory.Block(
                    incrementAndCheck.Statements.Add(body)
                );
            }
            
            // Create the instrumented while loop
            var newWhile = node.WithStatement(wrappedBody);
            
            // Wrap in a block with counter declaration
            var block = SyntaxFactory.Block(
                counterDeclaration,
                newWhile
            );
            
            return block;
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            // IMPLEMENT: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
            const int LOOP_LIMIT = 10000;
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var lineNumber = GetLineNumber(node);
            
            // Create counter variable declaration: int __loopCounter_guid = 0;
            var counterVarName = $"__loopCounter_{uniqueId.Replace("-", "_").Substring(0, 8)}";
            var counterDeclaration = SyntaxFactory.ParseStatement(
                $"int {counterVarName} = 0;"
            );
            
            // Create counter increment and check
            var body = node.Statement;
            var incrementAndCheck = SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName}++")
                ),
                SyntaxFactory.IfStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName} > {LOOP_LIMIT}"),
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.ParseExpression(
                                $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"Infinite Loop Warning: exceeded {LOOP_LIMIT} iterations\", {counterVarName}, \"loopCounter\")"
                            )
                        ),
                        SyntaxFactory.BreakStatement()
                    )
                )
            );
            
            // Wrap the loop body
            BlockSyntax wrappedBody;
            if (body is BlockSyntax blockBody)
            {
                wrappedBody = blockBody.WithStatements(
                    incrementAndCheck.Statements.AddRange(blockBody.Statements)
                );
            }
            else
            {
                wrappedBody = SyntaxFactory.Block(
                    incrementAndCheck.Statements.Add(body)
                );
            }
            
            // Create instrumented for loop
            var newFor = node.WithStatement(wrappedBody);
            
            // Wrap in block with counter
            var block = SyntaxFactory.Block(
                counterDeclaration,
                newFor
            );
            
            return block;
        }
        
        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
            // IMPLEMENT: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
            const int LOOP_LIMIT = 10000;
            string uniqueId = Guid.NewGuid().ToString();
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var lineNumber = GetLineNumber(node);
            
            // Create counter variable declaration: int __loopCounter_guid = 0;
            var counterVarName = $"__loopCounter_{uniqueId.Replace("-", "_").Substring(0, 8)}";
            var counterDeclaration = SyntaxFactory.ParseStatement(
                $"int {counterVarName} = 0;"
            );
            
            // Create counter increment and check
            var body = node.Statement;
            var incrementAndCheck = SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName}++")
                ),
                SyntaxFactory.IfStatement(
                    SyntaxFactory.ParseExpression($"{counterVarName} > {LOOP_LIMIT}"),
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.ParseExpression(
                                $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"Infinite Loop Warning: exceeded {LOOP_LIMIT} iterations\", {counterVarName}, \"loopCounter\")"
                            )
                        ),
                        SyntaxFactory.BreakStatement()
                    )
                )
            );
            
            // Wrap the do-body
            BlockSyntax wrappedBody;
            if (body is BlockSyntax blockBody)
            {
                wrappedBody = blockBody.WithStatements(
                    incrementAndCheck.Statements.AddRange(blockBody.Statements)
                );
            }
            else
            {
                wrappedBody = SyntaxFactory.Block(
                    incrementAndCheck.Statements.Add(body)
                );
            }
            
            // Create instrumented do statement
            var newDo = node.WithStatement(wrappedBody);
            
            // Wrap in block with counter
            var block = SyntaxFactory.Block(
                counterDeclaration,
                newDo
            );
            
            return block;
        }

        private IEnumerable<StatementSyntax> CreateNullAssignmentLogs(LocalDeclarationStatementSyntax node, int lineNumber)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                if (variable.Initializer != null && variable.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    yield return CreateNullAssignmentLog(variable.Identifier.Text, lineNumber);
                }
            }
        }

        private StatementSyntax CreateNullAssignmentLog(string variableName, int lineNumber)
        {
            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Null Assignment Warning\", \"null\", \"{variableName}\");");
        }

        private bool IsAlreadyWrapped(BlockSyntax body)
        {
            return body.Statements.Count == 1 && body.Statements[0] is TryStatementSyntax;
        }

        private CatchClauseSyntax CreateMethodCatchClause(int lineNumber)
        {
            var template = SyntaxFactory.ParseStatement("try { } catch (System.Exception ex) { }") as TryStatementSyntax;
            if (template == null || template.Catches.Count == 0)
            {
                throw new InvalidOperationException("Failed to create catch template.");
            }

            var escapedFileName = TargetFileName.Replace("\\", "\\\\");
            var catchLogs = CreateExceptionCatchLogs("ex", escapedFileName, lineNumber);
            var catchBlock = SyntaxFactory.Block(catchLogs);
            return template.Catches[0].WithBlock(catchBlock);
        }

        private IEnumerable<StatementSyntax> CreateExceptionCatchLogs(string identifier, string escapedFileName, int lineNumber)
        {
            yield return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Exception Caught\", {identifier}.Message, \"{identifier}.Message\");");
            yield return SyntaxFactory.ParseStatement(
                $"if ({identifier} is System.NullReferenceException) {{ global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Null Dereference\", {identifier}.Message, \"null.dereference\"); }}");
            yield return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Exception Type\", {identifier}.GetType().FullName, \"{identifier}.Type\");");
            yield return SyntaxFactory.ParseStatement(
                $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Exception Stack\", {identifier}.StackTrace ?? string.Empty, \"{identifier}.Stack\");");
            yield return SyntaxFactory.ParseStatement(
                $"System.Diagnostics.Debug.Fail(\"Unhandled exception\", {identifier}.ToString());");
            yield return SyntaxFactory.ParseStatement("throw;\n");
        }
    }
}

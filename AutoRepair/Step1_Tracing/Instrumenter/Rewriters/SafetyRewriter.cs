using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Step 4: Safety and Error Handling (For Sahal)
    // I just Inherited from TraceRewriterBase for consistency, as I also did it for my files.
    // The TODOs are from the google doc that we have, you can add more types to watch out for and traces that you might come up with while reading some material or programming.
    public class SafetyRewriter : TraceRewriterBase
    {
        // Types to Watch:
        // - Main Method: Global catch-all.
        // - Catch Blocks: Log exceptions.
        // - Loops: Watchdog counters.
        // - Null Dereferences: Check for null before accessing members

        // - NEW: "Add to Google Doc (Pseudocode_V2) to keep tasks updated, please." - I read an article that mentioned that Pointer Dereferences are common errors in code and important to keep in consideration for any static analysis detector.
        // Pointer Checks: Dereferences and NullReferenceExceptions.

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            // Check if variable is assigned null and log it
            var variables = node.Declaration.Variables;
            if (variables.Count > 0)
            {
                var variable = variables[0];
                if (variable.Initializer != null)
                {
                    var initValue = variable.Initializer.Value.ToString();
                    if (initValue == "null")
                    {
                        var varName = variable.Identifier.Text;
                        string uniqueId = Guid.NewGuid().ToString();
                        var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                        var lineNumber = GetLineNumber(node);

                        // Add a statement that logs null assignment
                        var logStatement = SyntaxFactory.ParseStatement(
                            $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"Null Assignment Warning\", \"null\", \"{varName}\");\n");

                        // Return the original declaration
                        return base.VisitLocalDeclarationStatement(node);
                    }
                }
            }
            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Log member access for debugging - helps identify null reference patterns
            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // IMPLEMENT: WRAP Main body in Try/Catch to log specific crash details and NullReferenceExceptions.
            if (node.Identifier.Text == "Main" && node.Body != null)
            {
                var body = node.Body;
                string uniqueId = Guid.NewGuid().ToString();
                var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                var lineNumber = GetLineNumber(node);
                
                // Create catch block code as a statement block with proper TraceLogger calls
                var catchBlock = SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.ParseExpression(
                            $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"Exception Caught: \" + ex.Message, ex.Message, \"ex\")"
                        )
                    ),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.ParseExpression(
                            $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Exception Type: \" + ex.GetType().Name, ex.GetType().Name, \"exType\")"
                        )
                    )
                );
                
                // Parse the entire try-catch as one statement to avoid formatting issues
                var tryStatement = SyntaxFactory.ParseStatement(
                    "try { } catch (System.Exception ex) { }"
                ) as TryStatementSyntax;
                
                if (tryStatement != null)
                {
                    // Replace the try block with actual body
                    tryStatement = tryStatement.WithBlock(body);
                    
                    // Replace the catch block with our logging code
                    var catchClauses = SyntaxFactory.SingletonList(
                        tryStatement.Catches[0]
                            .WithBlock(catchBlock)
                    );
                    
                    tryStatement = tryStatement.WithCatches(catchClauses);
                    
                    // Create new method with try-catch wrapped body
                    var newMethod = node.WithBody(
                        SyntaxFactory.Block(tryStatement)
                    );
                    
                    return base.VisitMethodDeclaration(newMethod);
                }
            }
            
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode? VisitCatchClause(CatchClauseSyntax node)
        {
            // TODO: INJECT logging of the exception being caught.
            return base.VisitCatchClause(node);
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
    }
}
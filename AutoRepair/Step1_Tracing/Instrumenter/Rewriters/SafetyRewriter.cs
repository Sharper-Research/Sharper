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
            // TODO: WRAP Main body in Try/Catch to log specific crash details and NullReferenceExceptions.
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode? VisitCatchClause(CatchClauseSyntax node)
        {
            // TODO: INJECT logging of the exception being caught.
            return base.VisitCatchClause(node);
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            // TODO: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
            return base.VisitWhileStatement(node);
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            // TODO: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
            return base.VisitForStatement(node);
        }
        
        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
             // TODO: INJECT a counter. IF counter > LIMIT, LOG "Infinite Loop Warning".
             return base.VisitDoStatement(node);
        }
    }
}
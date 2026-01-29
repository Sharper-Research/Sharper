using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Step 3: Heap State (For Sahal)
    // I just Inherited from TraceRewriterBase for consistency, as I also did it for my files.
    // The TODOs are from the google doc that we have, you can add more types to watch out for and traces that you might come up with while reading some material or programming.
    public class HeapRewriter : TraceRewriterBase
    {
        // Types to Watch:
        // - Arrays
        // - Lists/Collections
        // - Objects

        // - NEW: "Add to Google Doc (Pseudocode_V2) to keep tasks updated, please." - I read it is a common error in code and important to keep in consideration for any static analysis detector.
        // References (Watch for Null/Unassigned dereferences)

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var newStatements = new System.Collections.Generic.List<StatementSyntax>();

            foreach (var statement in node.Statements)
            {
                newStatements.Add((StatementSyntax)Visit(statement));

                // If it's a local declaration for array/list/object, add heap logging
                if (statement is LocalDeclarationStatementSyntax localDecl)
                {
                    var heapLogStatement = CreateHeapLogStatement(localDecl);
                    if (heapLogStatement != null)
                    {
                        newStatements.Add(heapLogStatement);
                    }
                }
            }

            return node.WithStatements(SyntaxFactory.List(newStatements));
        }

        private StatementSyntax? CreateHeapLogStatement(LocalDeclarationStatementSyntax node)
        {
            var variables = node.Declaration.Variables;
            if (variables.Count == 0)
                return null;

            var variable = variables[0];
            var typeInfo = node.Declaration.Type;

            // Check if it's an array type
            bool isArray = typeInfo is ArrayTypeSyntax;

            // Check if it's a List or Collection
            string typeString = typeInfo.ToString();
            bool isList = typeString.Contains("List") || typeString.Contains("Collection") || typeString.Contains("IEnumerable");

            if (isArray || isList)
            {
                var lineNumber = GetLineNumber(node);
                string varName = variable.Identifier.Text;

                // Create unique log entry
                string uniqueId = Guid.NewGuid().ToString();
                var escapedFileName = TargetFileName.Replace("\\", "\\\\");
                string heapInfo = $"Heap State: {varName} ({typeString})";
                var logCode = $"global::AutoRepairTracing.TraceLogger.Log(\"{uniqueId}\", \"{escapedFileName}\", {lineNumber}, \"{heapInfo}\");";

                return SyntaxFactory.ParseStatement(logCode + "\n");
            }

            return null;
        }
    }
}
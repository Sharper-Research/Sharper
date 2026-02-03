using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    public class HeapRewriter : TraceRewriterBase
    {
        // Types to Watch:
        // - Arrays
        // - Lists/Collections
        // - Objects
        // - NEW: "Add to Google Doc (Pseudocode_V2) to keep tasks updated".
        // References (Watch for Null/Unassigned dereferences)

        // Step 2 (for crash coverage) - TRACE IDEAS ONLY:
        // - Array length at allocation, and index accesses (arr[i]) to infer bounds.
        // - String/list/collection Count/Length and index access (s[i], list[i]).
        // - Dictionary access (dict[key]) + key existence (ContainsKey/TryGetValue).
        // - Object creation sites to know non-null origins.
        // - Optional: record buffer sizes for arguments passed into methods.

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
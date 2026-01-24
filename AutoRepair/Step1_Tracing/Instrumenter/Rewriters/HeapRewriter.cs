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

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            // TODO: Check if variable is Array, List, or Object.
            // TODO: LOG Type information (Semantic check).
            // TODO: LOG Contents (if collection) or ToString (if object).
            // Example: "Array arr (int[]): [0, 4, 78]"
            return base.VisitLocalDeclarationStatement(node);
        }
    }
}
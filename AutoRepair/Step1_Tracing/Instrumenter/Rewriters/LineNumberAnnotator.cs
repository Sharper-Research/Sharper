using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    // Adds original line annotations for stable logging.
    // Used before rewriting so logs map to source lines.
    public class LineNumberAnnotator
    {
        public static readonly string AnnotationKind = "OriginalLine";

        public static SyntaxNode Annotate(SyntaxNode root)
        {
            var nodesToAnnotate = root.DescendantNodes()
                .Where(n => n is StatementSyntax || n is MethodDeclarationSyntax);

            return root.ReplaceNodes(nodesToAnnotate, (original, rewritten) =>
            {
                var line = original.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                return rewritten.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationKind, line.ToString()));
            });
        }
    }
}

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Instrumenter.Rewriters
{
    public class LineNumberAnnotator
    {
        public static readonly string AnnotationKind = "OriginalLine";

        public static SyntaxNode Annotate(SyntaxNode root)
        {
            // Find all statements and method declarations
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

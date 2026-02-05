using System;
using System.Collections.Generic;
using System.Linq;
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
            var statements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                var visited = Visit(statement) as StatementSyntax ?? statement;
                statements.Add(visited);

                if (visited is LocalDeclarationStatementSyntax localDecl)
                {
                    var line = GetLineNumber(statement);
                    statements.AddRange(CreateHeapLogStatements(localDecl, line));
                }
            }

            return node.WithStatements(SyntaxFactory.List(statements));
        }

        public override SyntaxNode? VisitSwitchSection(SwitchSectionSyntax node)
        {
            var statements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                var visited = Visit(statement) as StatementSyntax ?? statement;
                statements.Add(visited);

                if (visited is LocalDeclarationStatementSyntax localDecl)
                {
                    var line = GetLineNumber(statement);
                    statements.AddRange(CreateHeapLogStatements(localDecl, line));
                }
            }

            return node.WithStatements(SyntaxFactory.List(statements));
        }

        private IEnumerable<StatementSyntax> CreateHeapLogStatements(LocalDeclarationStatementSyntax node, int lineNumber)
        {
            var typeSyntax = node.Declaration.Type;
            var typeString = typeSyntax.ToString();

            bool isArray = typeSyntax is ArrayTypeSyntax;
            bool isCollection = typeString.Contains("List") || typeString.Contains("Collection") || typeString.Contains("IEnumerable");

            if (!isArray && !isCollection)
            {
                yield break;
            }

            var escapedFileName = TargetFileName.Replace("\\", "\\\\");

            foreach (var variable in node.Declaration.Variables)
            {
                var varName = variable.Identifier.Text;
                var descriptor = BuildDescriptor(typeString, variable.Initializer?.Value, isArray);
                var logCode =
                    $"global::AutoRepairTracing.TraceLogger.Log(\"{Guid.NewGuid()}\", \"{escapedFileName}\", {lineNumber}, \"Heap State: {varName} ({descriptor})\");";
                yield return SyntaxFactory.ParseStatement(logCode);
            }
        }

        private string BuildDescriptor(string typeString, ExpressionSyntax? initializer, bool isArray)
        {
            var details = typeString;
            var capacity = ExtractCapacity(initializer, isArray);
            if (!string.IsNullOrEmpty(capacity))
            {
                details += $" size={capacity}";
            }

            return details;
        }

        private string? ExtractCapacity(ExpressionSyntax? initializer, bool isArray)
        {
            if (initializer == null)
            {
                return null;
            }

            if (isArray)
            {
                if (initializer is ArrayCreationExpressionSyntax arrayCreation)
                {
                    var ranks = arrayCreation.Type.RankSpecifiers
                        .Select(rs => string.Join("x", rs.Sizes.Where(size => !size.IsMissing).Select(size => size.ToString())))
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    var combined = string.Join("x", ranks);
                    return string.IsNullOrWhiteSpace(combined) ? null : combined;
                }

                if (initializer is ImplicitArrayCreationExpressionSyntax implicitArray)
                {
                    var elements = implicitArray.Initializer?.Expressions.Count ?? 0;
                    return elements > 0 ? elements.ToString() : null;
                }
            }
            else
            {
                if (initializer is ObjectCreationExpressionSyntax objectCreation && objectCreation.ArgumentList != null)
                {
                    var args = objectCreation.ArgumentList.Arguments;
                    if (args.Count > 0)
                    {
                        return string.Join(",", args.Select(a => a.ToString()));
                    }
                }
            }

            return null;
        }
    }
}

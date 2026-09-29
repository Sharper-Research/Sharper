using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Sharper.CFGAnalyzer.Analysis;

namespace Sharper.CFGAnalyzer.Repair;

/// <summary>
/// Generates catch-based candidates; does not implement the paper's predicate verification.
/// </summary>
public static class PatchSynthesizer
{
    public static async Task<(string Path, string Diff, PaperForwardResult Check, List<PaperForwardResult> Checks)?> WriteLoopGuardPatchAsync(
        Document document, StatementSyntax statement, Violation violation, string patchesDirectory)
    {
        var originalRoot = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Missing source root");
        var statementIndex = originalRoot.DescendantNodes().OfType<ExpressionStatementSyntax>().ToList()
            .FindIndex(candidate => candidate.Span == statement.Span);
        if (statementIndex < 0) return null;
        var current = document;
        var checks = new List<PaperForwardResult>();
        var edits = 0;
        async Task<PaperForwardResult> CheckAsync(Document candidate)
        {
            var root = await candidate.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Missing candidate root");
            var selected = root.DescendantNodes().OfType<ExpressionStatementSyntax>().ElementAt(statementIndex);
            var result = await PaperSourceAnalysis.AnalyzeAsync(candidate,
                violation with { Line = selected.GetLocation().GetLineSpan().StartLinePosition.Line + 1 });
            return result.ForwardModelCheck ?? new("Incomplete", 0, null, null, result.Reason);
        }
        for (var iteration = 0; iteration <= 8; iteration++)
        {
            var check = await CheckAsync(current);
            checks.Add(check);
            if (check.Status != "Counterexample" || iteration == 8) break;
            var root = await current.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Missing candidate root");
            var statements = root.DescendantNodes().OfType<ExpressionStatementSyntax>().ToArray();
            var matches = iteration == 0 ? [statements[statementIndex]] : statements.Where(candidate =>
                candidate.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == check.CounterexampleLine).Take(2).ToArray();
            var failing = matches.Length == 1 ? matches[0] : null;
            if (failing?.Expression is not AssignmentExpressionSyntax
                {
                    Left: ElementAccessExpressionSyntax { Expression: IdentifierNameSyntax array, ArgumentList.Arguments.Count: 1 } access
                } || !failing.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || access.ArgumentList.Arguments[0].Expression is not IdentifierNameSyntax index
                || failing.Parent is not BlockSyntax block || block.Statements[0] != failing
                || block.Parent is not WhileStatementSyntax loop) break;
            var guarded = loop.WithCondition(SyntaxFactory.ParseExpression(
                $"({loop.Condition}) && {array} != null && {index} >= 0 && {index} < {array}.Length"))
                .WithAdditionalAnnotations(Formatter.Annotation);
            var updated = Formatter.Format(root.ReplaceNode(loop, guarded), Formatter.Annotation, document.Project.Solution.Workspace);
            current = current.WithSyntaxRoot(updated);
            edits++;
        }
        if (edits == 0) return null;
        var candidateRoot = await current.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Missing candidate root");
        var (path, diff) = await WriteCandidateAsync(document, statement, candidateRoot, patchesDirectory);
        var saved = document.WithText(SourceText.From(await File.ReadAllTextAsync(path)));
        var savedCheck = await CheckAsync(saved);
        return (path, diff, savedCheck, checks);
    }

    public static async Task<(string patchedFilePath, string diffPath)> WritePatchAsync(
        Document doc,
        StatementSyntax statement,
        string exceptionType,
        string patchesDir,
        bool wrapRemainingBlock = false,
        ExpressionSyntax? failureGuard = null)
    {
        var tree = await doc.GetSyntaxTreeAsync()
                   ?? throw new InvalidOperationException($"No syntax tree for {doc.FilePath}");
        var root = await tree.GetRootAsync();

        SyntaxNode newRoot;
        if (failureGuard != null)
        {
            if (statement is not ReturnStatementSyntax)
                throw new NotSupportedException("Predicate guards currently require a direct return statement");
            var guarded = SyntaxFactory.Block(
                SyntaxFactory.IfStatement(failureGuard, SyntaxFactory.ParseStatement("return default;")),
                statement.WithoutLeadingTrivia())
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            newRoot = root.ReplaceNode(statement, guarded);
        }
        else newRoot = ReplaceWithTryCatch(root, statement, exceptionType, wrapRemainingBlock);
        return await WriteCandidateAsync(doc, statement, newRoot, patchesDir);
    }

    private static async Task<(string patchedFilePath, string diffPath)> WriteCandidateAsync(
        Document doc, StatementSyntax statement, SyntaxNode newRoot, string patchesDir)
    {
        var originalText = await doc.GetTextAsync();
        var formatted = Formatter.Format(newRoot, Formatter.Annotation, doc.Project.Solution.Workspace);
        var newText = formatted.ToFullString();

        var fileName = Path.GetFileName(doc.FilePath ?? doc.Name);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var line = statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        var patchedFile = Path.Combine(patchesDir, $"{stem}.L{line}.patched{ext}");
        var diffFile = Path.Combine(patchesDir, $"{stem}.L{line}.diff");

        await File.WriteAllTextAsync(patchedFile, newText);

        var diff = UnifiedDiff(
            originalLabel: $"a/{fileName}",
            patchedLabel: $"b/{fileName}",
            originalText.ToString().Split('\n'),
            newText.Split('\n'));
        await File.WriteAllTextAsync(diffFile, diff);

        return (patchedFile, diffFile);
    }

    /// <summary>
    /// Apply every patch in a plan in-place. Backs up the original file as
    /// "<file>.sharper.bak" so the change is reversible.
    /// </summary>
    public static int ApplyInPlace(RepairPlan plan)
    {
        var candidates = plan.Items.Where(item => item.PatchedFilePath != null)
            .DistinctBy(item => (item.ContainingFilePath, item.PatchedFilePath)).ToArray();
        if (candidates.Length == 0) return 0;
        if (candidates.Length != 1) throw new InvalidOperationException("Apply one independently validated candidate at a time; combinations require joint validation");
        var item = candidates[0];
        if (item.Validation?.Status != "AcceptedByTests")
            throw new InvalidOperationException("Refusing to apply a candidate without reproducer/regression acceptance");
        if (CandidateValidator.HashFile(item.ContainingFilePath) != item.Validation.SourceSha256
            || CandidateValidator.HashFile(item.PatchedFilePath!) != item.Validation.CandidateSha256)
            throw new InvalidOperationException("Source or candidate changed after validation");
        var backup = item.ContainingFilePath + ".sharper.bak";
        if (File.Exists(backup) && CandidateValidator.HashFile(backup) != item.Validation.SourceSha256)
            throw new InvalidOperationException("Existing backup belongs to a different source revision");
        if (!File.Exists(backup)) File.Copy(item.ContainingFilePath, backup);
        File.Copy(item.PatchedFilePath!, item.ContainingFilePath, overwrite: true);
        return 1;
    }

    /// <summary>
    /// Restore every file that was patched in-place via ApplyInPlace.
    /// </summary>
    public static int RestoreFromBackups(RepairPlan plan)
    {
        int restored = 0;
        foreach (var item in plan.Items)
        {
            if (string.IsNullOrEmpty(item.ContainingFilePath)) continue;
            var backup = item.ContainingFilePath + ".sharper.bak";
            if (!File.Exists(backup)) backup = item.ContainingFilePath + ".autorepair.bak";
            if (!File.Exists(backup)) continue;
            File.Copy(backup, item.ContainingFilePath, overwrite: true);
            File.Delete(backup);
            restored++;
        }
        return restored;
    }

    private static StatementSyntax WrapInTryCatch(
        IReadOnlyList<StatementSyntax> statements,
        string exceptionType)
    {
        if (statements.Count == 0)
            throw new ArgumentException("At least one statement is required", nameof(statements));

        var leadingTrivia = statements[0].GetLeadingTrivia();
        var withoutLeading = statements.Select((statement, index) =>
            index == 0 ? statement.WithLeadingTrivia() : statement).ToArray();

        var commentTrivia = SyntaxFactory.Comment(
            $"// [Sharper Step 3] Unverified catch-based candidate for {exceptionType}");

        var catchStatements = CreateCatchStatements(statements[0]);

        var tryStmt = SyntaxFactory.TryStatement(
            tryKeyword: SyntaxFactory.Token(SyntaxKind.TryKeyword),
            block: SyntaxFactory.Block(withoutLeading),
            catches: SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(exceptionType)),
                    null,
                    SyntaxFactory.Block(catchStatements))),
            @finally: null);

        return tryStmt
            .WithAdditionalAnnotations(Formatter.Annotation)
            .WithLeadingTrivia(leadingTrivia.Add(commentTrivia).Add(SyntaxFactory.ElasticCarriageReturnLineFeed));
    }

    private static SyntaxNode ReplaceWithTryCatch(
        SyntaxNode root,
        StatementSyntax statement,
        string exceptionType,
        bool wrapRemainingBlock)
    {
        if (statement.Parent is BlockSyntax parentBlock)
        {
            var index = parentBlock.Statements.IndexOf(statement);
            if (index >= 0)
            {
                var endIndex = wrapRemainingBlock
                    ? parentBlock.Statements.Count - 1
                    : FindDependentRangeEnd(parentBlock, index);
                var selected = parentBlock.Statements.Skip(index).Take(endIndex - index + 1).ToArray();
                var wrapped = WrapInTryCatch(selected, exceptionType);

                var newStatements = parentBlock.Statements.ToList();
                newStatements.RemoveRange(index, selected.Length);
                newStatements.Insert(index, wrapped);
                var newBlock = parentBlock.WithStatements(SyntaxFactory.List(newStatements));
                return root.ReplaceNode(parentBlock, newBlock);
            }
        }

        return root.ReplaceNode(statement, WrapInTryCatch(new[] { statement }, exceptionType));
    }

    private static int FindDependentRangeEnd(BlockSyntax block, int startIndex)
    {
        var statement = block.Statements[startIndex];
        if (statement is not LocalDeclarationStatementSyntax localDeclaration)
            return startIndex;

        var declaredNames = GetDeclaredNames(localDeclaration).ToHashSet(StringComparer.Ordinal);
        if (declaredNames.Count == 0)
            return startIndex;

        var endIndex = startIndex;
        var changed = true;
        while (changed)
        {
            changed = false;
            for (var index = endIndex + 1; index < block.Statements.Count; index++)
            {
                if (!ReferencesAnyIdentifier(block.Statements[index], declaredNames))
                    continue;

                for (var included = endIndex + 1; included <= index; included++)
                {
                    foreach (var name in GetDeclaredNames(block.Statements[included]))
                        declaredNames.Add(name);
                }

                endIndex = index;
                changed = true;
                break;
            }
        }

        return endIndex;
    }

    private static IEnumerable<string> GetDeclaredNames(StatementSyntax statement)
    {
        if (statement is not LocalDeclarationStatementSyntax localDeclaration)
            yield break;

        foreach (var variable in localDeclaration.Declaration.Variables)
        {
            var name = variable.Identifier.ValueText;
            if (!string.IsNullOrEmpty(name))
                yield return name;
        }
    }

    private static bool ReferencesAnyIdentifier(SyntaxNode node, HashSet<string> identifiers) =>
        node.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => identifiers.Contains(identifier.Identifier.ValueText));

    private static SyntaxList<StatementSyntax> CreateCatchStatements(StatementSyntax statement)
    {
        var method = statement.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
        if (method is not MethodDeclarationSyntax methodDeclaration)
            return default;

        if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefined
            && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return default;
        }

        if (methodDeclaration.DescendantNodes().OfType<YieldStatementSyntax>().Any())
            return default;

        if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword)
            && methodDeclaration.ReturnType.ToString() == "System.Threading.Tasks.Task")
        {
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ParseStatement("return;"));
        }

        return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ParseStatement("return default!;"));
    }

    /// <summary>
    /// Minimal unified-diff implementation. Good enough for human review and
    /// for `git apply --check` style validation; not optimized for large files.
    /// </summary>
    private static string UnifiedDiff(string originalLabel, string patchedLabel, string[] a, string[] b)
    {
        var sb = new StringBuilder();
        sb.Append("--- ").Append(originalLabel).Append('\n');
        sb.Append("+++ ").Append(patchedLabel).Append('\n');

        var lcs = LongestCommonSubsequence(a, b);
        int i = 0, j = 0, k = 0;
        var hunkLines = new List<string>();
        int hunkStartA = 0, hunkStartB = 0;
        bool inHunk = false;

        void FlushHunk()
        {
            if (!inHunk) return;
            int countA = hunkLines.Count(l => l.StartsWith(' ') || l.StartsWith('-'));
            int countB = hunkLines.Count(l => l.StartsWith(' ') || l.StartsWith('+'));
            sb.Append("@@ -").Append(hunkStartA + 1).Append(',').Append(countA)
              .Append(" +").Append(hunkStartB + 1).Append(',').Append(countB).Append(" @@\n");
            foreach (var l in hunkLines) sb.Append(l).Append('\n');
            hunkLines.Clear();
            inHunk = false;
        }

        while (i < a.Length || j < b.Length)
        {
            bool inLcs = k < lcs.Count && i < a.Length && j < b.Length && a[i] == lcs[k] && b[j] == lcs[k];
            if (inLcs)
            {
                if (inHunk) hunkLines.Add(" " + a[i]);
                i++; j++; k++;
            }
            else if (j < b.Length && (k >= lcs.Count || b[j] != lcs[k]))
            {
                if (!inHunk) { hunkStartA = i; hunkStartB = j; inHunk = true; }
                hunkLines.Add("+" + b[j]);
                j++;
            }
            else if (i < a.Length && (k >= lcs.Count || a[i] != lcs[k]))
            {
                if (!inHunk) { hunkStartA = i; hunkStartB = j; inHunk = true; }
                hunkLines.Add("-" + a[i]);
                i++;
            }
        }
        FlushHunk();
        return sb.ToString();
    }

    private static List<string> LongestCommonSubsequence(string[] a, string[] b)
    {
        // Classic O(n*m) LCS. Fine for sub-megabyte source files.
        var dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);

        var result = new List<string>();
        int x = a.Length, y = b.Length;
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1]) { result.Add(a[x - 1]); x--; y--; }
            else if (dp[x - 1, y] >= dp[x, y - 1]) x--;
            else y--;
        }
        result.Reverse();
        return result;
    }
}

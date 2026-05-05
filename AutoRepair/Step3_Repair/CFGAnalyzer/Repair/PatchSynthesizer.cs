using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace AutoRepair.CFGAnalyzer.Repair;

/// <summary>
/// Forward-propagation patch synthesizer.
///
/// For a reachable-exception violation, the only safe forward edit is to wrap
/// the offending statement in a try/catch that catches the specific exception
/// type. Backward analysis (computing weakest preconditions, refactoring callers)
/// is explicitly out of scope.
/// </summary>
public static class PatchSynthesizer
{
    public static async Task<(string patchedFilePath, string diffPath)> WritePatchAsync(
        Document doc,
        StatementSyntax statement,
        string exceptionType,
        string patchesDir)
    {
        var tree = await doc.GetSyntaxTreeAsync()
                   ?? throw new InvalidOperationException($"No syntax tree for {doc.FilePath}");
        var root = await tree.GetRootAsync();
        var originalText = await doc.GetTextAsync();

        var wrapped = WrapInTryCatch(statement, exceptionType);
        var newRoot = root.ReplaceNode(statement, wrapped);
        var formatted = Formatter.Format(newRoot, doc.Project.Solution.Workspace);
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
    /// "<file>.autorepair.bak" so the change is reversible.
    /// </summary>
    public static int ApplyInPlace(RepairPlan plan)
    {
        int applied = 0;
        foreach (var item in plan.Items)
        {
            if (item.PatchedFilePath == null) continue;
            if (string.IsNullOrEmpty(item.ContainingFilePath)) continue;
            if (!File.Exists(item.PatchedFilePath)) continue;
            if (!File.Exists(item.ContainingFilePath)) continue;

            var backup = item.ContainingFilePath + ".autorepair.bak";
            if (!File.Exists(backup))
                File.Copy(item.ContainingFilePath, backup);

            File.Copy(item.PatchedFilePath, item.ContainingFilePath, overwrite: true);
            applied++;
        }
        return applied;
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
            var backup = item.ContainingFilePath + ".autorepair.bak";
            if (!File.Exists(backup)) continue;
            File.Copy(backup, item.ContainingFilePath, overwrite: true);
            File.Delete(backup);
            restored++;
        }
        return restored;
    }

    private static StatementSyntax WrapInTryCatch(
        StatementSyntax statement,
        string exceptionType)
    {
        var leadingTrivia = statement.GetLeadingTrivia();
        var withoutLeading = statement.WithLeadingTrivia();

        var commentTrivia = SyntaxFactory.Comment(
            $"// [AutoRepair Step 3] Forward-propagated guard against {exceptionType}");

        var tryStmt = SyntaxFactory.TryStatement(
            tryKeyword: SyntaxFactory.Token(SyntaxKind.TryKeyword),
            block: SyntaxFactory.Block(withoutLeading),
            catches: SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause(
                    SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(exceptionType)),
                    null,
                    SyntaxFactory.Block())),
            @finally: null);

        return tryStmt
            .WithLeadingTrivia(leadingTrivia.Add(commentTrivia).Add(SyntaxFactory.ElasticCarriageReturnLineFeed));
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

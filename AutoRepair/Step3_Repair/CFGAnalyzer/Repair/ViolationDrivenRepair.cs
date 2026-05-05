using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoRepair.CFGAnalyzer.Repair;

// ---------- Data types ----------------------------------------------------

public sealed record Violation(
    string Predicate,
    string Assertion,
    Dictionary<string, JsonElement> Bindings,
    bool Result,
    string File,
    int Line,
    string TraceId)
{
    public string ExceptionType =>
        Bindings.TryGetValue("event.exception.type", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString() ?? ""
            : "";

    public string Detection =>
        Bindings.TryGetValue("detection", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString() ?? ""
            : "";
}

public enum RepairAction
{
    AutoPatchable,
    IntentionalValidator,
    TestFixtureException,
    DocumentedCancellation,
    NoSyntaxLocated,
    UnsupportedPredicate,
}

public sealed record RepairItem(
    Violation Violation,
    string ContainingMember,
    string ContainingFilePath,
    int? StatementStartLine,
    int? StatementEndLine,
    RepairAction Action,
    string ActionReason,
    List<string> PathConstraints,
    string? PatchedFilePath,
    string? DiffPath);

public sealed record RepairPlan(
    string GeneratedAt,
    string ViolationsFile,
    int TotalViolations,
    Dictionary<string, int> ByAction,
    List<RepairItem> Items);

// ---------- Loader --------------------------------------------------------

public static class ViolationLoader
{
    public static List<Violation> Load(string path)
    {
        if (!File.Exists(path)) return new List<Violation>();
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.Deserialize<List<Violation>>(json, opts) ?? new List<Violation>();
    }
}

// ---------- Driver --------------------------------------------------------

public static class ViolationDrivenRepair
{
    public static async Task<RepairPlan> RunAsync(
        Solution solution,
        string violationsPath,
        string outputDir)
    {
        var violations = ViolationLoader.Load(violationsPath);
        Directory.CreateDirectory(outputDir);
        var patchesDir = Path.Combine(outputDir, "patches");
        Directory.CreateDirectory(patchesDir);

        // Index documents by short file name for quick violation lookup.
        var docsByName = new Dictionary<string, List<Document>>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            foreach (var doc in project.Documents)
            {
                if (string.IsNullOrEmpty(doc.FilePath)) continue;
                var key = Path.GetFileName(doc.FilePath);
                if (!docsByName.TryGetValue(key, out var list))
                {
                    list = new List<Document>();
                    docsByName[key] = list;
                }
                list.Add(doc);
            }
        }

        var items = new List<RepairItem>();
        foreach (var v in violations)
        {
            var item = await PlanForViolationAsync(v, docsByName, patchesDir);
            items.Add(item);
        }

        var plan = new RepairPlan(
            GeneratedAt: DateTime.UtcNow.ToString("o"),
            ViolationsFile: violationsPath,
            TotalViolations: items.Count,
            ByAction: items.GroupBy(i => i.Action.ToString())
                           .ToDictionary(g => g.Key, g => g.Count()),
            Items: items);

        var planPath = Path.Combine(outputDir, "repair_plan.json");
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        }));

        return plan;
    }

    private static async Task<RepairItem> PlanForViolationAsync(
        Violation v,
        Dictionary<string, List<Document>> docsByName,
        string patchesDir)
    {
        if (!docsByName.TryGetValue(v.File, out var docs) || docs.Count == 0)
        {
            return new RepairItem(v, "<unknown>", v.File, null, null,
                RepairAction.NoSyntaxLocated, "Source file not found in solution",
                new List<string>(), null, null);
        }

        // Take the first match. Multi-project repos with same-named files are
        // a corner case we don't need to solve to ship a working tool.
        var doc = docs[0];
        var tree = await doc.GetSyntaxTreeAsync();
        var text = await doc.GetTextAsync();
        if (tree == null || text == null)
        {
            return new RepairItem(v, "<unknown>", doc.FilePath ?? v.File, null, null,
                RepairAction.NoSyntaxLocated, "Could not read syntax tree",
                new List<string>(), null, null);
        }

        if (v.Line < 1 || v.Line > text.Lines.Count)
        {
            return new RepairItem(v, "<unknown>", doc.FilePath ?? v.File, null, null,
                RepairAction.NoSyntaxLocated, $"Line {v.Line} outside file ({text.Lines.Count} lines)",
                new List<string>(), null, null);
        }

        var lineSpan = text.Lines[v.Line - 1].Span;
        var root = await tree.GetRootAsync();
        SyntaxNode? node;
        try { node = root.FindNode(lineSpan, getInnermostNodeForTie: true); }
        catch { node = null; }

        var member = node?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        var memberName = member?.ToString().Split('\n')[0].Trim() ?? "<unknown>";
        if (member is BaseMethodDeclarationSyntax mds && mds.Identifier() is { } id)
            memberName = id;

        // Find the smallest enclosing statement that's a direct child of a block,
        // since that's the unit we can safely wrap in try/catch.
        var statement = node?.AncestorsAndSelf().OfType<StatementSyntax>()
            .FirstOrDefault(s => s.Parent is BlockSyntax);
        var stmtStart = statement?.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var stmtEnd = statement?.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        // ---- Classify the violation ----
        var (action, reason) = Classify(v, statement, member);

        // Path constraints: a placeholder list. The CFG analyzer already prints
        // these to cfg_analyzer.log for the legacy entry-point traversal.
        // Surfacing them per-violation is a future refinement.
        var pathConstraints = new List<string>();

        if (action != RepairAction.AutoPatchable || statement == null || member == null)
        {
            return new RepairItem(v, memberName, doc.FilePath ?? v.File,
                stmtStart, stmtEnd, action, reason, pathConstraints, null, null);
        }

        var (patchedPath, diffPath) = await PatchSynthesizer.WritePatchAsync(
            doc, statement, v.ExceptionType, patchesDir);

        return new RepairItem(v, memberName, doc.FilePath ?? v.File,
            stmtStart, stmtEnd, action, reason, pathConstraints, patchedPath, diffPath);
    }

    private static (RepairAction action, string reason) Classify(
        Violation v, StatementSyntax? statement, MemberDeclarationSyntax? member)
    {
        if (v.Predicate.StartsWith("NoUnhandledException", StringComparison.Ordinal))
        {
            var ex = v.ExceptionType;

            // Test-fixture exceptions: nested types under a Tests namespace,
            // or names that look like test doubles. Patching production code to
            // catch a test-fixture exception is wrong: the type isn't even
            // visible to the production assembly.
            if (ex.StartsWith("Tests.", StringComparison.Ordinal)
                || ex.Contains("+Failure", StringComparison.Ordinal)
                || ex.Contains("+Boom", StringComparison.Ordinal)
                || ex.Contains(".Tests.", StringComparison.Ordinal))
            {
                return (RepairAction.TestFixtureException,
                    $"Exception '{ex}' originates in the test assembly");
            }

            // Documented cancellation tokens: TaskCanceledException is the
            // contracted way to signal cancellation.
            if (ex == "System.Threading.Tasks.TaskCanceledException"
                || ex == "System.OperationCanceledException")
            {
                return (RepairAction.DocumentedCancellation,
                    "Cancellation exceptions are part of the documented contract");
            }

            // Argument validators: an explicit throw of an Argument*Exception
            // is intentional input validation, not a bug.
            var isArgumentEx = ex.EndsWith("ArgumentException", StringComparison.Ordinal)
                || ex.EndsWith("ArgumentNullException", StringComparison.Ordinal)
                || ex.EndsWith("ArgumentOutOfRangeException", StringComparison.Ordinal);
            if (isArgumentEx && ContainsExplicitThrow(member))
            {
                return (RepairAction.IntentionalValidator,
                    $"Method explicitly throws an argument-validation exception");
            }

            if (statement == null)
            {
                return (RepairAction.NoSyntaxLocated,
                    "Could not locate a wrappable statement at the violation line");
            }

            return (RepairAction.AutoPatchable,
                $"Forward-propagation guard for unhandled {ex}");
        }

        return (RepairAction.UnsupportedPredicate,
            $"Predicate '{v.Predicate}' is not yet handled by the patch synthesizer");
    }

    private static bool ContainsExplicitThrow(MemberDeclarationSyntax? member)
    {
        if (member == null) return false;
        return member.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Any(t => t.Expression is ObjectCreationExpressionSyntax oc
                && oc.Type.ToString().EndsWith("Exception"));
    }
}

internal static class SyntaxExtensions
{
    public static string? Identifier(this BaseMethodDeclarationSyntax m) => m switch
    {
        MethodDeclarationSyntax md => md.Identifier.Text,
        ConstructorDeclarationSyntax cd => cd.Identifier.Text,
        _ => null
    };
}

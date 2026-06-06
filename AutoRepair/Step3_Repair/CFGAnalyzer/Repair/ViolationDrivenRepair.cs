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
        // Step 1's instrumenter records the violation's `file` as a path
        // relative to the source root (e.g. "Util\\Foo.cs"), but the doc
        // index keys on the bare filename. Try the bare-filename fallback
        // before giving up.
        if (!docsByName.TryGetValue(v.File, out var docs) || docs.Count == 0)
        {
            var baseName = Path.GetFileName(
                v.File.Replace('\\', Path.DirectorySeparatorChar)
                      .Replace('/',  Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(baseName)
                && !string.Equals(baseName, v.File, StringComparison.OrdinalIgnoreCase)
                && docsByName.TryGetValue(baseName, out var byBase) && byBase.Count > 0)
            {
                docs = byBase;
            }
            else
            {
                return new RepairItem(v, "<unknown>", v.File, null, null,
                    RepairAction.NoSyntaxLocated, "Source file not found in solution",
                    new List<string>(), null, null);
            }
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

        // Find the smallest enclosing statement we can wrap in try/catch.
        // Prefer one whose parent is a Block (clean replacement), and fall
        // back to any enclosing StatementSyntax that is *not itself* a
        // BlockSyntax — Roslyn's ReplaceNode can swap a single child in
        // IfStatement.Statement / SwitchSection / LabeledStatement etc.,
        // but slots typed as BlockSyntax (e.g. MethodDeclaration.Body)
        // reject a TryStatement and throw InvalidCastException at runtime.
        var statement = node?.AncestorsAndSelf().OfType<StatementSyntax>()
            .FirstOrDefault(s => s.Parent is BlockSyntax)
            ?? node?.AncestorsAndSelf().OfType<StatementSyntax>()
                .FirstOrDefault(s => s is not BlockSyntax);

        // Fallback: when the violation's line points to a member declaration
        // (e.g. the method signature line — Step 1 logs Enter Method using
        // the declaration's start line), there is no enclosing statement.
        // In that case, wrap the first statement of the member's body so the
        // whole method body is guarded.
        var wrapRemainingBlock = false;
        if (statement == null && member is BaseMethodDeclarationSyntax bmds)
        {
            statement = bmds.Body?.Statements.FirstOrDefault();
            wrapRemainingBlock = statement != null;
        }
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

        // For NoUnhandledException the catch type comes straight from the
        // observed exception. For the other predicates we map the predicate
        // family to the catch type that would actually fire if the violation
        // re-occurred at runtime (NullReferenceException for NotNull, etc.).
        var catchType = ResolveCatchType(v);
        var (patchedPath, diffPath) = await PatchSynthesizer.WritePatchAsync(
            doc, statement, catchType, patchesDir, wrapRemainingBlock);

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

            // Deliberate "not implemented / not supported" signals are explicit
            // developer contracts (abstract stubs, unsupported operations, test
            // doubles). Silently swallowing them with a fallback would hide the
            // signal, so they are never auto-patched.
            if (ex == "System.NotImplementedException"
                || ex == "System.NotSupportedException")
            {
                return (RepairAction.IntentionalValidator,
                    "NotImplementedException/NotSupportedException is a deliberate contract signal, not a repairable fault");
            }

            if (ContainsObservedExceptionThrow(member, ex))
            {
                return (RepairAction.IntentionalValidator,
                    $"Method explicitly throws the observed exception");
            }

            // Argument-family exceptions are caller-contract violations
            // (ArgumentException / ArgumentNullException / ArgumentOutOfRangeException).
            // They signal that the *caller* passed an invalid value, so swallowing
            // them with a try/catch would hide API misuse and is never a correct
            // repair. We never auto-patch them - even when the throw is delegated
            // to an internal helper, so the current method body has no direct
            // `throw` (e.g. guard libraries such as Ardalis.GuardClauses).
            var isArgumentEx = ex.EndsWith("ArgumentException", StringComparison.Ordinal)
                || ex.EndsWith("ArgumentNullException", StringComparison.Ordinal)
                || ex.EndsWith("ArgumentOutOfRangeException", StringComparison.Ordinal);
            if (isArgumentEx)
            {
                return (RepairAction.IntentionalValidator,
                    "Argument-family exception is a caller-contract violation, not an auto-patchable fault");
            }

            // Validation methods that delegate the throw to a caller-supplied
            // exception factory (a Func<...Exception> parameter, as used by guard
            // libraries) are intentionally throwing the caller's own exception
            // type, not failing. The observed base Exception is by design.
            if (DeclaresExceptionFactoryParameter(member))
            {
                return (RepairAction.IntentionalValidator,
                    "Method throws a caller-supplied exception factory (Func<Exception>), not an auto-patchable fault");
            }

            if (statement == null)
            {
                return (RepairAction.NoSyntaxLocated,
                    "Could not locate a wrappable statement at the violation line");
            }

            return (RepairAction.AutoPatchable,
                $"Forward-propagation guard for unhandled {ex}");
        }

        // Other Step 2 predicates each map to a runtime exception type that
        // would surface if the violation re-fired. We synthesise the same
        // forward-propagation try/catch wrap as for NoUnhandledException —
        // the catch type comes from ResolveCatchType.
        if (v.Predicate.StartsWith("NotNull",        StringComparison.Ordinal) ||
            v.Predicate.StartsWith("ValidCast",      StringComparison.Ordinal) ||
            v.Predicate.StartsWith("BufferBounds",   StringComparison.Ordinal) ||
            v.Predicate.StartsWith("IntegerOverflow",StringComparison.Ordinal))
        {
            if (statement == null)
            {
                return (RepairAction.NoSyntaxLocated,
                    $"Could not locate a wrappable statement at line {v.Line} for {v.Predicate}");
            }
            return (RepairAction.AutoPatchable,
                $"Forward-propagation guard for {v.Predicate}");
        }

        return (RepairAction.UnsupportedPredicate,
            $"Predicate '{v.Predicate}' is not yet handled by the patch synthesizer");
    }

    /// <summary>
    /// Map a violation's predicate to the .NET exception type whose catch
    /// would intercept the runtime fault if the violation reproduced.
    /// </summary>
    private static string ResolveCatchType(Violation v)
    {
        var p = v.Predicate;
        if (p.StartsWith("NoUnhandledException", StringComparison.Ordinal))
            return string.IsNullOrEmpty(v.ExceptionType) ? "System.Exception" : v.ExceptionType;
        if (p.StartsWith("NotNull",         StringComparison.Ordinal)) return "System.NullReferenceException";
        if (p.StartsWith("ValidCast",       StringComparison.Ordinal)) return "System.InvalidCastException";
        if (p.StartsWith("BufferBounds",    StringComparison.Ordinal)) return "System.ArgumentOutOfRangeException";
        if (p.StartsWith("IntegerOverflow", StringComparison.Ordinal)) return "System.OverflowException";
        return "System.Exception";
    }

    private static bool ContainsExplicitThrow(MemberDeclarationSyntax? member)
    {
        if (member == null) return false;
        return member.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Any(IsArgumentValidationThrow)
            || member.DescendantNodes()
            .OfType<ThrowExpressionSyntax>()
            .Any(t => IsExceptionExpression(t.Expression));
    }

    private static bool ContainsObservedExceptionThrow(MemberDeclarationSyntax? member, string exceptionType)
    {
        if (member == null || string.IsNullOrWhiteSpace(exceptionType)) return false;

        var shortName = exceptionType.Split('.').Last();
        return member.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Select(statement => statement.Expression)
            .Where(expression => expression != null)
            .Any(expression => IsExceptionExpression(expression!, exceptionType, shortName))
            || member.DescendantNodes()
                .OfType<ThrowExpressionSyntax>()
                .Any(expression => IsExceptionExpression(expression.Expression, exceptionType, shortName));
    }

    private static bool ContainsArgumentValidationCall(MemberDeclarationSyntax? member)
    {
        if (member == null) return false;
        return member.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(IsArgumentValidationCall);
    }

    /// <summary>
    /// True when the method accepts a caller-supplied exception factory such as
    /// <c>Func&lt;Exception&gt;</c> (the pattern used by guard libraries like
    /// Ardalis.GuardClauses). A method that throws via such a factory is
    /// intentionally throwing the caller's exception, not failing, so it must
    /// not be auto-patched.
    /// </summary>
    private static bool DeclaresExceptionFactoryParameter(MemberDeclarationSyntax? member)
    {
        if (member is not BaseMethodDeclarationSyntax method)
            return false;

        return method.ParameterList.Parameters.Any(p =>
        {
            var type = p.Type?.ToString();
            return type is not null
                && type.Contains("Func<", StringComparison.Ordinal)
                && type.Contains("Exception", StringComparison.Ordinal);
        });
    }

    private static bool IsArgumentValidationCall(InvocationExpressionSyntax invocation)
    {
        var expression = invocation.Expression.ToString();
        if (expression.StartsWith("Ensure.", StringComparison.Ordinal)
            || expression.StartsWith("Guard.", StringComparison.Ordinal)
            || expression.Contains(".ThrowIf", StringComparison.Ordinal)
            || expression.Contains("ExceptionFactory.Argument", StringComparison.Ordinal))
        {
            return true;
        }

        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
            IdentifierNameSyntax id        => id.Identifier.Text,
            GenericNameSyntax gn           => gn.Identifier.Text,
            _                              => string.Empty,
        };

        return name is "ThrowIfNull" or "ThrowIfNegative" or "ThrowIfNegativeOrZero"
            or "ThrowIfZero" or "ThrowIfGreaterThan" or "ThrowIfGreaterThanOrEqual"
            or "ThrowIfLessThan" or "ThrowIfLessThanOrEqual"
            || (name.StartsWith("Check", StringComparison.Ordinal)
                && (name.Contains("Argument", StringComparison.Ordinal)
                    || name.Contains("Range", StringComparison.Ordinal)
                    || name.Contains("Index", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Recognise both `throw new ArgumentException(...)` and factory-style
    /// patterns like `throw Ensure.ExceptionFactory.ArgumentException(...)`
    /// that ensure.that-style libraries use. Without this, factory throws
    /// would be classified as AutoPatchable and we'd generate patches that
    /// silently swallow deliberate input validation.
    /// </summary>
    private static bool IsArgumentValidationThrow(ThrowStatementSyntax t)
        => t.Expression != null && IsExceptionExpression(t.Expression);

    private static bool IsExceptionExpression(ExpressionSyntax expr)
    {
        switch (expr)
        {
            case ObjectCreationExpressionSyntax oc:
                return oc.Type.ToString().EndsWith("Exception", StringComparison.Ordinal);

            case InvocationExpressionSyntax inv:
            {
                // The "method name" of the invocation. For
                // `Foo.Bar.ArgumentException(...)` this is "ArgumentException".
                var name = inv.Expression switch
                {
                    MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                    IdentifierNameSyntax id        => id.Identifier.Text,
                    GenericNameSyntax gn           => gn.Identifier.Text,
                    _                              => string.Empty,
                };
                return name.EndsWith("Exception", StringComparison.Ordinal);
            }

            default:
                return false;
        }
    }

    private static bool IsExceptionExpression(ExpressionSyntax expr, string fullName, string shortName)
    {
        var text = expr.ToString();
        return text.Contains(fullName, StringComparison.Ordinal)
            || text.Contains(shortName, StringComparison.Ordinal);
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

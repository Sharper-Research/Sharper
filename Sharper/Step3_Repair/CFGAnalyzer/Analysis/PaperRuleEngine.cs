using System.Collections.Immutable;

namespace Sharper.CFGAnalyzer.Analysis;

public sealed record PaperExpression(string Operator, string? Atom, ImmutableArray<PaperExpression> Operands)
{
    public static PaperExpression Variable(string identity) => new("variable", identity, []);
    public static PaperExpression Constant(string value) => new("constant", value, []);
    public static PaperExpression Apply(string operation, params PaperExpression[] operands) => new(operation, null, [.. operands]);
    public static PaperExpression Bounds(PaperExpression array, PaperExpression index) =>
        Apply("||", Apply("<", index, Constant("0")), Apply(">=", index, Apply("len", array)));
    public static PaperExpression Null(PaperExpression value) => Apply("==", value, Constant("null"));
    public static PaperExpression Cast(PaperExpression value, PaperExpression type) =>
        Apply("&&", Apply("!=", value, Constant("null")), Apply("!", Apply("compatible", value, type)));

    public IReadOnlySet<string> FreeVariables() => Operator == "variable"
        ? new HashSet<string>(StringComparer.Ordinal) { Atom! }
        : Operands.SelectMany(operand => operand.FreeVariables()).ToHashSet(StringComparer.Ordinal);

    // Figure 2 Assign: identities denote resolved symbols; the expression language has no binders.
    public PaperExpression Substitute(string identity, PaperExpression value) =>
        Operator == "variable" && Atom == identity ? value : this with
        {
            Operands = [.. Operands.Select(operand => operand.Substitute(identity, value))]
        };

    public override string ToString()
    {
        if (Atom != null) return Atom;
        if (Operator == "!") return $"!({Operands[0]})";
        if (Operator is "len" or "compatible") return $"{Operator}({string.Join(", ", Operands)})";
        return $"({Operands[0]} {Operator} {Operands[1]})";
    }
}

[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperAssignment), "assign")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperGuard), "guard")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperFrame), "frame")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperUnsupported), "unsupported")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperCall), "call")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperArrayAllocation), "array-allocation")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperArrayRead), "array-read")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(PaperArrayWrite), "array-write")]
public abstract record PaperStatement;
public sealed record PaperAssignment(string Variable, PaperExpression Value) : PaperStatement;
public sealed record PaperGuard(PaperExpression Condition, bool Taken) : PaperStatement;
public sealed record PaperFrame(IReadOnlyList<string> Definitions) : PaperStatement;
public sealed record PaperUnsupported(string Reason) : PaperStatement;
public sealed record PaperCall(string Procedure, string Result, IReadOnlyList<PaperExpression> Arguments) : PaperStatement;
public sealed record PaperArrayAllocation(string Variable, int Length) : PaperStatement;
public sealed record PaperArrayRead(string Variable, PaperExpression Array, PaperExpression Index) : PaperStatement;
public sealed record PaperArrayWrite(PaperExpression Array, PaperExpression Index, PaperExpression Value) : PaperStatement;
public sealed record PaperEdge(string Target, PaperStatement Statement);
public sealed record PaperNode(string Id, int Line, IReadOnlyList<PaperEdge> Successors);
public sealed record PaperRuleStep(string Node, int Line, string Rule, string Input, string Output);
public sealed record PaperPropagationResult(
    string Status,
    string? EntryPredicate,
    IReadOnlyList<PaperRuleStep> Steps,
    IReadOnlyList<int> RelevantLines,
    string? Reason)
{
    public string RootStatus => "BlockedByPrintedRootDefinition";
    [System.Text.Json.Serialization.JsonIgnore]
    public PaperExpression? EntryExpression { get; init; }
    public string? ProposedEntryCondition { get; init; }
    public PaperForwardResult? ForwardModelCheck { get; init; }
    public string ForwardVerificationStatus => ForwardModelCheck?.Status ?? "NotPerformed";
    public string ForwardVerificationScope { get; init; } = "Selected failure site in the supported source/model semantics, not the generated patch or whole method";
}

public static class PaperRuleEngine
{
    public static (PaperExpression Predicate, string Rule) Transfer(PaperStatement statement, PaperExpression post)
    {
        var free = post.FreeVariables();
        return statement switch
        {
            PaperAssignment assignment when free.Contains(assignment.Variable) =>
                (post.Substitute(assignment.Variable, assignment.Value), "Assign"),
            PaperAssignment => (post, "Assign-Irrel"),
            PaperGuard { Taken: true } guard => (PaperExpression.Apply("&&", guard.Condition, post), "Guard-T"),
            PaperGuard guard => (PaperExpression.Apply("&&", PaperExpression.Apply("!", guard.Condition), post), "Guard-F"),
            PaperFrame frame when !frame.Definitions.Any(free.Contains) => (post, "Frame"),
            PaperFrame => throw new NotSupportedException("Frame defines a free variable of the propagated predicate"),
            PaperUnsupported unsupported => throw new NotSupportedException(unsupported.Reason),
            _ => throw new NotSupportedException("Unknown statement")
        };
    }

    // Figure 2 Join: each successor predicate has already crossed its incoming statement.
    public static PaperExpression Join(IReadOnlyList<PaperExpression> predicates)
    {
        if (predicates.Count == 0) return PaperExpression.Constant("false");
        return predicates.Skip(1).Aggregate(predicates[0],
            (combined, predicate) => PaperExpression.Apply("||", combined, predicate));
    }

    public static PaperPropagationResult Propagate(
        IReadOnlyList<PaperNode> nodes, string entry, string failure, PaperExpression initial)
    {
        var graph = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var steps = new List<PaperRuleStep>();
        if (!graph.ContainsKey(entry) || !graph.ContainsKey(failure)
            || nodes.Any(node => node.Successors.Any(edge => !graph.ContainsKey(edge.Target))))
            return new("Incomplete", null, steps, [], "Graph has missing entry, failure, or successor nodes");

        var reachesFailure = new HashSet<string>(StringComparer.Ordinal) { failure };
        bool changed;
        do
        {
            changed = false;
            foreach (var node in nodes)
                if (node.Successors.Any(edge => reachesFailure.Contains(edge.Target)))
                    changed |= reachesFailure.Add(node.Id);
        } while (changed);

        if (!reachesFailure.Contains(entry))
            return new("Unreachable", "false", steps, [], "No control-flow path from entry to this failure site");

        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var cache = new Dictionary<string, PaperExpression>(StringComparer.Ordinal);
        PaperExpression Visit(string identity)
        {
            if (cache.TryGetValue(identity, out var cached)) return cached;
            var node = graph[identity];
            if (identity == failure)
            {
                steps.Add(new(identity, node.Line, "Initialize", "template(failure,state)", initial.ToString()));
                return cache[identity] = initial;
            }
            if (!visiting.Add(identity)) throw new NotSupportedException("Cycle on a path to failure; loop semantics are not implemented");
            var predicates = new List<PaperExpression>();
            foreach (var edge in node.Successors.Where(edge => reachesFailure.Contains(edge.Target)))
            {
                var post = Visit(edge.Target);
                var (predicate, rule) = Transfer(edge.Statement, post);
                steps.Add(new(identity, node.Line, rule, post.ToString(), predicate.ToString()));
                predicates.Add(predicate);
            }
            var result = Join(predicates);
            if (predicates.Count > 1)
                steps.Add(new(identity, node.Line, "Join", string.Join("; ", predicates), result.ToString()));
            visiting.Remove(identity);
            return cache[identity] = result;
        }

        try
        {
            var predicate = Visit(entry);
            var relevant = steps.Where(step => step.Rule is not "Frame" and not "Assign-Irrel")
                .Select(step => step.Line).Distinct().Order().ToArray();
            return new("PropagatedSupportedSubset", predicate.ToString(), steps, relevant, null) { EntryExpression = predicate };
        }
        catch (NotSupportedException exception)
        {
            return new("Incomplete", null, steps, [], exception.Message);
        }
    }
}
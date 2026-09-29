namespace Sharper.CFGAnalyzer.Analysis;

public sealed record PaperEvaluation(bool Known, object? Value, string? Reason)
{
    public static PaperEvaluation Of(object? value) => new(true, value, null);
    public static PaperEvaluation Unknown(string reason) => new(false, null, reason);
}

public sealed record PaperRootResult(string Status, bool? IsCandidate, string? Reason);

public static class PaperEvaluator
{
    public static PaperRootResult EvaluateObservedActivation(
        bool inSlice, PaperStatement statement, PaperExpression fixedPredicate,
        IReadOnlyDictionary<string, object?> before, IReadOnlyDictionary<string, object?> after)
    {
        var result = EvaluateRoot(inSlice, statement, fixedPredicate, fixedPredicate, before, after);
        return result with { Reason = result.Reason ?? "Observed fixed-predicate activation; not literal Root or a proof of causality" };
    }

    public static PaperEvaluation Evaluate(PaperExpression expression, IReadOnlyDictionary<string, object?> state)
    {
        if (expression.Operator == "variable")
            return state.TryGetValue(expression.Atom!, out var value)
                ? PaperEvaluation.Of(value) : PaperEvaluation.Unknown($"Missing observed value: {expression.Atom}");
        if (expression.Operator == "constant")
        {
            if (expression.Atom == "null") return PaperEvaluation.Of(null);
            if (bool.TryParse(expression.Atom, out var boolean)) return PaperEvaluation.Of(boolean);
            if (int.TryParse(expression.Atom, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var integer)) return PaperEvaluation.Of(integer);
            return PaperEvaluation.Unknown("Unsupported constant; do not guess a value or runtime type");
        }
        if (expression.Operands.Length == 0) return PaperEvaluation.Unknown("Missing operands");
        var left = Evaluate(expression.Operands[0], state);
        if (!left.Known) return left;
        if (expression.Operator == "len")
            return left.Value is Array { Rank: 1 } array ? PaperEvaluation.Of(array.Length)
                : PaperEvaluation.Unknown("Array length requires an observed one-dimensional array");
        if (expression.Operator == "!")
            return left.Value is bool operand ? PaperEvaluation.Of(!operand) : PaperEvaluation.Unknown("Expected boolean operand");
        if (expression.Operator == "&&" && left.Value is false) return PaperEvaluation.Of(false);
        if (expression.Operator == "||" && left.Value is true) return PaperEvaluation.Of(true);
        if (expression.Operands.Length != 2) return PaperEvaluation.Unknown("Expected two operands");
        var right = Evaluate(expression.Operands[1], state);
        if (!right.Known) return right;
        if (expression.Operator is "==" or "!=")
        {
            bool equal;
            if (left.Value == null || right.Value == null) equal = left.Value == null && right.Value == null;
            else if (left.Value is int leftInteger && right.Value is int rightInteger) equal = leftInteger == rightInteger;
            else if (left.Value is bool leftBoolean && right.Value is bool rightBoolean) equal = leftBoolean == rightBoolean;
            else return PaperEvaluation.Unknown("Unsupported equality semantics");
            return PaperEvaluation.Of(expression.Operator == "==" ? equal : !equal);
        }
        if (left.Value is bool firstBoolean && right.Value is bool secondBoolean)
            return expression.Operator switch
            {
                "&&" => PaperEvaluation.Of(firstBoolean && secondBoolean),
                "||" => PaperEvaluation.Of(firstBoolean || secondBoolean),
                _ => PaperEvaluation.Unknown("Unsupported boolean operator")
            };
        if (left.Value is not int first || right.Value is not int second)
            return PaperEvaluation.Unknown("Expected Int32 operands");
        try
        {
            return expression.Operator switch
            {
                "<" => PaperEvaluation.Of(first < second),
                "<=" => PaperEvaluation.Of(first <= second),
                ">" => PaperEvaluation.Of(first > second),
                ">=" => PaperEvaluation.Of(first >= second),
                "int32.unchecked+" => PaperEvaluation.Of(unchecked(first + second)),
                "int32.unchecked-" => PaperEvaluation.Of(unchecked(first - second)),
                "int32.unchecked*" => PaperEvaluation.Of(unchecked(first * second)),
                "int32.checked+" => PaperEvaluation.Of(checked(first + second)),
                "int32.checked-" => PaperEvaluation.Of(checked(first - second)),
                "int32.checked*" => PaperEvaluation.Of(checked(first * second)),
                _ => PaperEvaluation.Unknown("Unsupported operator")
            };
        }
        catch (OverflowException)
        {
            return PaperEvaluation.Unknown("Checked arithmetic has no normal successor state because it overflows");
        }
    }

    // Printed Figure 2 Root: evaluate the distinct propagated predicates, not a fixed-predicate heuristic.
    public static PaperRootResult EvaluateRoot(
        bool inSlice, PaperStatement statement, PaperExpression predecessorPredicate,
        PaperExpression successorPredicate, IReadOnlyDictionary<string, object?> before,
        IReadOnlyDictionary<string, object?> after)
    {
        if (!inSlice) return new("Evaluated", false, "Statement is not in the backward slice");
        string? assigned = null;
        if (statement is PaperAssignment assignment)
        {
            assigned = assignment.Variable;
            var value = Evaluate(assignment.Value, before);
            if (!value.Known || !after.TryGetValue(assigned, out var observed))
                return new("Unknown", null, "Assignment transition lacks a known value");
            if (!SameScalar(value.Value, observed)) return new("InvalidTransition", null, "Post-state does not match the assignment");
        }
        else if (statement is PaperGuard guard)
        {
            var outcome = Evaluate(guard.Condition, before);
            if (!outcome.Known || outcome.Value is not bool taken) return new("Unknown", null, "Guard outcome is unknown");
            if (taken != guard.Taken) return new("InvalidTransition", null, "Observed branch does not match the guard");
        }
        else if (statement is not PaperFrame { Definitions.Count: 0 })
            return new("Unknown", null, "Statement transition semantics are unsupported");

        foreach (var identity in before.Keys.Union(after.Keys))
        {
            if (identity == assigned) continue;
            if (!before.TryGetValue(identity, out var original) || !after.TryGetValue(identity, out var observed))
                return new("Unknown", null, "Observed state domains differ");
            if (!IsScalar(original) || !IsScalar(observed))
                return new("Unknown", null, "Heap snapshots require explicit identity and mutation semantics");
            if (!SameScalar(original, observed)) return new("InvalidTransition", null, "An unassigned variable changed");
        }
        var predecessor = Evaluate(predecessorPredicate, before);
        var successor = Evaluate(successorPredicate, after);
        if (!predecessor.Known || !successor.Known || predecessor.Value is not bool wasEnabled || successor.Value is not bool isEnabled)
            return new("Unknown", null, "A propagated predicate is not a known boolean in the observed state");
        return new("Evaluated", !wasEnabled && isEnabled, null);
    }

    private static bool IsScalar(object? value) => value is null or int or bool;
    private static bool SameScalar(object? first, object? second) => IsScalar(first) && IsScalar(second) && Equals(first, second);
}
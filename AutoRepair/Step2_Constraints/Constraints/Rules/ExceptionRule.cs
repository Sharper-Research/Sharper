using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

/// <summary>
/// General-purpose fallback rule.
/// Catches exception types that are NOT handled by the dedicated forward rules
/// (NullDereferenceRule, InvalidCastingRule, IntegerOverflowRule).
/// Examples: ArgumentException, InvalidOperationException, etc.
/// </summary>
public sealed class ExceptionRule : IRule
{
    private static readonly string[] HandledByForwardRules =
    {
        "NullReferenceException",
        "InvalidCastException",
        "OverflowException"
    };

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        if (!string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase) ||
            evt.Variable is not { Name: { } name, Value: { } value })
        {
            return false;
        }

        // Skip exception types that dedicated forward rules handle
        foreach (var handled in HandledByForwardRules)
        {
            if (value.Contains(handled, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        violation = new ConstraintViolation(
            Predicate: "NoUnhandledException(event)",
            Assertion: "event.exception == null",
            Bindings: new Dictionary<string, object?>
            {
                ["event.exception.type"] = value,
                ["detection"] = "reachable-exception"
            },
            Result: false,
            File: evt.File,
            Line: evt.Line,
            TraceId: evt.Id);
        return true;
    }
}

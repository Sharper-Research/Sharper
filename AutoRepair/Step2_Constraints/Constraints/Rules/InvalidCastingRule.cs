using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

// ── FUTURE WORK: Unboxing-failure detection ─────────────────────────────
// Currently this rule catches:
//   Path 0 – Cast Attempt: tracks the source type before an explicit cast.
//   Path 1 – Failed 'as' cast: LogAsCast detects null result from non-null source.
//   Path 2 – InvalidCastException: correlates with the last Cast Attempt event.
//
// NOT yet covered:
//   - Unboxing failures:  (int)(object)"hi"  throws InvalidCastException but
//     the CastingRewriter currently skips value-type target casts, so Path 0
//     never fires and Path 2 falls back to the generic "ValidCast(event)"
//     violation (no source-type / target-type bindings).
//
// When the Instrumenter gains unboxing instrumentation (check CastingRewriter
// FUTURE WORK for more info), add a new path here to handle events with
// msg = "Unbox Failure" (or a cast.kind = "unbox" flag) and emit a richer
// ConstraintViolation with source-type, target-type, and cast-kind bindings.
//
// Jyoti note: "Unboxing failure will be good to catch! But I am not
// sure if the current methodology will be sufficient."
// ─────────────────────────────────────────────────────────────────────────
public sealed class InvalidCastingRule : IRule
{
    private TraceEvent? _lastCastAttempt;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // ─ Path 0: Track cast attempts (from CastingRewriter)
        if (string.Equals(evt.Message, "Cast Attempt", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name }
            && name.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            _lastCastAttempt = evt;
            return false;
        }

        // ─ Path 1: Failed 'as' cast (CastingRewriter emits "Invalid Cast")
        if (string.Equals(evt.Message, "Invalid Cast", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } castName, Value: { } sourceType }
            && castName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            var targetType = castName[5..];

            violation = new ConstraintViolation(
                Predicate: $"ValidCast({sourceType}, {targetType})",
                Assertion: $"typeof(source) is {targetType}",
                Bindings: new Dictionary<string, object?>
                {
                    ["source.type"] = sourceType,
                    ["target.type"] = targetType
                },
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            _lastCastAttempt = null;
            return true;
        }

        // ─ Path 2: InvalidCastException from exception logging
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("InvalidCastException", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["exception.type"] = exType
            };

            string predicate;
            string assertion;

            if (_lastCastAttempt?.Variable is { Name: { } attemptName, Value: { } attemptSource }
                && attemptName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
            {
                var targetType = attemptName[5..];
                bindings["source.type"] = attemptSource;
                bindings["target.type"] = targetType;

                predicate = $"ValidCast({attemptSource}, {targetType})";
                assertion = $"typeof(source) is {targetType}";
            }
            else
            {

                predicate = "ValidCast(event)";
                assertion = "cast does not throw InvalidCastException";
            }

            violation = new ConstraintViolation(
                Predicate: predicate,
                Assertion: assertion,
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            _lastCastAttempt = null;
            return true;
        }

        return false;
    }
}
using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

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
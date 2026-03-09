using AutoRepair.Step2Constraints.Models;  // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// FUTURE WORK: unboxing-failure detection
// right now this covers:
//   path 0: cast attempt tracking (knows the source type before a cast)
//   path 1: failed 'as' cast (null result from non-null source)
//   path 2: InvalidCastException correlation with the last cast attempt
// not yet covered:
//   unboxing failures like (int)(object)"hi" - the CastingRewriter skips
//   value-type target casts so path 0 never fires and path 2 falls back
//   to a generic violation without source/target type info
// when the instrumenter gets unboxing support, add a new path here
// for "Unbox Failure" events with richer bindings
//
// Jyoti note: "Unboxing failure will be good to catch! But I am not
// sure if the current methodology will be sufficient."

public sealed class InvalidCastingRule : IRule
{
    private TraceEvent? _lastCastAttempt; // remembers the most recent cast attempt for correlation

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // path 0: track cast attempts so we can correlate with later exceptions
        // the instrumenter emits "Cast Attempt" with name like "cast:TargetType"
        if (string.Equals(evt.Message, "Cast Attempt", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name }
            && name.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            _lastCastAttempt = evt; // save it for later correlation
            return false;           // not a violation by itself
        }

        // path 1: the 'as' cast returned null (instrumenter logs "Invalid Cast")
        // the variable name has the target type and the value has the source type
        if (string.Equals(evt.Message, "Invalid Cast", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } castName, Value: { } sourceType }
            && castName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            var targetType = castName[5..]; // everything after "cast:"

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

            _lastCastAttempt = null; // clear it since we handled it
            return true;
        }

        // path 2: InvalidCastException from exception logging
        // try to correlate it with the last cast attempt if we have one
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

            // if we have a recent cast attempt, use it for richer error info
            if (_lastCastAttempt?.Variable is { Name: { } attemptName, Value: { } attemptSource }
                && attemptName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
            {
                var targetType = attemptName[5..]; // target type from the attempt
                bindings["source.type"] = attemptSource;
                bindings["target.type"] = targetType;

                predicate = $"ValidCast({attemptSource}, {targetType})";
                assertion = $"typeof(source) is {targetType}";
            }
            else
            {
                // no cast attempt to correlate with, use generic message
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

            _lastCastAttempt = null; // done with this attempt
            return true;
        }

        return false; // not a casting event
    }
}
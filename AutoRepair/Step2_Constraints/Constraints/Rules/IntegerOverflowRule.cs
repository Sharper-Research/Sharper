using AutoRepair.Step2Constraints.Models;  // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// FUTURE WORK: narrowing-conversion (truncation) overflow
// if the instrumenter adds support for narrowing overflow detection,
// handle those events here (or in a new dedicated rule) with
// msg = "NarrowingOverflow" and bindings for source value/type and target type

public sealed class IntegerOverflowRule : IRule
{
    private TraceEvent? _lastOverflowEvent; // stores the last overflow event for correlation

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // path 1: direct overflow detected by the instrumenter
        // msg = "Overflow", name = "overflow:<operator>", value = "left|right|resultType"
        // like name="overflow:+", value="123456788889|1|Int32"
        if (string.Equals(evt.Message, "Overflow", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name, Value: { } value }
            && name.StartsWith("overflow:", StringComparison.OrdinalIgnoreCase))
        {
            _lastOverflowEvent = evt; // save for potential exception correlation

            var op = name[9..]; // grab the operator after "overflow:"
            var parts = value.Split('|'); // split "left|right|resultType"

            var bindings = new Dictionary<string, object?>
            {
                ["operator"] = op
            };

            // if the value had all three parts, include the operands and result type
            if (parts.Length >= 3)
            {
                bindings["left"] = parts[0];
                bindings["right"] = parts[1];
                bindings["resultType"] = parts[2];
            }

            violation = new ConstraintViolation(
                Predicate: $"NoOverflow({op})",
                Assertion: parts.Length >= 3
                    ? $"{parts[0]} {op} {parts[1]} fits in {parts[2]}" // nice readable message
                    : $"operation {op} does not overflow",             // fallback
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true;
        }

        // path 2: OverflowException from exception logging
        // try to correlate with the last overflow event if we have one
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("OverflowException", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["exception.type"] = exType
            };

            string predicate;
            string assertion;

            // try to use the saved overflow event for better error info
            if (_lastOverflowEvent?.Variable is { Name: { } overflowName, Value: { } overflowValue }
                && overflowName.StartsWith("overflow:", StringComparison.OrdinalIgnoreCase))
            {
                var op = overflowName[9..]; // the operator
                var parts = overflowValue.Split('|');
                bindings["operator"] = op;

                if (parts.Length >= 3)
                {
                    bindings["left"] = parts[0];
                    bindings["right"] = parts[1];
                    bindings["resultType"] = parts[2];
                    predicate = $"NoOverflow({parts[0]} {op} {parts[1]})";
                    assertion = $"{parts[0]} {op} {parts[1]} fits in {parts[2]}";
                }
                else
                {
                    predicate = $"NoOverflow({op})";
                    assertion = $"operation {op} does not overflow";
                }
            }
            else
            {
                // no overflow event to correlate with, use generic message
                predicate = "NoOverflow(event)";
                assertion = "arithmetic does not throw OverflowException";
            }

            violation = new ConstraintViolation(
                Predicate: predicate,
                Assertion: assertion,
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            _lastOverflowEvent = null; // clear it
            return true;
        }

        return false; // not an overflow event
    }
}

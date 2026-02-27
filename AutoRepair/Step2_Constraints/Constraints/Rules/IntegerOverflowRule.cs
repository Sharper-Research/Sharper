using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public sealed class IntegerOverflowRule : IRule
{
    private TraceEvent? _lastOverflowEvent;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // Direct overflow
        // msg = "Overflow"
        // var.name = "overflow:<operator>"       -> "overflow:+"
        // var.value = "left|right|resultType"    -> "123456788889|1|Int32"
        if (string.Equals(evt.Message, "Overflow", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name, Value: { } value }
            && name.StartsWith("overflow:", StringComparison.OrdinalIgnoreCase))
        {
            _lastOverflowEvent = evt;

            var op = name[9..]; // operator after "overflow:"
            var parts = value.Split('|');

            var bindings = new Dictionary<string, object?>
            {
                ["operator"] = op
            };

            if (parts.Length >= 3)
            {
                bindings["left"] = parts[0];
                bindings["right"] = parts[1];
                bindings["resultType"] = parts[2];
            }

            violation = new ConstraintViolation(
                Predicate: $"NoOverflow({op})",
                Assertion: parts.Length >= 3
                    ? $"{parts[0]} {op} {parts[1]} fits in {parts[2]}"
                    : $"operation {op} does not overflow",
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true;
        }

        // OverflowException from exception logging
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

            if (_lastOverflowEvent?.Variable is { Name: { } overflowName, Value: { } overflowValue }
                && overflowName.StartsWith("overflow:", StringComparison.OrdinalIgnoreCase))
            {
                var op = overflowName[9..];
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

            _lastOverflowEvent = null;
            return true;
        }

        return false;
    }
}

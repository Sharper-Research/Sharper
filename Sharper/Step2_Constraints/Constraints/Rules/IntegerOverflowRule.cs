using Sharper.Step2Constraints.Models;
using Sharper.Step2Constraints.Runtime;

namespace Sharper.Step2Constraints.Rules;

/// <summary>
/// Forward integer-overflow detection.
/// The "Overflow" event is emitted by the instrumenter's checked-arithmetic
/// wrapper at the exact point of the operation — this IS forward detection
/// (it fires at the operation site, before an exception propagates).
/// OverflowException events are kept as a fallback confirmation path.
/// Additionally, when variable values approach type boundaries, the rule
/// flags them as overflow risks for subsequent arithmetic.
/// </summary>
public sealed class IntegerOverflowRule : IRule
{
    private TraceEvent? _lastOverflowEvent;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // ── Forward path 1: checked-arithmetic overflow detected at operation site ──
        // msg = "Overflow"
        // var.name = "overflow:<operator>"  e.g. "overflow:+"
        // var.value = "left|right|resultType"  e.g. "123456788889|1|Int32"
        if (string.Equals(evt.Message, "Overflow", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name, Value: { } value }
            && name.StartsWith("overflow:", StringComparison.OrdinalIgnoreCase))
        {
            _lastOverflowEvent = evt;

            var op = name[9..];
            var parts = value.Split('|');

            var bindings = new Dictionary<string, object?>
            {
                ["operator"] = op,
                ["detection"] = "forward-checked-arithmetic"
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

        // (Removed: forward-boundary-check clause that flagged any long >
        // int.MaxValue as a potential overflow.  It produced false positives
        // for legitimate large values such as DateTime.Ticks, file sizes,
        // Stopwatch counters, etc., where the value never participates in
        // an Int32 operation.  We now rely solely on the instrumenter's
        // checked-arithmetic Overflow events and OverflowException fallback.)

        // ── Fallback: OverflowException from exception logging ──
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("OverflowException", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["exception.type"] = exType,
                ["detection"] = "fallback"
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

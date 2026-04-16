using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

/// <summary>
/// Forward null-dereference detection.
/// Tracks variables assigned null/default, then flags when a null-tracked
/// variable is accessed (member access, array index) BEFORE the runtime
/// throws NullReferenceException.  NullReferenceException events are kept
/// only as a fallback confirmation path.
/// </summary>
public sealed class NullDereferenceRule : IRule
{
    private bool _suppressNextNullReferenceExceptionType;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // ── Forward path 1: member access on a null-tracked receiver ──
        // Variable Update with name like "receiver.Member" where receiver is null-tracked.
        if (string.Equals(evt.Message, "Variable Update", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } name })
        {
            var dotIdx = name.IndexOf('.');
            if (dotIdx > 0)
            {
                var receiver = name[..dotIdx];
                if (!receiver.Equals("this", StringComparison.OrdinalIgnoreCase)
                    && state.IsNullTracked(receiver))
                {
                    violation = new ConstraintViolation(
                        Predicate: $"NotNull({receiver})",
                        Assertion: $"{receiver} != null before accessing .{name[(dotIdx + 1)..]}",
                        Bindings: new Dictionary<string, object?>
                        {
                            [receiver] = null,
                            ["access"] = name,
                            ["detection"] = "forward-member-access"
                        },
                        Result: false,
                        File: evt.File,
                        Line: evt.Line,
                        TraceId: evt.Id);
                    return true;
                }
            }

            // ── Forward path 2: array index on a null-tracked buffer ──
            // Variable Update with name "index:bufferName" where bufferName is null-tracked.
            if (name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
            {
                var bufName = name.Split(':').Last();
                if (state.IsNullTracked(bufName))
                {
                    violation = new ConstraintViolation(
                        Predicate: $"NotNull({bufName})",
                        Assertion: $"{bufName} != null before array indexing",
                        Bindings: new Dictionary<string, object?>
                        {
                            [bufName] = null,
                            ["detection"] = "forward-null-array"
                        },
                        Result: false,
                        File: evt.File,
                        Line: evt.Line,
                        TraceId: evt.Id);
                    return true;
                }
            }
        }

        // ── Forward path 3: runtime null-dereference signal ──
        // SafetyRewriter's try/catch detects NullReferenceException and logs
        // "Null Dereference" event.  This is still a forward signal because
        // it fires at the exact point of dereference (inside the instrumented
        // method), before the exception propagates.
        if (string.Equals(evt.Message, "Null Dereference", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["detection"] = "forward-runtime-signal"
            };

            if (evt.Variable is { Value: { } message })
                bindings["exception.message"] = message;

            violation = new ConstraintViolation(
                Predicate: "NotNull(dereference.receiver)",
                Assertion: "dereference receiver is not null",
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            _suppressNextNullReferenceExceptionType = true;
            return true;
        }

        // ── Fallback: NullReferenceException from exception logging ──
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase))
        {
            if (_suppressNextNullReferenceExceptionType)
            {
                _suppressNextNullReferenceExceptionType = false;
                return false;
            }

            violation = new ConstraintViolation(
                Predicate: "NotNull(dereference.receiver)",
                Assertion: "dereference receiver is not null",
                Bindings: new Dictionary<string, object?>
                {
                    ["exception.type"] = exType,
                    ["detection"] = "fallback"
                },
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            return true;
        }

        return false;
    }
}

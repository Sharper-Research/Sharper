using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public sealed class NullDereferenceRule : IRule
{
    private bool _suppressNextNullReferenceExceptionType;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        if (string.Equals(evt.Message, "Null Dereference", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["signal"] = "Null Dereference"
            };

            if (evt.Variable is { Value: { } message })
            {
                bindings["exception.message"] = message;
            }

            // Look back in state for the exact variable assignment that was null.
            // e.g. trace line 13: method=null is the real root cause, not the crash line.
            string originFile = evt.File;
            int originLine = evt.Line;
            string originTraceId = evt.Id;

            if (state.TryGetLastNullAssignment(out var nullVar, out var nullFile, out var nullLine, out var nullTraceId))
            {
                bindings["null.variable"] = nullVar;
                bindings["null.assigned_at_line"] = nullLine;
                originFile = nullFile;
                originLine = nullLine;
                originTraceId = nullTraceId;
            }

            violation = new ConstraintViolation(
                Predicate: "NotNull(dereference.receiver)",
                Assertion: "dereference receiver is not null",
                Bindings: bindings,
                Result: false,
                File: originFile,
                Line: originLine,
                TraceId: originTraceId);

            // SafetyRewriter logs Exception Type right after this; avoid duplicate.
            _suppressNextNullReferenceExceptionType = true;
            return true;
        }

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
                    ["exception.type"] = exType
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

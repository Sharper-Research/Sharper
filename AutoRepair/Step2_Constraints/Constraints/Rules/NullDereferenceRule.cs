using AutoRepair.Step2Constraints.Models;  // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// catches null dereference errors in two ways:
// 1) direct "Null Dereference" messages from the instrumenter
// 2) NullReferenceException from exception logging
// has suppression logic to avoid counting the same error twice
public sealed class NullDereferenceRule : IRule
{
    // flag to skip the next NullReferenceException event
    // because the instrumenter logs both a "Null Dereference" and then an "Exception Type" for the same error
    private bool _suppressNextNullReferenceExceptionType;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // path 1: the instrumenter caught a null dereference directly
        if (string.Equals(evt.Message, "Null Dereference", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["signal"] = "Null Dereference"
            };

            // grab the exception message if there is one
            if (evt.Variable is { Value: { } message })
            {
                bindings["exception.message"] = message;
            }

            violation = new ConstraintViolation(
                Predicate: "NotNull(dereference.receiver)",
                Assertion: "dereference receiver is not null",
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            // the instrumenter will log an Exception Type event right after this
            // so we need to suppress it to avoid duplicate violations
            _suppressNextNullReferenceExceptionType = true;
            return true;
        }

        // path 2: caught via exception type logging
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase))
        {
            // if we already reported this from path 1, skip it
            if (_suppressNextNullReferenceExceptionType)
            {
                _suppressNextNullReferenceExceptionType = false;
                return false; // already handled above
            }

            // standalone NullReferenceException that wasnt covered by path 1
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

        return false; // not a null dereference event
    }
}

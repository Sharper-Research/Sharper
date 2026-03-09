using AutoRepair.Step2Constraints.Models;  // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// the simplest rule: catches any Exception Type event
// this is the generic fallback that fires when no more specific rule handles the exception
public sealed class ExceptionRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // only care about "Exception Type" messages that have variable data
        if (!string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase) ||
            evt.Variable is not { Name: { } name, Value: { } value })
        {
            return false; // not an exception event
        }

        // report it as a violation, the exception type goes in the bindings
        violation = new ConstraintViolation(
            Predicate: "NoUnhandledException(event)",
            Assertion: "event.exception == null",
            Bindings: new Dictionary<string, object?>
            {
                ["event.exception.type"] = value
            },
            Result: false,
            File: evt.File,
            Line: evt.Line,
            TraceId: evt.Id);
        return true;
    }
}

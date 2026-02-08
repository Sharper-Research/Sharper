using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public sealed class ExceptionRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        if (!string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase) ||
            evt.Variable is not { Name: { } name, Value: { } value })
        {
            return false;
        }

        violation = new ConstraintViolation(
            Rule: "ExceptionRule",
            Message: $"Unhandled exception observed: {value}",
            File: evt.File,
            Line: evt.Line,
            TraceId: evt.Id);
        return true;
    }
}

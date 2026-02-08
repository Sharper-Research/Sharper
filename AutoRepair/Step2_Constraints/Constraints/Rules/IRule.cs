using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public interface IRule
{
    bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation);
}

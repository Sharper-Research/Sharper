using Sharper.Step2Constraints.Models;
using Sharper.Step2Constraints.Runtime;

namespace Sharper.Step2Constraints.Rules;

public interface IRule
{
    bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation);
}

using AutoRepair.Step2Constraints.Models;  // for TraceEvent and ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// interface that all constraint rules must implement
// each rule checks trace events for a specific kind of bug
public interface IRule
{
    // takes a trace event and the current state, returns true if it found a violation
    // the violation out parameter gets filled in with the details if it returns true
    bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation);
}

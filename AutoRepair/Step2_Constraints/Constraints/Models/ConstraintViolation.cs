namespace AutoRepair.Step2Constraints.Models;

// record that holds all the info about a single constraint violation
// each one of these gets written out to the violations.json file
public sealed record ConstraintViolation(
    string Predicate,                      // formal predicate expression like "BufferInBounds(index:buf, buf)"
    string Assertion,                      // human readable version of what was expected
    Dictionary<string, object?> Bindings,  // variable bindings at the time of violation (names to values)
    bool Result,                           // always false since it only gets created on violation
    string File,                           // source file where the violation happened
    int Line,                              // line number in that file
    string TraceId);                       // id of the trace event that triggered this

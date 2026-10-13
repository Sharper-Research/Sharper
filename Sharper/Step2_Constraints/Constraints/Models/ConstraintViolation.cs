namespace Sharper.Step2Constraints.Models;

public sealed record ConstraintViolation(
    string Predicate,
    string Assertion,
    Dictionary<string, object?> Bindings,
    bool Result,
    string File,
    int Line,
    string TraceId);

namespace AutoRepair.Step2Constraints.Models;

public sealed record ConstraintViolation(
    string Rule,
    string Message,
    string File,
    int Line,
    string TraceId);

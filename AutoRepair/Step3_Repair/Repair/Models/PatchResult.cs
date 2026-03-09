namespace AutoRepair.Step3Repair.Models;

// represents one source-level patch that a repair strategy produced
// basically says "here is what we changed in the code and why"
public sealed record PatchResult(
    string File,                        // source file path relative to project root
    int Line,                           // 1-based line number that was patched
    string RuleName,                    // which rule/violation type triggered this
    string Description,                 // human readable explanation of the change
    string OriginalCode,                // the original code that was there before
    string PatchedCode,                 // the new code we replaced it with
    ConstraintViolation Violation);     // the violation that motivated this patch

namespace AutoRepair.Step3Repair.Models;

// the full report that gets written to patches.json
// has summary stats plus the list of all individual patches
public sealed record PatchReport(
    string GeneratedAt,        // when this report was created
    string ViolationsFile,     // path to the violations.json input
    string TargetRoot,         // root directory of the target project
    int TotalViolations,       // how many violations were found in total
    int PatchesApplied,        // how many patches were actually applied
    int PatchesSkipped,        // how many we couldnt fix
    List<PatchResult> Patches); // the actual patches

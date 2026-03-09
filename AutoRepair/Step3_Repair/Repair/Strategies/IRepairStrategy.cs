using AutoRepair.Step3Repair.Models;   // for ConstraintViolation, PatchResult
using Microsoft.CodeAnalysis.CSharp;   // for CSharpSyntaxTree (Roslyn)

namespace AutoRepair.Step3Repair.Strategies;

// interface that every repair strategy has to implement
// each strategy knows how to fix a specific kind of violation
public interface IRepairStrategy
{
    // returns true if this strategy can handle the given violation
    // like the OffByOneRepairStrategy only handles OffByOne violations
    bool CanHandle(ConstraintViolation violation);

    // tries to produce a patched version of the source code at the violation location
    // returns the patch result, or null if it cant figure out how to fix it
    PatchResult? TryRepair(ConstraintViolation violation, string sourceCode, CSharpSyntaxTree tree);
}

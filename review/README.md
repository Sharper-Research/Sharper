# Sharper Review Guide

Read [PAPER_AMENDMENTS.md](PAPER_AMENDMENTS.md) first. It corrects the Root definition and separates model safety, test acceptance and empirical repair success.

## Reproduction

Windows x64, .NET 10 SDK, PowerShell 5.1+, and NuGet access are required. Run from the repository root with a fresh output directory:

```powershell
.\review\reproduce.ps1 -Output review/rerun
```

The script builds the solution, runs the analyzer tests, verifies a model containing a loop and a procedure call, checks the corrected activation example, accepts the synthetic bounds guard, and rejects the overfitted runtime candidate. It checks that both original fixture files are unchanged. Logs, TRX files, plans and source hashes are retained in the output directory.

| Check | Expected result |
| --- | --- |
| Analyzer unit suite | All pass; existing local-function CFG warnings may remain |
| Loop and call model | VerifiedInductiveModel; two safety locations checked |
| Fixed-predicate activation | ObservedActivation-v1; candidate true |
| Synthetic guard | Baseline 2/4 pass; candidate 4/4 pass; AcceptedByTests |
| Extracted runtime bug | Baseline 3/5 pass; candidate 4/5 pass; Rejected |

The deliberately faulty target projects are not expected to pass their original suites. Do not use solution-wide test success as the reproduction criterion.

## Implementation Map

| Claim | Implementation |
| --- | --- |
| Backward formula rules | [PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs) |
| Literal Root and explicit correction | [PaperEvaluation.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs) |
| Checked loop invariants | [PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs) |
| Automatic source loops and static calls | [PaperSourceModel.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs) |
| Procedure bodies and call frames | [PaperProgramVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperProgramVerifier.cs) |
| Sandbox test acceptance | [CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs) |
| Hash-checked application | [PatchSynthesizer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs) |

## Boundaries

Supported `while`, `for`, and `do` loops and direct static calls are now lowered automatically from C#. Finite-path exploration verifies only when every feasible path is exhausted within the budget; cutoffs return Incomplete. Tests include an unsafe callee result that must produce a counterexample. Explicit model invariants remain supplied and checked, not inferred. Recursive contracts, general heap effects, checked exceptional arithmetic, automatic trace-root discovery and full semantic repair correctness remain outside the demonstrated scope. The manuscript must use these boundaries rather than claim unrestricted conformance.

The lean ZIP is built with `review/package.ps1 -Evidence review/rerun` after reproduction. It includes tool source, fixtures, current evidence, the input paper and these amendments. It excludes build products, IDE metadata, cloned repositories, old report generators and unrelated tooling. A SHA-256 manifest records the included contents and Git revision when available. Existing output ZIPs are not overwritten; supply a fresh `-Output` path when rebuilding.
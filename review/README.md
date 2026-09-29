# Sharper Review Guide

Read [PAPER_AMENDMENTS.md](PAPER_AMENDMENTS.md) first. It corrects the Root definition and separates model safety, test acceptance and empirical repair success.

## Manuscript Revision

[paper-annotated.pdf](paper-annotated.pdf) contains 19 highlighted comments with proposed replacement wording and implementation evidence. [PAPER_REVIEW.md](PAPER_REVIEW.md) contains the same comments as a readable checklist. The original [paper.pdf](paper.pdf) is unchanged. This is an author revision copy, not a clean submission manuscript or a claim of publication acceptance.

The [paste-ready Evaluation section](PAPER_REVIEW.md#paste-ready-evaluation-section) includes research questions, the measured results table, two case studies, and threats to validity. It distinguishes 240 formula/execution input combinations, 98 analyzer tests, and two repair scenarios; none of these counts is substituted for another.

After applying the comments to the editable manuscript, rebuild and review the final PDF, confirm author approval, licensing and venue requirements, and cite the exact artifact commit. No manuscript source or submission-portal access is included in this workspace.

The optional annotation generator requires Python 3.12 and PyMuPDF; it is separate from the .NET reproduction workflow:

```powershell
python -m pip install -r review/requirements.txt
python review/annotate_paper.py
python review/annotate_paper.py --check
```

The check verifies every saved comment, unchanged original PDF bytes, page content streams and annotation-free rendering, and hashes of the reviewed implementation and test evidence. Regenerate comments after changing cited evidence or source files.

## Reproduction

Windows x64, .NET 10 SDK, PowerShell 5.1+, and NuGet access are required. Run from the repository root with a fresh output directory:

```powershell
.\review\reproduce.ps1 -Output review/rerun
```

The script builds the solution, runs the analyzer tests, verifies a model containing a loop and a procedure call, and checks the corrected activation example. It instruments a disposable runtime-fixture copy, runs its tests, detects violations from the fresh trace, accepts the synthetic bounds guard, and rejects the overfitted runtime candidate. It checks that both original fixture files are unchanged. Logs, traces, TRX files, plans, source hashes, and environment/timing metadata are retained in the output directory. The single elapsed-time observation is not a performance benchmark.

| Check | Expected result |
| --- | --- |
| Analyzer unit suite | 98/98 pass; no skipped tests or prior local-function CFG warnings |
| CFG ownership and invocation | Top-level/nested bodies retained; condition/return calls discovered; no declaration-only call edges; distinct lambda IDs |
| Formula/execution correspondence | 240 boundary-input combinations agree with emitted predicates and compiled C# behavior |
| Repeated loop-local array access | Later failing visit produces Counterexample; safe repeated access verifies |
| Validation input integrity | Modified test/project inputs are rejected even with unchanged test identities |
| Loop and call model | VerifiedInductiveModel; two safety locations checked |
| Fixed-predicate activation | ObservedActivation-v1; candidate true |
| Synthetic guard | Baseline 2/4 pass; candidate 4/4 pass; AcceptedByTests |
| Extracted runtime bug | Baseline 3/5 pass; candidate 4/5 pass; Rejected |
| Fresh runtime trace | Instrumented 3/5; two null-dereference observations at one site |

The deliberately faulty target projects are not expected to pass their original suites. Do not use solution-wide test success as the reproduction criterion.

## Implementation Map

| Claim | Implementation |
| --- | --- |
| Roslyn CFG ownership and call discovery | [ProjectAnalyzer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/ProjectAnalyzer.cs), [CallGraphBuilder.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphBuilder.cs) |
| Backward formula rules | [PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs) |
| Literal Root and explicit correction | [PaperEvaluation.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs) |
| Checked loop invariants | [PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs) |
| Automatic source loops and static calls | [PaperSourceModel.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs) |
| Procedure bodies and call frames | [PaperProgramVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperProgramVerifier.cs) |
| Sandbox test acceptance | [CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs) |
| Hash-checked application | [PatchSynthesizer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs) |

## Boundaries

The general Roslyn CFG/call-graph analysis and the restricted source/model verifier are separate. A collected lambda or local-function CFG is not evidence that the verifier supports all effects in that body. Call edges describe static possibilities under the configured resolvers, not a complete dynamic trace.

Supported `while`, `for`, and `do` loops and direct static calls are now lowered automatically from C#. Finite-path exploration verifies only when every feasible path is exhausted within the budget; cutoffs return Incomplete. Tests include an unsafe callee result that must produce a counterexample. Explicit model invariants remain supplied and checked, not inferred. Recursive contracts, general heap effects, checked exceptional arithmetic, automatic trace-root discovery and full semantic repair correctness remain outside the demonstrated scope. The manuscript must use these boundaries rather than claim unrestricted conformance.

The lean ZIP is built with `review/package.ps1 -Evidence review/rerun` after reproduction. It includes tool source, fixtures, current evidence, the input paper and these amendments. It excludes build products, IDE metadata, cloned repositories, old report generators and unrelated tooling. A SHA-256 manifest records the included contents and Git revision when available. Existing output ZIPs are not overwritten; supply a fresh `-Output` path when rebuilding.
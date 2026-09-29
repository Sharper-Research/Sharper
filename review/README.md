# Sharper Review Guide

Read [PAPER_AMENDMENTS.md](PAPER_AMENDMENTS.md) first. It corrects the Root definition and separates model safety, test acceptance and empirical repair success.

## Manuscript Revision

[paper-annotated.pdf](paper-annotated.pdf) contains 19 highlighted comments with proposed replacement wording and implementation evidence. [PAPER_REVIEW.md](PAPER_REVIEW.md) contains the same comments as a readable checklist. The original [paper.pdf](paper.pdf) is unchanged. This is an author revision copy, not a clean submission manuscript or a claim of publication acceptance.

The [paste-ready Evaluation section](PAPER_REVIEW.md#paste-ready-evaluation-section) includes research questions, the measured results table, three case studies, and threats to validity. It distinguishes 240 formula/execution combinations, 56 listing-candidate execution inputs, 128 analyzer tests, and three repair scenarios; none of these counts is substituted for another.

Images: [recorded evidence summary](evidence-summary.png) and [highlighted formula page](paper-formulas.png). The summary is rendered from validated TRX and JSON outcomes, not a terminal screenshot. The formula page reproduces the original manuscript; apply the Root correction in the [formula correspondence matrix](PAPER_AMENDMENTS.md#formula-correspondence) before submission.

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

The script builds the solution, runs the analyzer tests, verifies a model containing a loop and a procedure call, and checks the corrected activation example. It instruments a disposable runtime-fixture copy, runs its tests, detects violations from the fresh trace, accepts the synthetic bounds guard, and rejects the overfitted runtime candidate. It also generates, model-checks, and test-validates the two-loop listing candidate. All three original fixture files must remain unchanged. Logs, traces, TRX files, plans, source hashes, and environment/timing metadata are retained in the output directory. The single elapsed-time observation is not a performance benchmark.

| Check | Expected result |
| --- | --- |
| Analyzer unit suite | 128/128 pass; no skipped tests or prior local-function CFG warnings |
| Cyclic backward propagation | Solver-checked least fixed point; iteration cutoff returns Incomplete |
| Typed-trace activation | Event 2 activates fixed Q without supplied state pairs; synthetic trace fixture |
| CFG ownership and invocation | Top-level/nested bodies retained; condition/return calls discovered; no declaration-only call edges; distinct lambda IDs |
| Formula/execution correspondence | 240 boundary-input combinations agree with emitted predicates and compiled C# behavior |
| Repeated loop-local array access | Later failing visit produces Counterexample; safe repeated access verifies |
| Validation input integrity | Modified test/project inputs are rejected even with unchanged test identities |
| Loop and call model | VerifiedInductiveModel; two safety locations checked |
| Canonical Root | Root-ObservedActivation-v1; candidate true; code and replacement comment agree |
| Synthetic guard | Emitted candidate VerifiedAcyclicModel; baseline 2/4, candidate 4/4; AcceptedByTests |
| Extracted runtime bug | Emitted candidate Incomplete; baseline 3/5, candidate 4/5; Rejected |
| Paper Listing 1.1 | Two counterexample stages; emitted candidate VerifiedExploredModel; baseline 1/4, candidate 4/4; AcceptedByTests |
| Candidate model integrity | Stale candidate hashes and known safety counterexamples block test acceptance |
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

Root is no longer blocked by the printed definition: `--check-root` and the `--check-activation` alias evaluate the same corrected fixed-Q rule. Propagation without a transition reports `NotEvaluated`. The superseded two-predicate rule is a legacy diagnostic only. The [exact replacement](PAPER_AMENDMENTS.md#root-implemented-replacement) and highlighted PDF comment define Root operationally as observed activation, distinct from causal proof and guard synthesis.

Every emitted candidate receives its own saved-byte model check. This means an attempted check for every patch, not a proof for every patch. Cyclic source backward analysis separately iterates from false for up to 16 rounds, requiring solver-proved equivalence at all graph nodes before reporting convergence. It does not replace the loop-guard bounds template or infer invariants. Typed trace reconstruction requires complete scalar updates and unambiguous names; it is not arbitrary-trace causal localization.

The three cases support prototype feasibility and worked-example claims. Broad effectiveness claims need independent real defects, explicit selection criteria and denominators, stronger output oracles, comparative or ablation experiments, and repeatable cost measurements. Neither passing this protocol nor producing screenshots guarantees reviewer acceptance.

The listing path supports fixed-size one-dimensional Int32 array allocation, zero initialization, alias-visible stores and checked element accesses. It refines eligible while-loop guards up to eight times and reparses the emitted candidate for a separate, hash-bound model check. The generated policy stops loops before invalid writes; the fixture tests normal completion, not private buffer contents or functional equivalence. Dynamic-size allocation, general heap effects and allocation exhaustion remain outside this result.

The general Roslyn CFG/call-graph analysis and the restricted source/model verifier are separate. A collected lambda or local-function CFG is not evidence that the verifier supports all effects in that body. Call edges describe static possibilities under the configured resolvers, not a complete dynamic trace.

Supported `while`, `for`, and `do` loops and direct static calls are now lowered automatically from C#. Finite-path exploration verifies only when every feasible path is exhausted within the budget; cutoffs return Incomplete. Tests include an unsafe callee result that must produce a counterexample. Explicit model invariants remain supplied and checked, not inferred. Recursive contracts, general heap effects, checked exceptional arithmetic, automatic trace-root discovery and full semantic repair correctness remain outside the demonstrated scope. The manuscript must use these boundaries rather than claim unrestricted conformance.

The lean ZIP is built with `review/package.ps1 -Evidence review/rerun` after reproduction. It includes tool source, fixtures, current evidence, the input paper and these amendments. It excludes build products, IDE metadata, cloned repositories, old report generators and unrelated tooling. A SHA-256 manifest records the included contents and Git revision when available. Existing output ZIPs are not overwritten; supply a fresh `-Output` path when rebuilding.
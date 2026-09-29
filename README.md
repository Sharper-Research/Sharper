# Sharper

Research artifact for C# fault detection, predicate propagation, model verification and test-gated repair candidates. Start with [review/README.md](review/README.md) and [review/PAPER_AMENDMENTS.md](review/PAPER_AMENDMENTS.md).

Manuscript revisions are marked directly in [review/paper-annotated.pdf](review/paper-annotated.pdf), with proposed wording and evidence in [review/PAPER_REVIEW.md](review/PAPER_REVIEW.md). The annotated copy requires author revisions before submission; it is not a clean final manuscript.

The [evaluation and case studies](review/PAPER_REVIEW.md#paste-ready-evaluation-section) provide paste-ready manuscript text with measured results, three detailed scenarios, and limitations.

## Reproduce

Requires Windows x64, .NET 10 SDK, PowerShell 5.1+, and NuGet access. Other native Z3 platforms have not been validated.

```powershell
.\review\reproduce.ps1 -Output review/rerun
.\review\package.ps1 -Evidence review/rerun
```

## Scope

- Backward rules: Initialize, Assign, Assign-Irrel, Guard-T/F, Frame and Join for supported source expressions.
- Cyclic backward analysis: least-fixed-point iteration with solver-checked convergence; unresolved budgets and unsupported transfers return Incomplete.
- Root: one fixed-predicate observed-activation definition in code, CLI (`--check-root`) and the exact paper replacement comment. The printed two-predicate rule is legacy-only; propagation without observations reports NotEvaluated. Root is not a proof of causality or a synthesis rule.
- Trace activation: automatic scalar-pair reconstruction from type-tagged updates for a supplied statement and fixed predicate; missing observations remain Unknown.
- Source verification: automatic lowering of supported structured loops and direct static calls; exhaustive symbolic exploration with fail-closed budgets.
- Paper listing: fixed-size Int32 arrays, alias-visible reads/writes, counterexample-guided loop-bound guards, and a separate hash-bound safety check of the emitted candidate.
- Model verification: Z3, Int32 bitvectors, checked loop invariants, explicit callee bodies and isolated call frames. Unknown effects and recursion fail closed.
- Patch acceptance: isolated baseline, failing reproducer and regression runs; stable test identities; byte hashes; application restricted to one accepted candidate at a time.
- Every emitted candidate receives a hash-bound model-check outcome: supported guards verify; unsupported catch effects remain Incomplete.

Reviewer images: [review/evidence-summary.png](review/evidence-summary.png) and [review/paper-formulas.png](review/paper-formulas.png). The latter shows the original manuscript formulas with review highlights, not corrected typesetting.

**Model verification is not whole-C# verification, and test acceptance is not a correctness proof.** Finite supported source paths can now be checked without supplied models or invariants. Arbitrary loops, heap effects, recursion, checked-overflow paths and automatic trace-root extraction are not established. Unrestricted C# verification is not decidable in general; incomplete analyses are never reported as proofs. The paper's claims must match the amendments.

The repository contains the tool in `Sharper/`, its solution, and the reviewer scripts, fixtures, input paper and evidence in `review/`. Historical research tooling and cloned experiments have been archived outside the repository. The tracing driver temporarily edits source: use a disposable checkout.

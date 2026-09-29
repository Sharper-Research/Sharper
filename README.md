# Sharper

Research artifact for C# fault detection, predicate propagation, model verification and test-gated repair candidates. Start with [review/README.md](review/README.md) and [review/PAPER_AMENDMENTS.md](review/PAPER_AMENDMENTS.md).

Manuscript revisions are marked directly in [review/paper-annotated.pdf](review/paper-annotated.pdf), with proposed wording and evidence in [review/PAPER_REVIEW.md](review/PAPER_REVIEW.md). The annotated copy requires author revisions before submission; it is not a clean final manuscript.

## Reproduce

Requires Windows x64, .NET 10 SDK, PowerShell 5.1+, and NuGet access. Other native Z3 platforms have not been validated.

```powershell
.\review\reproduce.ps1 -Output review/rerun
.\review\package.ps1 -Evidence review/rerun
```

## Scope

- Backward rules: Initialize, Assign, Assign-Irrel, Guard-T/F, Frame and Join for supported source expressions.
- Root correction: explicit fixed-predicate observed activation, separate from the printed rule. Neither is a proof of causality.
- Source verification: automatic lowering of supported structured loops and direct static calls; exhaustive symbolic exploration with fail-closed budgets.
- Model verification: Z3, Int32 bitvectors, checked loop invariants, explicit callee bodies and isolated call frames. Unknown effects and recursion fail closed.
- Patch acceptance: isolated baseline, failing reproducer and regression runs; stable test identities; byte hashes; application restricted to one accepted candidate at a time.

**Model verification is not whole-C# verification, and test acceptance is not a correctness proof.** Finite supported source paths can now be checked without supplied models or invariants. Arbitrary loops, heap effects, recursion, checked-overflow paths and automatic trace-root extraction are not established. Unrestricted C# verification is not decidable in general; incomplete analyses are never reported as proofs. The paper's claims must match the amendments.

The repository contains the tool in `Sharper/`, its solution, and the reviewer scripts, fixtures, input paper and evidence in `review/`. Historical research tooling and cloned experiments have been archived outside the repository. The tracing driver temporarily edits source: use a disposable checkout.

# Analysis and Repair

The analyzer loads C# projects through Roslyn. Violation-driven analysis emits formula evidence, candidate patches, and diffs. `AutoPatchable` means candidate-generation eligibility, not acceptance.

## Verification

- Acyclic predicate propagation implements Initialize, Assign, Assign-Irrel, Guard, Frame, and Join.
- Cyclic propagation iterates least-fixed-point equations from false and checks semantic convergence with Z3. Source analysis allows 16 rounds; unresolved queries, unsupported transfers and budgets report Incomplete.
- The source adapter lowers supported `while`, `for`, and `do` loops, local assignments, increments, `break`/`continue`, and direct static calls with available synchronous bodies.
- Source exploration starts from all modeled inputs. `VerifiedExploredModel` requires exhausting all feasible paths; a reachable exploration limit returns `Incomplete`. No loop invariant is guessed or assumed.
- Explicit typed models support checked invariants and isolated procedure frames. A model proof is not a proof of every C# runtime effect.
- Root uses one canonical fixed-predicate observed-activation definition. `EvaluatePrintedRoot` preserves the superseded two-predicate rule only as a legacy diagnostic. Propagation without observations reports NotEvaluated. See [the manuscript amendments](../../review/PAPER_AMENDMENTS.md).

## Commands

From the repository root:

```powershell
dotnet run --project Sharper/Step3_Repair/CFGAnalyzer -- --verify-model review/models/loop-and-call.json
dotnet run --project Sharper/Step3_Repair/CFGAnalyzer -- --check-root review/models/activation.json
dotnet run --project Sharper/Step3_Repair/CFGAnalyzer -- --check-root review/models/trace-activation.json
.\review\reproduce.ps1 -Output review/rerun
```

The full tracing pipeline defaults to the deliberately faulty runtime fixture:

```powershell
.\Sharper\run_all.ps1
```

Use a disposable checkout because tracing temporarily edits source. For other targets pass `-SourceDir`, `-DriverProject`, and `-Solution`. Test gating additionally requires `-ValidationRoot` and `-ReproducerFilter`. `-ApplyPatches` requires that gate and cannot be combined with `-RestorePatches`.

The analyzer CLI accepts `--violations`, `--output-dir`, `--validation-root`, `--test-project`, and `--reproducer-filter`. Only a candidate with `AcceptedByTests` and unchanged source/candidate hashes may be applied using `--apply-patches`. Multiple independently tested patches are not composed. `--restore` restores repair backups.

Passing tests can still overfit an incomplete specification. Original-source analysis and emitted-candidate checking are separate. Every emitted candidate receives a hash-bound attempt from all modeled inputs: direct-return and loop guards can verify, while unsupported catch effects report Incomplete. Neither result establishes functional equivalence or absence of every possible C# failure.

Trace activation accepts a request containing TraceFile, File, Line, Statement and Predicate instead of Before/After pairs. Type-tagged scalar observations are reconstructed per thread and cleared at boundaries; incomplete or untyped observations are Unknown. The caller must provide unambiguous names and complete update logging. A detected fixed-predicate activation is not a causal-root proof.

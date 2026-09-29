# Analysis and Repair

The analyzer loads C# projects through Roslyn. Violation-driven analysis emits formula evidence, candidate patches, and diffs. `AutoPatchable` means candidate-generation eligibility, not acceptance.

## Verification

- Acyclic predicate propagation implements Initialize, Assign, Assign-Irrel, Guard, Frame, and Join.
- The source adapter lowers supported `while`, `for`, and `do` loops, local assignments, increments, `break`/`continue`, and direct static calls with available synchronous bodies.
- Source exploration starts from all modeled inputs. `VerifiedExploredModel` requires exhausting all feasible paths; a reachable exploration limit returns `Incomplete`. No loop invariant is guessed or assumed.
- Explicit typed models support checked invariants and isolated procedure frames. A model proof is not a proof of every C# runtime effect.
- Literal Root and the amended observed-activation rule remain separate. See [the manuscript amendments](../../review/PAPER_AMENDMENTS.md).

## Commands

From the repository root:

```powershell
dotnet run --project Sharper/Step3_Repair/CFGAnalyzer -- --verify-model review/models/loop-and-call.json
dotnet run --project Sharper/Step3_Repair/CFGAnalyzer -- --check-activation review/models/activation.json
.\review\reproduce.ps1 -Output review/rerun
```

The full tracing pipeline defaults to the deliberately faulty runtime fixture:

```powershell
.\Sharper\run_all.ps1
```

Use a disposable checkout because tracing temporarily edits source. For other targets pass `-SourceDir`, `-DriverProject`, and `-Solution`. Test gating additionally requires `-ValidationRoot` and `-ReproducerFilter`. `-ApplyPatches` requires that gate and cannot be combined with `-RestorePatches`.

The analyzer CLI accepts `--violations`, `--output-dir`, `--validation-root`, `--test-project`, and `--reproducer-filter`. Only a candidate with `AcceptedByTests` and unchanged source/candidate hashes may be applied using `--apply-patches`. Multiple independently tested patches are not composed. `--restore` restores repair backups.

Passing tests can still overfit an incomplete specification. The source checker verifies the selected failure site, not the generated patch or every other failure in the method.

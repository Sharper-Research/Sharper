# Step 3: Repair (CFG + Violation-Driven Patch Synthesis)

Step 3 closes the loop: it consumes `violations.json` from Step 2, locates the
faulty code with Roslyn, classifies each violation, and emits forward-only
guarded patches as side-by-side `.patched.cs` files plus unified diffs.

Layout:

```
Step3_Repair/
|-- README.md                       (this file)
|-- CFGAnalyzer/                    (analyzer + violation-driven repair)
|   |-- Analysis/                   (CFG, call graph, path conditions; @jpksh90)
|   `-- Repair/                     (violation loader, patch synthesizer)
`-- reports/                        (generated; gitignored)
    |-- cfg_analyzer.log
    |-- repair_plan.json            (per-violation classification + diff path)
    `-- patches/
        |-- <File>.L<line>.patched.cs
        `-- <File>.L<line>.diff
```

## Pipeline (Step 1 -> Step 2 -> Step 3)

`run_all.ps1` runs the steps in order. Step 3 consumes the restored
(uninstrumented) source plus Step 2's `violations.json`:

1. **Step 1** instruments the target, runs the test suite, writes
   `Step1_Tracing/trace_logs.jsonl`.
2. **Step 2** analyses that trace and writes
   `Step2_Constraints/reports/violations.json`.
3. **`restore_project.ps1`** reverts the target sources back to pristine state.
4. **Step 3 (CFGAnalyzer)** loads the project with `MSBuildWorkspace`, builds
   the call graph + CFGs (Task 1), then for every violation: locates the
   containing method/statement, classifies the violation, and synthesises a
   patch when appropriate (Tasks 2 + 3).

Run end-to-end:

```powershell
# Generate plan + side-by-side patches (default; never touches your source)
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1

# Apply the generated patches in-place (originals saved as *.autorepair.bak)
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1 -ApplyPatches

# Roll back: restores from *.autorepair.bak using the saved repair_plan.json
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1 -RestorePatches
```

## Violation classification

Step 3 categorises every violation before deciding whether to patch. This is
the crucial guard that stops AutoRepair from "fixing" code that was never
broken (e.g. test fixtures, argument validators, documented cancellation
paths).

| Action                  | Meaning                                                                 | Patched? |
| ----------------------- | ----------------------------------------------------------------------- | -------- |
| `AutoPatchable`         | Reachable runtime exception with a wrappable statement at the line.     | yes      |
| `TestFixtureException`  | Exception type lives in the test assembly (e.g. `Tests.X+Failure`).     | no       |
| `DocumentedCancellation`| `TaskCanceledException` / `OperationCanceledException` (contracted).    | no       |
| `IntentionalValidator`  | Explicit `throw new Argument*Exception(...)` for input validation.      | no       |
| `NoSyntaxLocated`       | Violation line is a method declaration, blank line, or unreachable.     | no       |
| `UnsupportedPredicate`  | Predicate other than `NoUnhandledException` (future work).              | no       |

The categories are emitted in `repair_plan.json` and printed in the console
summary so a reviewer can audit every decision.

## Forward-only patch shape

The synthesised patch wraps the offending statement in a `try/catch` that
catches the exact exception type Step 2 observed:

```csharp
// [AutoRepair Step 3] Forward-propagated guard against System.IndexOutOfRangeException
try
{
    alias[data.Length] = 99; // OOB via alias
}
catch (System.IndexOutOfRangeException)
{
}
```

This is intentionally the only edit Step 3 will perform. We explicitly do
**not** do backward analysis (weakest precondition, refactoring callers,
hoisting checks). Forward propagation is the only direction supported by the
current design.

## End-to-end demos

### ThrottleDebounce (no real bugs)

`run_all.ps1` finds 6 violations and classifies all 6 as non-bugs, so the
patches directory is empty. The 110-test suite continues to pass. This is the
correct outcome: ThrottleDebounce is not actually buggy.

### TargetApp (deliberate OOB)

A fixture violation at `TargetApp/Worker.cs:35` (the OOB alias write) produces:

* `reports/targetapp/repair_plan.json` with action `AutoPatchable`
* `reports/targetapp/patches/Worker.L35.patched.cs` (compiles cleanly)
* `reports/targetapp/patches/Worker.L35.diff`

Reproduce:

```powershell
dotnet run --project AutoRepair\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj `
  -- TargetApp\TargetApp.csproj `
  --violations AutoRepair\Step3_Repair\reports\fixtures\targetapp_violations.json `
  --output-dir AutoRepair\Step3_Repair\reports\targetapp
```

## Status

* Task 1 (CFG + call graph + path conditions): done in `CFGAnalyzer/Analysis/`.
* Task 2 (consume `violations.json`, locate containing methods, classify): done
  in `CFGAnalyzer/Repair/ViolationDrivenRepair.cs`.
* Task 3 (forward-propagated patch synthesis with side-by-side files +
  unified diffs, opt-in apply, restore-from-backup): done in
  `CFGAnalyzer/Repair/PatchSynthesizer.cs`.

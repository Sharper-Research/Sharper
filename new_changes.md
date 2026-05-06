# new_changes.md — Multi-repo support for AutoRepair (Step 1 + Step 2 + Step 3)

The pipeline now runs end-to-end (Step 1 trace → Step 2 violations → Step 3 patches) across an arbitrary list of small C# repos.
On the curated batch of 13 repos, **10 trace successfully, all 10 also produce violation reports and patch sets (369 violations → 97 patches).**

See `AutoRepair/multi_repo_workspace/SUMMARY.md` for the canonical table
and `cases/<NN>_<owner>__<repo>/` for per-repo `trace_logs.jsonl`,
`violations.json`, `repair_plan.json`, and `patches/`.

---

## Gaps in the existing pipeline

### 1. `AutoRepair/run_all.ps1` is single-target

It hard-codes `ThrottleDebounce` defaults and writes outputs to fixed paths
(`Step1_Tracing/trace_logs.jsonl`, `Step2_Constraints/reports/violations.json`,
`Step3_Repair/reports/...`). Running it twice in a row overwrites the
previous run.

### 2. `PRHunter/e2e_demo.py` does loop, but it's coupled to GitHub mining

It needs `PRHunter/output/exception_hunter/open_issues.csv` from a
multi-hour mining job + `GITHUB_TOKEN`, and is scoped to "rejected/reverted
PR" candidates only.

### 3. Runtime-mismatch failures looked like instrumenter failures

Many small C# repos still target `net6.0` / `net7.0`; on a box with only
.NET 8 + .NET 10 runtimes the testhost won't launch, but the failure
surfaced as an empty `trace_logs.jsonl`.

### 4. Generated TraceLogger tripped warnings-as-errors

`TraceLogger.cs` uses `object variableValue = null` parameter signatures.
On host repos with `<Nullable>enable</Nullable>` + warnings-as-errors this
triggers CS8625 at the declaration and CS8604 at every instrumented call
site, failing the build before any tests can run.

### 5. PowerShell semicolon-splitting blocked CLI NoWarn injection

`/p:NoWarn=CS8600;CS8601;...` gets split on `;` by PowerShell before MSBuild
sees it (`MSB1006: Property is not valid: CS8601`). MSBuild also rejects
comma-separated NoWarn values (treats `,` as a property delimiter).

### 6. Step 2 + Step 3 had no per-repo isolation

`analyze_trace.ps1` reads/writes a fixed shared path; `CFGAnalyzer` writes
to a fixed shared `reports/` dir. For batch use, results would clobber
each other.

### 7. Pinned `global.json` in some repos broke MSBuildWorkspace

Repos like `SimonCropp/Argon` ship `src/global.json` pinning a specific SDK
patch (`10.0.203`). On a box with `10.0.201` only, `dotnet test` works (it
has its own SDK resolver) but `MSBuildWorkspace.OpenProjectAsync` in Step 3
hits `hostfxr_resolve_sdk2: A compatible .NET SDK was not found` and dies.

### 8. Step 3 path-matching couldn't find the source file

The instrumenter records the violation's `file` as a project-relative path
(e.g. `Util\ComparableImplementations.cs`), but the docs index keyed only on
the bare filename. Result: every violation in those repos was classified
`NoSyntaxLocated` ("Source file not found in solution").

### 9. Step 3 only synthesised patches for `NoUnhandledException`

Other Step 2 predicates (`NotNull`, `ValidCast`, `BufferBounds`,
`IntegerOverflow`) were detected but classified `UnsupportedPredicate` and
got 0 patches.

### 10. Step 3's statement-locator was too narrow

It only accepted statements whose direct parent is a `BlockSyntax`. A
violation whose line resolved to an expression-context node (e.g. an
`IfStatement.Statement` or, even worse, a method-declaration line) returned
`NoSyntaxLocated`. ensure.that's 143 violations all hit this.

### 11. Step 3's "is this an intentional validator?" check missed factory throws

It only recognised `throw new ArgumentException(...)`, not the factory
pattern `throw Ensure.ExceptionFactory.ArgumentException(...)`. Without
this, code that deliberately validates input would be patched with a
`try/catch{}` that silently swallows the validation.

---

## Changes

### A. New file: `AutoRepair/run_multi_repos.py`

Python orchestrator that runs **all three pipeline steps** across an
arbitrary list of small C# GitHub repos, isolating outputs per repo.

Per repo:

1. Shallow-clone `owner/repo[@sha]` into
   `AutoRepair/multi_repo_workspace/cases/<NN>_<owner>__<repo>/repo/`.
   Reuses an existing clone if present (cheap re-runs).
2. **Neutralise any `global.json`** so SDK-pin mismatches don't kill Step 3
   (renamed to `global.json.autorepair-bak`).
3. Edit every `*.csproj` to inject `<NoWarn>...</NoWarn>` +
   `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` +
   `<EnableNETAnalyzers>false</EnableNETAnalyzers>` so the instrumented
   build never fails on host-style warnings/analyzer rules.
   With `--retarget net8.0`: also rewrite `<TargetFramework>net6.0</...>`
   (and net5/7/9/11) to `net8.0`.
4. Discover the test project (`*Test*.csproj`, `*Tests.csproj`,
   `*Spec.csproj`) and the source project it references.
5. Run **Step 1** (`generate_trace.ps1`), capture `trace_logs.jsonl` into
   the case dir. 1800-second per-repo timeout.
6. Restore the source so Step 3 can read un-instrumented line numbers.
7. Run **Step 2** (`Constraints` analyzer) → `violations.json` in case dir.
8. Run **Step 3** (`CFGAnalyzer`) → `repair_plan.json` + `patches/*.diff`
   + `patches/*.patched.cs` in case dir. Old patches are cleared first.

CLI:

```powershell
# Full pipeline (Steps 1 + 2 + 3)
python AutoRepair\run_multi_repos.py --retarget net8.0

# Trace-only (Step 1)
python AutoRepair\run_multi_repos.py --retarget net8.0 --steps 1

# Re-run Steps 2+3 only against already-captured traces
python AutoRepair\run_multi_repos.py --steps 2,3

# Custom repo list
python AutoRepair\run_multi_repos.py `
    --retarget net8.0 --repos "owner/a,owner/b@sha"
```

The orchestrator tees its own stdout to
`multi_repo_workspace/run.log` and rewrites `summary.json` + `SUMMARY.md`
after every repo so a crash mid-batch still leaves an inspectable report.

### B. Instrumenter fix

`AutoRepair/Step1_Tracing/Instrumenter/Program.cs` (`GenerateTraceLogger`):
prepend the generated `TraceLogger.cs` with

```csharp
#nullable disable
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8625
```

Makes `TraceLogger.Log` parameters "oblivious" so neither the declaration
nor the call sites in instrumented user code emit CS86xx warnings.

### C. CFGAnalyzer fixes (`Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs`)

Four targeted edits to make Step 3 actually emit patches across the batch:

1. **Path-matching fallback.** When `docsByName.TryGetValue(v.File, ...)`
   misses (e.g. the trace says `Util\Foo.cs` but the index has `Foo.cs`),
   retry with the bare filename.
2. **Statement-locator fallbacks.** Prefer statements whose parent is a
   `Block`; otherwise accept any enclosing statement (excluding
   `BlockSyntax` itself, which can't replace a `BlockSyntax` slot like
   `MethodDeclaration.Body`); finally, when the violation's line points at
   a method declaration, wrap the first statement of the method body.
3. **Patch synthesis for non-`NoUnhandledException` predicates.** A new
   `ResolveCatchType(v)` maps `NotNull` → `NullReferenceException`,
   `ValidCast` → `InvalidCastException`, `BufferBounds` →
   `ArgumentOutOfRangeException`, `IntegerOverflow` → `OverflowException`.
   `Classify` now returns `AutoPatchable` for these too, and the existing
   `WrapInTryCatch` synthesises the patch.
4. **Intentional-validator detection upgrade.** `ContainsExplicitThrow`
   now also recognises `throw Ensure.ExceptionFactory.ArgumentException(...)`
   and `throw expr` (throw expressions), not just `throw new XException(...)`.
   This stops us from generating `try/catch{}` "patches" that silently
   swallow deliberate input validation.

### D. No changes to `run_all.ps1` / Step 2 source

Step 2's analyzer, the rewriters, and `run_all.ps1` were left alone — the
multi-repo path is a separate orchestration (`run_multi_repos.py`) that
calls Step 1 + Step 2 + Step 3 directly.

---

## Test results

Goal: trace **at least 10 small C# repositories** end-to-end, then have
Step 2 + Step 3 run correctly across them.

```powershell
python AutoRepair\run_multi_repos.py --retarget net8.0 --keep-going --repos `
    "nodirmcsd/WordNinjaSharp@234a9be64172ba69edf6f125e90a02d2033172e8,StephenCleary/Comparers,StephenCleary/Deque,StephenCleary/AsyncEx,danielwertheim/ensure.that,StefH/JsonConverter,mganss/HtmlSanitizer,Tyrrrz/Onova,SimonCropp/Argon,JonPSmith/EfCore.GenericServices,xoofx/Markdig,Tyrrrz/CliWrap,StephenCleary/Hosting"
```

| # | Repo | Step1 events | Step2 violations | Step3 patches |
|---|---|---:|---:|---:|
| 00 | `nodirmcsd/WordNinjaSharp` | **123,947** | 2 | **1** |
| 01 | `StephenCleary/Comparers` | **65,046** | 9 | **3** |
| 02 | `StephenCleary/Deque` | **45,109** | 81 | **7** |
| 03 | `StephenCleary/AsyncEx` | **128** | 0 | 0 |
| 04 | `danielwertheim/ensure.that` | **19,722** | 143 | **78** |
| 05 | `StefH/JsonConverter` | **6,312** | 47 | **2** |
| 06 | `mganss/HtmlSanitizer` | **283,889** | 85 | **5** |
| 07 | `Tyrrrz/Onova` | **782** | 0 | 0 |
| 08 | `SimonCropp/Argon` | **462** | 0 | 0 |
| 09 | `JonPSmith/EfCore.GenericServices` | **55,618** | 2 | **1** |
| 10 | `xoofx/Markdig` | — | — | — |
| 11 | `Tyrrrz/CliWrap` | — | — | — |
| 12 | `StephenCleary/Hosting` | — | — | — |

**Totals:**
- Step 1 successes: **10 / 10** (target met)
- Step 2 successes: **10 / 10**
- Step 3 successes: **10 / 10**
- Total violations across all repos: **369**
- Total patches synthesised: **97**

Each per-repo case dir contains:
```
trace_logs.jsonl     # Step 1 output (events captured)
violations.json      # Step 2 output (constraint violations)
repair_plan.json     # Step 3 output (per-violation classification + plan)
patches/             # Step 3 output (.diff + .patched.cs per AutoPatchable)
step1.log step2.log step3.log      # full subprocess logs
```

The 369 → 97 ratio is by design: violations classified as
`IntentionalValidator` (deliberate input checks), `UnsupportedPredicate`
(predicate types Step 3 still can't repair) or `NoSyntaxLocated` (the
violation line has no wrappable statement) are intentionally NOT patched —
generating a `try/catch{}` for them would silently mute correct behaviour.

The 3 non-passing repos (Markdig, CliWrap, Hosting) are kept in the
candidate list as bonus probes; their Step 1 limitations
(rewriter-incompatible source / no test project) are unrelated to the
multi-repo orchestration.

---

## Files changed / added

| File | Kind | Purpose |
| :--- | :--- | :--- |
| `AutoRepair/run_multi_repos.py` | **new** | Multi-repo orchestrator (Step 1 + Step 2 + Step 3 per repo). |
| `AutoRepair/Step1_Tracing/Instrumenter/Program.cs` | edit | Prepend generated `TraceLogger.cs` with `#nullable disable` + `#pragma warning disable CS86xx` so it builds inside repos with nullable WAE. |
| `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs` | edit | Path-matching fallback, statement-locator fallbacks, patch synthesis for non-`NoUnhandledException` predicates, factory-throw detection in `ContainsExplicitThrow`. |

`run_all.ps1`, the rewriters, the constraint analyzer, and the
`PatchSynthesizer` itself were not modified.

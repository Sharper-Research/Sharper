# Patching / AutoRepair

Automated C# program repair: runtime tracing + constraint solving + CFG-based
path-condition analysis.

The pipeline runs end-to-end on `ThrottleDebounce` (110-test xUnit suite) and
on at least one external real-world target (`tghamm/Anthropic.SDK#197`, via
PRHunter). Some larger third-party repos still hit Instrumenter limits and
are the next thing to fix.

## Status

| Step | Module                                                       | State        | Description |
| :--: | :----------------------------------------------------------- | :----------- | :---------- |
| 1    | [Tracing](AutoRepair/Step1_Tracing/README.md)                | Done         | Roslyn instrumenter injects logging into any C# project; produces a structured JSONL execution trace. |
| 2    | [Constraints](AutoRepair/Step2_Constraints/README.md)        | Done         | Rule engine reads the trace and emits `violations.json` (buffer-bounds, exceptions, invalid casts, integer overflow, null deref). |
| 3    | [Repair / CFGAnalyzer](AutoRepair/Step3_Repair/README.md)    | Done         | [`AutoRepair/Step3_Repair/CFGAnalyzer/`](AutoRepair/Step3_Repair/CFGAnalyzer/) builds Roslyn CFGs + interprocedural call graph + path conditions (Task 1, by **@jpksh90**), then consumes `violations.json` to locate flagged methods (Task 2) and synthesises forward-only `try/catch` patches as side-by-side `.patched.cs` files plus unified diffs (Task 3). Opt-in `-ApplyPatches` / `-RestorePatches` switches in `run_all.ps1`. |

Auxiliary tooling:

* [`PRHunter/`](PRHunter/README.md): mines GitHub for failed C# bug-fix
  attempts (rejected / reverted PRs that left an open issue) and drives the
  AutoRepair pipeline against each candidate. Used for external validation,
  not part of the core repair loop.
* [`Research/CompetitorTools/`](Research/CompetitorTools): setup notes for
  SonarQube, CodeQL, Semgrep, and InferSharp (used to compare against
  AutoRepair findings).

## Quick Start

Run the full pipeline (Step 1 -> Step 2 -> restore -> Step 3) against
ThrottleDebounce:

```powershell
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1
```

Or run the steps individually:

```powershell
# 1. Instrument & trace
.\AutoRepair\Step1_Tracing\generate_trace.ps1

# 2. Analyze the trace for constraint violations
.\AutoRepair\Step2_Constraints\analyze_trace.ps1

# 3. Restore the target project to its original state
.\AutoRepair\Step1_Tracing\restore_project.ps1

# 4. Run the CFG analyzer (Step 3)
dotnet run --project AutoRepair\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj `
  -- ThrottleDebounce\ThrottleDebounce\ThrottleDebounce.csproj
```

## Pipeline overview

```
+---------------+     +-------------------+     +--------------------+
| Step 1: Trace | --> | Step 2: Constraints| --> | Step 3: CFGAnalyzer|
| (Instrumenter)|     | (Rule engine)      |     | (Roslyn CFG+paths) |
+---------------+     +-------------------+     +--------------------+
        |                    |                          |
trace_logs.jsonl       violations.json           cfg_analyzer.log
```

Step 3 reads `violations.json` (when present), classifies each violation, and
emits side-by-side patched files plus unified diffs in
`AutoRepair/Step3_Repair/reports/patches/`. It only edits source in-place when
`run_all.ps1 -ApplyPatches` is passed.

## Repository layout

```
Patching/
|-- AutoRepair/
|   |-- Step1_Tracing/          (Instrumenter + generate_trace.ps1)
|   |-- Step2_Constraints/      (Constraints analyzer + analyze_trace.ps1)
|   |-- Step3_Repair/           (CFGAnalyzer + reports/cfg_analyzer.log)
|   |   `-- CFGAnalyzer/        (Step 3 implementation, by @jpksh90)
|   `-- run_all.ps1             (Orchestrator: Step1 -> 2 -> restore -> 3)
|
|-- ThrottleDebounce/           (Primary integration target; 110 xUnit tests)
|-- TargetApp/                  (Tiny demo app with deliberate bugs)
|-- MicrosoftBugDemo/           (dotnet/runtime#120561 reproduction)
|
|-- PRHunter/                   (GitHub miner + e2e demo orchestrator)
|-- Research/
|   `-- CompetitorTools/        (SonarQube, CodeQL, Semgrep, InferSharp setup)
|
`-- Patching.sln
```

## Targets

| Target                                | Role                       | Notes |
| :------------------------------------ | :------------------------- | :---- |
| `ThrottleDebounce`                    | Primary integration target | Real rate-limiting library, 110-test xUnit suite. Pipeline instruments it, runs the tests, derives violations, then runs the CFG analyzer. |
| `TargetApp`                           | Smoke test                 | Tiny app with deliberate buffer overflows / casts / overflows; used during early development of the Instrumenter. |
| `MicrosoftBugDemo/`                   | Real-world repro           | Reproduces dotnet/runtime#120561 (NRE in `ReflectionXmlSerializationWriter`). |
| `AutoRepair/Step3_Repair/CFGAnalyzer/samples/` | CFG analyzer fixtures | Small projects validating the Step 3 call-graph resolvers. |

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download) (preview).
* Windows PowerShell 5.1+ (ships with Windows). PowerShell 7 also works.
* Python 3.10+ if you want to use PRHunter.

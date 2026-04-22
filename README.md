# AutoRepair

Automated C# program repair system using runtime tracing, constraint solving, and CFG-based path-condition analysis.

> **📍 All three pipeline steps now run end-to-end against ThrottleDebounce.**
> Run `powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1` to execute the full Step 1 → 2 → 3 chain.

---

## Status

| Step | Module | Status | Description |
| :--: | :--- | :--- | :--- |
| **1** | [Tracing](AutoRepair/Step1_Tracing/README.md) | **Done** | Roslyn-based instrumenter injects logging into any C# project; produces a structured JSONL execution trace. |
| **2** | [Constraints](AutoRepair/Step2_Constraints/README.md) | **Done** | Rule engine reads the trace and emits `violations.json` (buffer-bounds, exceptions, invalid casts, integer overflow, null dereference). FP fixes from `demo-test-repo` merged with null-origin tracking. |
| **3** | [Repair (CFGAnalyzer)](AutoRepair/Step3_Repair/README.md) | **Task 1 done** | [`AutoRepair/CFGAnalyzer/`](AutoRepair/CFGAnalyzer/) (by **@jpksh90**) builds Roslyn CFGs and traverses the call graph to print path constraints. Task 2 (consume `violations.json`) and Task 3 (patch synthesis) are next. |

---

## Architecture & Scalability

The system is designed to **work with any open-source C# project**, not just one hardcoded target.

| Principle | How |
| :--- | :--- |
| **Recursive scanning** | The Instrumenter accepts a root directory and instruments every `*.cs` file it finds, skipping `bin/` and `obj/`. |
| **In-place instrumentation** | Source files are backed up (`*.bak`) and overwritten directly, no `.csproj` edits needed. Works with MSBuild, `dotnet CLI`, etc. |
| **Universal logger** | A shared `TraceLogger.cs` is generated in the project root using `global::` qualified calls to avoid namespace conflicts. |
| **Safe restoration** | `restore_project.ps1` instantly reverts all source files from backups and removes `TraceLogger.cs`. |

---

## Quick Start

```powershell
# Run the full pipeline (Step 1 → Step 2 → restore → Step 3) against ThrottleDebounce
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1
```

Or run the steps individually:

```powershell
# 1. Instrument & trace (instruments target, runs tests, collects JSONL trace)
.\AutoRepair\Step1_Tracing\generate_trace.ps1

# 2. Analyze the trace for constraint violations
.\AutoRepair\Step2_Constraints\analyze_trace.ps1

# 3. Restore the target project to its original state
.\AutoRepair\Step1_Tracing\restore_project.ps1

# 4. Run the CFG analyzer (Step 3)
dotnet run --project AutoRepair\CFGAnalyzer\CFGAnalyzer.csproj -- ThrottleDebounce\ThrottleDebounce\ThrottleDebounce.csproj
```

Each step has its own detailed README. See the links in the Status table above.

---

## Pipeline Overview

```
┌─────────────────┐      ┌──────────────────────┐       ┌──────────────────────┐
│  Step 1: Trace  │ ───▶ │ Step 2: Constraints │ ───▶  │ Step 3: CFGAnalyzer  │
│  (Instrumenter) │      │  (Rule Engine)       │       │ (Roslyn CFG + paths) │
└─────────────────┘      └──────────────────────┘       └──────────────────────┘
        │                         │                              │
   trace_logs.jsonl         violations.json               cfg_analyzer.log
```

*Step 3 currently runs as a standalone analyzer; consuming `violations.json` to drive targeted CFG traversal is the next task.*

<!-- SCREENSHOT SUGGESTION: terminal showing the full pipeline run (Steps 1 → 2) -->

---

## Project Structure

```text
Patching/
├── AutoRepair/
│   ├── Step1_Tracing/                  # Instrumentation & trace collection
│   │   ├── generate_trace.ps1          # Run instrumentation + execute target
│   │   ├── restore_project.ps1         # Revert target to original state
│   │   ├── trace_logs.jsonl            # (Generated) structured execution trace
│   │   ├── trace_logs.txt              # (Generated) raw stdout capture
│   │   └── Instrumenter/              # Roslyn-based C# rewriting tool
│   │       ├── Program.cs
│   │       └── Rewriters/             # 7 SyntaxRewriters (see Step 1 README)
│   │
│   ├── Step2_Constraints/              # Constraint analysis
│   │   ├── analyze_trace.ps1           # Build & run the analyzer
│   │   ├── reports/violations.json     # (Generated) detected violations
│   │   └── Constraints/               # C# analyzer project
│   │       ├── Program.cs
│   │       ├── Models/                 # TraceEvent, ConstraintViolation
│   │       ├── Runtime/                # TraceReader, StateTracker
│   │       └── Rules/                  # IRule + 5 rule implementations
│   │
│   ├── Step3_Repair/                   # Output drop zone for Step 3
│   │   ├── README.md                   # Pointer to AutoRepair/CFGAnalyzer/
│   │   └── reports/cfg_analyzer.log    # (Generated) CFG traversal output
│   │
│   ├── CFGAnalyzer/                    # Step 3: Roslyn CFG + path conditions (by @jpksh90)
│   │
│   └── run_all.ps1                     # Orchestrator: Step 1 → 2 → restore → 3
│
├── ThrottleDebounce/                   # Integration target (110-test xUnit suite)
│   ├── ThrottleDebounce/               # Library under analysis (net10.0)
│   └── Tests/                          # xUnit test driver
│
├── TargetApp/                          # Minimal demo app with known bugs
│   ├── Program.cs
│   └── Worker.cs
│
└── Patching.sln
```

---

## Target Applications

| Target | Role | Details |
| :--- | :--- | :--- |
| **ThrottleDebounce** | Primary integration target | Real-world rate-limiting/debouncing library with a 110-test xUnit suite. The pipeline instruments it, runs the tests, derives violations, and feeds the restored source to the CFG analyzer. |
| **TargetApp** | Demo / smoke-test | A tiny app with deliberate bugs (buffer overflows, invalid casts, integer overflows) used during early development to validate the instrumenter and rules. |
| **AutoRepair/CFGAnalyzer/samples/** | CFG analyzer fixtures | Small projects (DelegateDemo, GenericsDemo, etc.) used by **@jpksh90** to validate the Step 3 call-graph resolvers. |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (preview)
- PowerShell 5.1+ (ships with Windows)

# AutoRepair

Automated C# program repair system using runtime tracing, constraint solving, and patch generation.

> **All three steps are complete.** The full pipeline — trace, analyze, repair — is functional.

---

## Status

| Step | Module | Status | Description |
| :--: | :--- | :--- | :--- |
| **1** | [Tracing](AutoRepair/Step1_Tracing/README.md) | **Done** | Roslyn-based instrumenter injects logging into any C# project; produces a structured JSONL execution trace. |
| **2** | [Constraints](AutoRepair/Step2_Constraints/README.md) | **Done** | Rule engine reads the trace and emits `violations.json` (buffer-bounds, off-by-one, null dereferences, exceptions, invalid casts, integer overflow). |
| **3** | [Repair](AutoRepair/Step3_Repair/README.md) | **Done** | Roslyn-based patch generator reads violations and produces source-level fixes (bounds guards, null checks, checked arithmetic, safe casts, try-catch wrapping). |

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
# 1. Instrument & trace (instruments target, runs tests, collects JSONL trace)
.\AutoRepair\Step1_Tracing\generate_trace.ps1

# 2. Analyze the trace for constraint violations
.\AutoRepair\Step2_Constraints\analyze_trace.ps1

# 3. Generate patches from violations (dry-run)
.\AutoRepair\Step3_Repair\generate_patches.ps1

# 3b. Apply patches to source files
.\AutoRepair\Step3_Repair\generate_patches.ps1 -Apply

# 4. Restore the target project to its original state
.\AutoRepair\Step1_Tracing\restore_project.ps1
```

Each step has its own detailed README. See the links in the Status table above.

---

## Pipeline Overview

```
┌─────────────────┐      ┌──────────────────────┐       ┌─────────────────┐
│  Step 1: Trace  │ ───▶ │ Step 2: Constraints │ ───▶  │  Step 3: Repair │
│  (Instrumenter) │      │  (Rule Engine)       │       │  (Patch Gen)    │
└─────────────────┘      └──────────────────────┘       └─────────────────┘
        │                         │                            │
   trace_logs.jsonl         violations.json              patches.json
```

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
│   │       └── Rules/                  # IRule + 4 rule implementations
│   │
│   └── Step3_Repair/                   # Patch generation
│       ├── generate_patches.ps1        # Build & run the patch generator
│       ├── reports/patches.json        # (Generated) patch report
│       └── Repair/                     # C# Roslyn-based patch generator
│           ├── Program.cs
│           ├── Models/                 # ConstraintViolation, PatchResult, PatchReport
│           ├── Core/                   # PatchEngine orchestrator
│           └── Strategies/             # 6 IRepairStrategy implementations
│
├── BrightHrKata/                       # Primary target app (checkout kata)
│   ├── Checkout/                       # Source code under test
│   └── Checkout.Tests/                 # MSTest suite that drives tracing
│
├── TargetApp/                          # Minimal demo app with known bugs
│   ├── Program.cs
│   └── Worker.cs
│
├── nodatime/                           # NodaTime library (real-world scalability target)
│
└── Patching.sln
```

---

## Target Applications

| Target | Role | Details |
| :--- | :--- | :--- |
| **BrightHrKata/Checkout** | Primary target | A supermarket checkout pricing kata with tests. The pipeline instruments it and runs its test suite to generate traces. See [BrightHrKata/README.md](BrightHrKata/README.md). |
| **TargetApp** | Demo / smoke-test | A tiny app with deliberate bugs (buffer overflows, invalid casts, integer overflows) used during early development to validate the instrumenter and rules. |
| **nodatime/** | Reference codebase | The [NodaTime](https://nodatime.org/) date/time library, included as a large real-world C# project to test the instrumenter's scalability beyond small kata-sized apps. |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (preview)
- PowerShell 5.1+ (ships with Windows)

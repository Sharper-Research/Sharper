# Step 1 – Tracing

This module injects runtime logging into the **TargetApp** source tree so we can capture detailed execution traces (control flow, variable updates, heap activity, and exceptions) while the program runs.

## How it works

1. **Instrumenter** (a Roslyn tool) scans every `*.cs` file under `TargetApp/`, skipping `bin/`, `obj/`, and previously generated artifacts.
2. Each file is backed up as `*.cs.bak`, then rewritten in-place with tracing statements that call a generated `TraceLogger` helper.
3. After instrumentation, `generate_trace.ps1` runs `dotnet run --project TargetApp/TargetApp.csproj` so the *instrumented* app executes.
4. Every injected `TraceLogger.Log(...)` appends structured JSON to `trace_logs.jsonl` (plus stdout mirroring in `trace_logs.txt`), giving us a chronological view of the program’s behavior.
5. `restore_project.ps1` removes `TraceLogger.cs` and copies every `*.bak` file back over the instrumented file so the TargetApp codebase returns to its original state.

## Key files

| File/Folder | Purpose |
| --- | --- |
| `generate_trace.ps1` | Orchestrates Step 1: cleans old logs, builds & runs the Instrumenter against `TargetApp/`, sets `TRACE_LOG_JSON_PATH`, and executes the instrumented app. |
| `restore_project.ps1` | Cleans up after tracing by deleting `TraceLogger.cs` and restoring `*.cs.bak` backups throughout `TargetApp/`. |
| `trace_logs.jsonl` | Structured trace output (JSON Lines). Each line captures GUID id, timestamp, thread id, file, line, message, and optional `{ name, val }` for variables. |
| `trace_logs.txt` | Raw stdout captured from the TargetApp run; stderr still prints to the console so crashes remain visible. |
| `Instrumenter/Instrumenter.csproj` | Build project for the Roslyn-based instrumentation tool. |
| `Instrumenter/Program.cs` | Entry point: generates `TraceLogger.cs`, discovers target files, and runs the rewriter pipeline (Line annotations → Safety → ControlFlow → Variable → Heap). |
| `Instrumenter/Rewriters/LineNumberAnnotator.cs` | Adds stable line annotations before rewriting so injected logs report the original source line numbers. |
| `Instrumenter/Rewriters/TraceRewriterBase.cs` | Shared helpers for all rewriters (builds `TraceLogger` statements, wraps single statements in blocks, retrieves annotated line numbers). |
| `Instrumenter/Rewriters/SafetyRewriter.cs` | Adds global try/catch wrappers, logs exceptions, emits infinite-loop watchdogs, and warns on null assignments. |
| `Instrumenter/Rewriters/ControlFlowRewriter.cs` | Logs method entries, branch selections, loop bodies, switch cases, and jump statements (break/continue/return). |
| `Instrumenter/Rewriters/VariableRewriter.cs` | Logs assignments, increments/decrements, denominators before division, array index expressions, and initial local values. |
| `Instrumenter/Rewriters/HeapRewriter.cs` | Logs allocations of arrays/collections inside blocks & switch sections, including inferred sizes (e.g., `Heap State: buf1 (int[] size=10)`). |

## Running Step 1

```powershell
# From repo root (Patching/)
.\AutoRepair\Step1_Tracing\generate_trace.ps1
```

Outputs are written to `AutoRepair\Step1_Tracing\trace_logs.jsonl` and `trace_logs.txt`. Inspect the JSONL file to correlate events with source locations.

## Restoring the target project

```powershell
.\AutoRepair\Step1_Tracing\restore_project.ps1
```

This deletes the generated `TraceLogger.cs` and restores every `*.cs` file from its `.bak`, returning `TargetApp/` to a pre-instrumented state. Always run this before committing or rerunning instrumentation to avoid stacking modifications.

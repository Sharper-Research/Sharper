# Step 1: Tracing

> **Status: Complete.** The instrumenter works end-to-end. Outputs feed into [Step 2 (Constraints)](../Step2_Constraints/README.md).

This is the *concrete execution* stage of the professor's "Proactive Repair from One Fault" pipeline. It injects runtime logging into a target C# project's source tree so we can capture a detailed execution trace (control-flow decisions, heap state, variable values, type casts, arithmetic overflows, exceptions) while the program runs.

## Verified runs

| Target | Files | Tests | Trace events | Notes |
|---|---|---|---|---|
| `ThrottleDebounce` (net8) | 7 | 110 passing | **10,446** | Primary integration target |
| `nodirmcsd/WordNinjaSharp#3` (net6) | 1 | 8 passing / some failing (signal) | **123,947** | Patch generated at WordNinja.cs:172 |
| `AngleSharp/AngleSharp.Css#183` (net8) | 8 | passing | large | 934 ValidCast violations detected |

### Instrumenter compatibility fixes (May 2026)

| Bug | Fix |
|---|---|
| `thrownew` - loop safety `ThrowStatement` had no trailing space | Use `ParseStatement("throw new ...")` instead of `ThrowStatement(ParseExpression(...))` |
| `catch (System.Exception__autoRepairCaughtEx)` - missing space | Add leading whitespace trivia to `CatchDeclaration` identifier |
| Pack step crashes on repos with `GeneratePackageOnBuild=true` and hardcoded developer paths | Pass `/p:GeneratePackageOnBuild=false` to `dotnet test` |
| Null byte (0x00) in JSON trace from control-char strings | `EJ()` now escapes all chars < 0x20 as `\uXXXX` |

---

## How It Works

1. **Instrument**: The Instrumenter (a Roslyn `CSharpSyntaxRewriter` tool) scans every `*.cs` file under the target directory, skipping `bin/`, `obj/`, and previously generated artifacts.
2. **Backup**: Each file is backed up as `*.cs.bak`, then rewritten in-place with tracing statements that call a generated `TraceLogger` helper.
3. **Execute**: `generate_trace.ps1` runs the target's test suite (or executable) so the *instrumented* code executes.
4. **Collect**: Every injected `TraceLogger.Log(...)` appends structured JSON to `trace_logs.jsonl`. Console output is also captured in `trace_logs.txt`.
5. **Restore**: `restore_project.ps1` copies every `*.bak` back and removes `TraceLogger.cs`, returning the target to its original state.

<!-- SCREENSHOT SUGGESTION: terminal output of generate_trace.ps1 running successfully -->

---

## Running Step 1

```powershell
# From repo root (Patching/)
.\AutoRepair\Step1_Tracing\generate_trace.ps1
```

By default, this targets `ThrottleDebounce/ThrottleDebounce` and runs its test suite. You can override:

```powershell
.\AutoRepair\Step1_Tracing\generate_trace.ps1 `
    -TargetSourceDir "path\to\source"
```

The script auto-discovers the companion `Tests.csproj` in a sibling folder.

### Restoring the Target

```powershell
.\AutoRepair\Step1_Tracing\restore_project.ps1
```

Always run this before committing or re-running instrumentation to avoid stacking modifications.

---

## Outputs

| File | Description |
| :--- | :--- |
| `trace_logs.jsonl` | Structured trace (JSON Lines). Each line is one event, see schema below. |
| `trace_logs.txt` | Raw stdout/stderr captured from the target run. |

### JSONL Schema (per line)

```jsonc
{
  "id":   "GUID",                        // unique event id
  "ts":   "yyyy-MM-dd HH:mm:ss.fff",     // timestamp
  "tid":  1,                              // thread id
  "file": "Worker.cs",                    // source file (relative)
  "line": 42,                             // original source line number
  "msg":  "Variable Update",              // event category (see below)
  "var":  { "name": "i", "val": "10" }   // optional payload
}
```

**Event categories** (`msg` values): `Enter Method`, `Variable Update`, `While Loop`, `If Branch`, `Switch Case`, `Break`, `Continue`, `Return`, `Heap State`, `Array Parameter`, `Array Argument Alias`, `Overflow`, `Cast Attempt`, `Invalid Cast`, `Exception Caught`, `Exception Type`, `Exception Stack`, and more.

> **Tip:** `trace_logs.txt` captures stdout. Unhandled exceptions print to stderr and appear in the terminal. To pinpoint a crash, match the **last JSONL entry's** `file`/`line` to the stack trace.

<!-- SCREENSHOT SUGGESTION: a few lines of trace_logs.jsonl in an editor or terminal -->

---

## Rewriter Pipeline

The Instrumenter applies rewriters **in order**. Each one is a Roslyn `CSharpSyntaxRewriter`:

| # | Rewriter | What It Injects |
| :-: | :--- | :--- |
| 1 | `LineNumberAnnotator` | Annotates every node with its original line number so subsequent rewriters report correct locations. |
| 2 | `SafetyRewriter` | Global try/catch wrappers, infinite-loop watchdogs, null-assignment warnings. |
| 3 | `ControlFlowRewriter` | Logs method entries, branch selections (`if`/`else`), loop bodies, `switch` cases, jump statements (`break`/`continue`/`return`). |
| 4 | `VariableRewriter` | Logs assignments, increments/decrements, denominators before division, array index expressions, initial local values. |
| 5 | `HeapRewriter` | Logs allocations of arrays/collections, including inferred sizes (e.g., `Heap State: buf1 (int[] size=10)`). |
| 6 | `CastingRewriter` | Logs explicit and `as` cast attempts; flags type mismatches. |
| 7 | `OverflowRewriter` | Logs arithmetic operations on boundary values that would silently overflow. |

---

## Key Files

| File / Folder | Purpose |
| :--- | :--- |
| `generate_trace.ps1` | Orchestrates Step 1: restores any previous instrumentation, builds the Instrumenter, instruments the target, runs the driver, and collects logs. |
| `restore_project.ps1` | Deletes `TraceLogger.cs` and restores every `*.bak` throughout the target directory. |
| `Instrumenter/Instrumenter.csproj` | Build project for the Roslyn-based tool. |
| `Instrumenter/Program.cs` | Entry point: generates `TraceLogger.cs`, discovers target files, runs the rewriter pipeline. |
| `Instrumenter/Rewriters/TraceRewriterBase.cs` | Shared helpers for all rewriters (build `TraceLogger` statements, wrap single statements in blocks, retrieve annotated line numbers). |
| `Instrumenter/Rewriters/*.cs` | The seven rewriters listed in the table above. |

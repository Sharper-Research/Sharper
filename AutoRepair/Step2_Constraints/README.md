# Step 2: Constraint Analysis  <-- WE ARE CURRENTLY HERE

> **Status: Done.** The rule engine is fully functional with 6 rules covering buffer bounds, off-by-one errors, null dereferences, unhandled exceptions, invalid casts, and integer overflows.

This step reads the Step 1 trace (`trace_logs.jsonl`), applies predefined safety rules, and emits a report of every violation (buffer overflows, invalid casts, integer overflows, unhandled exceptions, Off-by-one-errors and Null Dereferences) rerun the target app; it only reasons over the captured execution log.

---

## How It Works

1. **Trace ingestion**: `Program.cs` reads each JSON line, deserializes it to a `TraceEvent`, and streams it through the rule engine.
2. **State tracking**: `StateTracker` maintains a running picture of variable values and heap metadata (buffer lengths) extracted from the trace.
3. **Rule evaluation**: Each `IRule` implementation inspects relevant events plus the current state. If a constraint is violated, it produces a `ConstraintViolation`.
4. **Reporting**: All violations are serialized to `reports/violations.json` and printed to stdout.

<!-- SCREENSHOT SUGGESTION: terminal output of analyze_trace.ps1 showing violation count -->

---

## Current Rules

| Rule | Predicate | What It Detects |
| :--- | :--- | :--- |
| `BufferBoundsRule` | `index < len(buffer)` | Array/buffer index-out-of-range (uses heap metadata from Step 1). |
| `ExceptionRule` | `event.exception == null` | Any `Exception Type` event logged by the SafetyRewriter. |
| `InvalidCastingRule` | `typeof(source) is targetType` | `as`-cast nulls and explicit-cast `InvalidCastException` events. |
| `OffByOneRule` | `index < len(buffer)` (boundary) | Index equals buffer length exactly (`<=` vs `<` boundary mistake). |
| `IntegerOverflowRule` | `a op b fits in ResultType` | Arithmetic operations (`+`, `*`, `+=`) that silently overflow `Int32`/`Int64`. |

### Example Violation (from `violations.json`)

```json
{
  "predicate": "NoOverflow(+)",
  "assertion": "2147483647 + 1 fits in Int32",
  "bindings": { "operator": "+", "left": "2147483647", "right": "1", "resultType": "Int32" },
  "result": false,
  "file": "Worker.cs",
  "line": 159,
  "traceId": "f0f57032-027e-4dec-af1e-d04921761d15"
}
```

<!-- SCREENSHOT SUGGESTION: violations.json open in VS Code showing the full list -->

---

## Usage

```powershell
# From repo root
.\AutoRepair\Step2_Constraints\analyze_trace.ps1
```

The script builds `Constraints/Constraints.csproj`, reads the latest `trace_logs.jsonl`, and writes `reports/violations.json`.

> **Prerequisite:** Run Step 1 first so `trace_logs.jsonl` exists and contains data.

---

## Extending Rules

1. Create a new class under `Rules/` implementing `IRule`.
2. Register it in `Program.cs` by adding it to the rules list.
3. Run `analyze_trace.ps1` to verify it picks up the new violations.

---

## Layout

```text
AutoRepair/Step2_Constraints/
+-- README.md                       # You are here
+-- analyze_trace.ps1               # Builds & runs the analyzer
+-- reports/
|   +-- violations.json             # Latest findings (generated)
+-- Constraints/                    # Analyzer source code (C# console app)
    +-- Constraints.csproj          # Targets net10.0, uses System.CommandLine
    +-- Program.cs                  # Entry point, CLI wiring, rule orchestration
    +-- Models/
    |   +-- TraceEvent.cs           # Shape of each JSONL entry
    |   +-- ConstraintViolation.cs  # Output record (rule, message, file, line)
    +-- Runtime/
    |   +-- TraceReader.cs          # Streams JSONL -> TraceEvent instances
    |   +-- StateTracker.cs         # Maintains variable values & buffer sizes
    +-- Rules/
        +-- IRule.cs                # Rule contract
        +-- BufferBoundsRule.cs     # index < buffer.Length
        +-- ExceptionRule.cs        # Flags logged exceptions
        +-- InvalidCastingRule.cs   # Flags failed casts
        +-- OffByOneRule.cs         # Flags index == array.Length boundary errors
        +-- IntegerOverflowRule.cs  # Flags silent integer overflow
```

# Step 2 – Constraint Analysis

Goal: read the Step 1 trace (`AutoRepair/Step1_Tracing/trace_logs.jsonl`), apply predefined safety rules, and emit a report of every rule violation (buffer overflows, divide-by-zero, exceptions, etc.). This stage does **not** rerun the target app; it only reasons over the captured execution log.

## Layout

```
AutoRepair/Step2_Constraints/
├── README.md                    # You are here
├── analyze_trace.ps1            # Builds & runs the analyzer end-to-end
├── reports/
│   └── violations.json          # Latest findings (created by the analyzer)
└── Constraints/                 # Analyzer source code (C# console app)
    ├── Constraints.csproj       # Targets net10.0 + System.CommandLine
    ├── Program.cs               # Entry point, CLI wiring, rule orchestration
    ├── Models/
    │   ├── TraceEvent.cs        # Shape of each JSONL entry
    │   └── ConstraintViolation.cs# Output record (rule, message, file, line, id)
    ├── Runtime/
    │   ├── TraceReader.cs       # Streams JSONL → TraceEvent instances
    │   └── StateTracker.cs      # Maintains variable values & buffer sizes
    └── Rules/
        ├── IRule.cs             # Rule contract
        ├── BufferBoundsRule.cs  # Flags index >= buffer length events
        └── ExceptionRule.cs     # Flags any logged exception types
```

## How it works
1. **Trace ingestion** – `Program.cs` reads each JSON line, deserializes to `TraceEvent`, and streams it through the rule engine.
2. **State tracking** – `StateTracker` memoizes variable values and heap metadata (buffer lengths) pulled from Step 1 logs.
3. **Rule evaluation** – Each `IRule` implementation inspects relevant events + state. Example:
   - `BufferBoundsRule` asserts that `index:buf1` stays `< len(buf1)`.
   - `ExceptionRule` surfaces any `Exception Type` events emitted by SafetyRewriter.
4. **Reporting** – Violations are collected and serialized to `reports/violations.json` and printed to stdout.

## Usage

From repo root:

```powershell
.\AutoRepair\Step2_Constraints\analyze_trace.ps1
```

The script builds `Constraints/Constraints.csproj`, reads the latest trace, and writes `reports/violations.json` (human-readable summary TBD).

## Extending rules

1. Add a new class under `Rules/` implementing `IRule`.
2. Register it inside `Program.cs` via `ruleEngine.AddRule(new MyRule())`.
3. Update README + tests describing the new rule.

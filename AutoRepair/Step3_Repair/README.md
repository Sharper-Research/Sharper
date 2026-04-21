# Step 3 â€” Repair (CFG + Path Conditions)

The Step 3 implementation lives in [`AutoRepair/CFGAnalyzer/`](../CFGAnalyzer/) (authored by **@jpksh90**). It loads the target C# project with `MSBuildWorkspace`, builds a Roslyn `ControlFlowGraph` for each method, and traverses the call graph from a configurable entry point to print path constraints and call sites.

## How the pipeline feeds Step 3

`run_all.ps1` runs the steps in order so Step 3 consumes the cleaned-up source produced after Step 2:

1. **Step 1** instruments the target, runs the test suite, and writes `Step1_Tracing/trace_logs.jsonl`.
2. **Step 2** analyses that trace and writes `Step2_Constraints/reports/violations.json`.
3. **`restore_project.ps1`** reverts the target sources back to their pristine (uninstrumented) state.
4. **Step 3 (CFGAnalyzer)** runs against the restored project and writes `Step3_Repair/reports/cfg_analyzer.log`.

Run the whole chain with:

```powershell
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1
```

See [`AutoRepair/CFGAnalyzer/`](../CFGAnalyzer/) for CFGAnalyzer-specific docs and snapshot tests.

# Step 3: Repair (CFG + Path Conditions)

The Step 3 implementation is `AutoRepair/CFGAnalyzer/` (authored by **@jpksh90**).
It loads the target C# project with `MSBuildWorkspace`, builds a Roslyn
`ControlFlowGraph` for each method, builds an interprocedural call graph, and
prints path constraints and call sites starting from a configurable entry point.

This folder (`Step3_Repair/`) only holds the per-run output log:

```
Step3_Repair/
├── README.md                   (this file)
└── reports/
    └── cfg_analyzer.log        (generated; gitignored)
```

For analyzer documentation, samples, and the full resolver list see
[`AutoRepair/CFGAnalyzer/README.md`](../CFGAnalyzer/README.md).

## How the pipeline feeds Step 3

`run_all.ps1` runs the steps in order so Step 3 consumes the cleaned-up source
produced after Step 2:

1. **Step 1** instruments the target, runs the test suite, and writes
   `Step1_Tracing/trace_logs.jsonl`.
2. **Step 2** analyses that trace and writes
   `Step2_Constraints/reports/violations.json`.
3. **`restore_project.ps1`** reverts the target sources back to their pristine
   (uninstrumented) state.
4. **Step 3 (CFGAnalyzer)** runs against the restored project and writes
   `Step3_Repair/reports/cfg_analyzer.log`.

Run the whole chain with:

```powershell
powershell -ExecutionPolicy Bypass -File AutoRepair\run_all.ps1
```

## Status

* Task 1 (build CFG + call graph + path conditions): **done** in `CFGAnalyzer/`.
* Task 2 (consume `violations.json` and target the CFG traversal at the
  reported faulty methods): not started.
* Task 3 (synthesise a guarded patch from the path condition and re-run
  Step 1 to confirm): not started.

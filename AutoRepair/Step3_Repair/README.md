# Step 3 — Repair (CFG + Path Conditions)

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
# Step 3: Repair

> **Status: Under review.** CFG Builder (Task 1) works end-to-end and is awaiting review. Tasks 2 and 3 are next.

This step takes the violations from Step 2 and the original source code to build **forward** Control Flow Graphs, propagate constraints forward, and synthesize patches.

## Last verified run (ThrottleDebounce)

| Metric | Value |
|---|---|
| Violations consumed from Step 2 | 6 |
| Distinct methods analyzed | **6** |
| Largest CFG (`Retrier.Attempt`) | 24 nodes / 31 edges |
| CFG direction | Forward only (edges from `block.FallThroughSuccessor` and `block.ConditionalSuccessor`) |

`Predecessors` is inspected **only** to label loop headers — no reverse
edges are materialised, so the graph is a pure forward CFG as in the
professor's slide.

---

## The Three Tasks

| # | Task | Status | What It Does |
| :-: | :--- | :--- | :--- |
| 1 | **CFG Builder** | Under review | Parses source with Roslyn, builds a Control Flow Graph for each method containing a violation. Outputs nodes, edges, and violation locations. |
| 2 | **Constraint Propagation** | Not started | Walks the CFG forward from each violation to find all other code sites that could violate the same constraint. |
| 3 | **Patch Synthesis** | Not started | Generates code fixes (null checks, bounds guards, etc.) via Roslyn rewriting, applies them, and runs tests to validate. |

---

## How Task 1 Works

1. **Read violations**: Loads `violations.json` from Step 2.
2. **Locate source**: Matches each violation's relative file path to the actual source file on disk.
3. **Build CFGs**: For each method containing a violation, Roslyn parses the syntax tree and the builder walks every statement, branch, loop, try/catch, return, throw, break, and continue to produce a graph of nodes and edges.
4. **Output**: Writes `cfg_report.json` with one entry per method, including its violation lines, CFG nodes, and CFG edges.

---

## Usage

```powershell
# Run just Step 3
.\AutoRepair\Step3_Repair\build_cfg.ps1

# Or run the full pipeline (Steps 1 + 2 + 3) in one command
.\AutoRepair\run_all.ps1
```

> **Prerequisite:** Run Steps 1 and 2 first (or use `run_all.ps1` which does everything).

---

## Outputs

| File | Description |
| :--- | :--- |
| `reports/cfg_report.json` | CFG report with nodes, edges, and violation locations for each method. |

### CFG Report Schema

```jsonc
{
  "generatedAt": "2026-04-11T...",
  "sourceDirectory": "...",
  "totalViolations": 30,
  "methodsAnalyzed": 17,
  "methods": [
    {
      "methodName": "Attempt",
      "file": "Retrier.cs",
      "methodStartLine": 29,
      "methodEndLine": 57,
      "violationLines": [29],
      "nodes": [
        { "id": 0, "kind": "entry", "startLine": 29, "endLine": 29, "code": "Enter Attempt" },
        { "id": 3, "kind": "loop-header", "startLine": 36, "endLine": 36, "code": "..." },
        // ...
      ],
      "edges": [
        { "fromId": 0, "toId": 2, "label": "sequential" },
        { "fromId": 3, "toId": 6, "label": "true" },
        { "fromId": 5, "toId": 3, "label": "back-edge" },
        // ...
      ]
    }
  ]
}
```

**Node kinds:** `entry`, `exit`, `statement`, `branch`, `join`, `loop-header`, `loop-exit`, `return`, `throw`, `try`, `catch`, `finally`, `switch`, `case`, `break`, `continue`.

**Edge labels:** `sequential`, `true`, `false`, `back-edge`, `return`, `throw`, `exception`, `fall-through`, `try-body`, `finally`, `break`, `continue`, `case`.

---

## Layout

```text
AutoRepair/Step3_Repair/
+-- README.md                       # You are here
+-- build_cfg.ps1                   # Builds & runs the CFG builder
+-- reports/
|   +-- cfg_report.json             # Latest CFG output (generated)
+-- Repair/                         # Source code (C# console app)
    +-- Repair.csproj               # Targets net10.0, uses Roslyn + System.CommandLine
    +-- Program.cs                  # Entry point, CLI wiring
    +-- Models/
    |   +-- ViolationInput.cs       # Deserialization model for violations.json
    |   +-- CfgModels.cs            # CFG data model (nodes, edges, method graphs)
    +-- Analysis/
        +-- SourceLocator.cs        # Resolves violation file paths to disk paths
        +-- CfgBuilder.cs           # Roslyn-based CFG construction
```

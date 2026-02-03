# AutoRepair

Automated C# program repair system using runtime tracing and constraint solving.

## Status

| Step | Module | Status | Details |
| :--- | :--- | :--- | :--- |
| **1** | **Tracing** | In Review | Infrastructure ready. |
| **2** | **Constraints** | Todo | Analyze trace logs to identify failure conditions. |
| **3** | **Repair** | Todo | Generate patches using Static Analysis. |

## Architecture & Scalability

This project is built to **"apply easily to most open-source C# GitHub projects"**.

- **Recursive Scanning**: Instead of hardcoded files, the Instrumenter accepts a root directory and instruments all `*.cs` files found, ignoring build artifacts (`obj/`, `bin/`).
- **In-Place Instrumentation**: No manual `.csproj` edits needed. The tool backs up original files (`*.bak`) and overwrites the source code directly to guarantee compilation with **any build system** (MSBuild, dotnet CLI, etc.).
- **Universal Logger**: Automatically generates a shared `TraceLogger.cs` in the project root. Instrumented code uses `global::` references to avoid namespace conflicts.
- **Safe Restoration**: Includes a dedicated scripts to instantly revert code to its original checking-in state (`restore_project.ps1`).

## Step 1: Tracing

The tracing module modifies source code in-place to inject logging statements before every control flow point and variable assignment.

### How to Run

1. Open a terminal in the root folder.
2. Run the generation script:
   ```powershell
   .\AutoRepair\Step1_Tracing\generate_trace.ps1
   ```
   This will:
   - Compile the `Instrumenter` tool.
   - Backup `TargetApp` source files (`*.bak`).
   - Modify `TargetApp` source files in-place to inject logs.
   - Run the `TargetApp`.
   - Collect Trace Logs.

### Outputs

Located in `AutoRepair\Step1_Tracing\`:
- **trace_logs.jsonl**: JSON Lines file. The machine-readable execution trace.
- **trace_logs.txt**: The standard console output of the Target App (stdout).

#### JSONL Schema (per line)

Each line is a JSON object with these fields:

- `id`: Unique event id (GUID).
- `ts`: Timestamp string (`yyyy-MM-dd HH:mm:ss.fff`).
- `tid`: Thread id.
- `file`: Relative file name where the trace was injected.
- `line`: Source line number.
- `msg`: Event message (e.g., `Enter Method`, `Variable Update`, `While Loop`, `Heap State`).
- `var`: Optional variable payload. When present, it contains:
   - `name`: Variable name.
   - `val`: Variable value (stringified).

#### TXT Output Notes

- `trace_logs.txt` captures **stdout only**. Unhandled exceptions are printed to **stderr**, so they appear in the terminal even if the text file is empty.
- To pinpoint the crash using logs, look at the **last JSONL entry** and match its `file`/`line` to the stack trace shown in the terminal.

### How to Restore

To revert the `TargetApp` changes and clean up artifacts:

```powershell
.\AutoRepair\Step1_Tracing\restore_project.ps1
```
This restores files from `*.bak` and deletes `TraceLogger.cs`.

## Project Structure

```text
Patching/
├── AutoRepair/
│   ├── Step1_Tracing/
│   │   ├── generate_trace.ps1      # Main script to run instrumentation & tracing
│   │   ├── restore_project.ps1     # Cleanup script to revert changes
│   │   └── Instrumenter/           # C# Tool that rewrites source code
│   │       ├── Program.cs          # Entry point & directory scanner
│   │       └── Rewriters/          # Roslyn SyntaxRewriters (ControlFlow, Variables, Heap State and Exceptions/Assertions)
│   ├── Step2_Constraints/          # (Future) Constraint analysis logic
│   └── Step3_Repair/               # (Future) Patch generation logic
├── TargetApp/                      # The application to be repaired
│   ├── Program.cs                  # Target source code
│   └── TraceLogger.cs              # (Generated) Shared logging logic
└── Patching.sln
```

## File Descriptions

- **Instrumenter/Program.cs**: Scans the target folder, generates `TraceLogger.cs`, and orchestrates the rewriting process.
- **Instrumenter/Rewriters/*.cs**: Contains logic for injecting `TraceLogger.Log()` calls into specific SyntaxNodes (Blocks, Assignments, If-Statements).
- **TargetApp/**: Place any C# project here. The system automatically detects and instruments all `.cs` files.

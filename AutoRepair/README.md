# AutoRepair — Automated C# Bug Detection Pipeline

An end-to-end pipeline that **instruments**, **traces**, and **analyses** C# projects to detect runtime violations such as null dereferences, unhandled exceptions, buffer overflows, invalid casts, and integer overflows.

The pipeline can operate on local projects or automatically fetch external repositories from GitHub.

---

## Repository Under Analysis

The pipeline was validated against **SharpZipLib**, an open-source compression library for .NET:

- **Repository**: [icsharpcode/SharpZipLib](https://github.com/icsharpcode/SharpZipLib)
- **NullReferenceException issues on GitHub**: [Open issues mentioning NullReferenceException](https://github.com/icsharpcode/SharpZipLib/issues?q=is%3Aissue+NullReferenceException)

### Results (SharpZipLib)

| Metric | Value |
| :--- | :--- |
| Files instrumented | 82 (3 reverted due to semantic conflicts) |
| Tests executed | 411 (376 passed, 4 failed, 31 skipped) |
| Trace events captured | 2,336,316 |
| Violations detected | 3 |

All 3 violations are `NoUnhandledException` in `Zip\Compression\Streams\DeflaterOutputStream.cs`:

| Line | Exception Type |
| :--- | :--- |
| 487 | `System.InvalidOperationException` |
| 487 | `System.InvalidOperationException` |
| 412 | `System.NotSupportedException` |

---

## Prerequisites

- **.NET SDK 10.0+** (the pipeline targets `net10.0`)
- **Python 3.10+** (for `fetch_and_analyze.py`)
- **Git** (for cloning repositories)
- **Windows** (tested on Windows; PowerShell scripts are included for manual steps)

### Verify dotnet is accessible

```powershell
dotnet --version
```

If `dotnet` is not found, add it to your PATH:

```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
```

### PowerShell execution policy

If script execution is blocked, run:

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
```

---

## Quick Start — Automated Pipeline (`fetch_and_analyze.py`)

The automated script handles the entire flow: clone → build → instrument → trace → analyse.

### Run against a specific repository

```powershell
cd Patching
python AutoRepair\fetch_and_analyze.py --repo icsharpcode/SharpZipLib
```

### Auto-search GitHub for a C# repo with a NullReferenceException bug

```powershell
python AutoRepair\fetch_and_analyze.py
```

### With a GitHub personal access token (avoids rate limits)

```powershell
python AutoRepair\fetch_and_analyze.py --token ghp_your_token_here
# or
$env:GITHUB_TOKEN = "ghp_your_token_here"
python AutoRepair\fetch_and_analyze.py
```

### What the script does

1. **Search** — Queries the GitHub Issues API for open issues mentioning `NullReferenceException` in C# repos (or uses the `--repo` you specify).
2. **Clone** — Shallow-clones the repository into `AutoRepair/fetched_repo/`.
3. **Retarget** — Detects installed .NET runtimes and retargets the project if needed (e.g. `net6.0` → `net10.0`).
4. **Strip signing** — Removes strong-name signing config and `InternalsVisibleTo` public keys that break shallow clones.
5. **Build** — Runs `dotnet build` to verify the project compiles.
6. **Instrument** (Step 1a) — Runs the Roslyn-based Instrumenter on every `.cs` file in the source project, injecting trace logging.
7. **Fix instrumentation errors** — Post-instrumentation build check; reverts any files whose instrumentation introduced semantic errors.
8. **Trace** (Step 1b) — Runs `dotnet test` so the instrumented code produces `trace_logs.jsonl`.
9. **Analyse** (Step 2) — Feeds the trace into the constraint engine, producing `violations.json`.

---

## Manual Pipeline Steps

If you prefer to run each step individually (e.g. for a local project like `BrightHrKata` or `MicrosoftBugDemo`):

### Step 1a — Instrument the source

```powershell
cd Patching

# Instrument a target source directory
dotnet run --project AutoRepair\Step1_Tracing\Instrumenter\Instrumenter.csproj -- <path-to-source-dir>
```

Or use the PowerShell wrapper:

```powershell
.\AutoRepair\Step1_Tracing\generate_trace.ps1
```

To customise the target:

```powershell
.\AutoRepair\Step1_Tracing\generate_trace.ps1 `
    -TargetSourceDir "path\to\source" `
    -DriverProject   "path\to\tests.csproj"
```

### Step 1b — Run tests to collect traces

```powershell
$env:TRACE_LOG_JSON_PATH = "AutoRepair\Step1_Tracing\trace_logs.jsonl"
dotnet test <path-to-test-project.csproj> --verbosity minimal
```

### Step 2 — Analyse the trace

```powershell
.\AutoRepair\Step2_Constraints\analyze_trace.ps1
```

Or run directly:

```powershell
dotnet run --project AutoRepair\Step2_Constraints\Constraints\Constraints.csproj -- `
    --trace  AutoRepair\Step1_Tracing\trace_logs.jsonl `
    --report AutoRepair\Step2_Constraints\reports\violations.json
```

### Restore the source (undo instrumentation)

```powershell
.\AutoRepair\Step1_Tracing\restore_project.ps1
```

---

## Outputs

| File | Description |
| :--- | :--- |
| `Step1_Tracing/trace_logs.jsonl` | Structured execution trace (JSON Lines). Each line is one event. |
| `Step1_Tracing/trace_logs.txt` | Raw stdout/stderr from the test run. |
| `Step2_Constraints/reports/violations.json` | Detected violations with file, line, predicate, and bound values. |

### Example violation entry

```json
{
  "predicate": "NoUnhandledException(event)",
  "assertion": "event.exception == null",
  "bindings": {
    "event.exception.type": "System.InvalidOperationException"
  },
  "result": false,
  "file": "Zip\\Compression\\Streams\\DeflaterOutputStream.cs",
  "line": 487,
  "traceId": "70c9d346-54b3-4d93-97a3-5fe86f8462ea"
}
```

---

## Constraint Rules

| Rule | Predicate | Detects |
| :--- | :--- | :--- |
| `ExceptionRule` | `event.exception == null` | Unhandled exceptions (NullReferenceException, InvalidOperationException, etc.) |
| `BufferBoundsRule` | `index < len(buffer)` | Array/buffer index out of range |
| `InvalidCastingRule` | `typeof(source) is targetType` | Failed `as`-casts and `InvalidCastException` |
| `IntegerOverflowRule` | `a op b fits in ResultType` | Silent arithmetic overflow on Int32/Int64 |

---

## Project Layout

```
AutoRepair/
├── README.md                         # This file
├── fetch_and_analyze.py              # Automated end-to-end pipeline script
├── fetched_repo/                     # Cloned repository (generated, gitignored)
├── Step1_Tracing/
│   ├── README.md                     # Detailed Step 1 docs
│   ├── generate_trace.ps1            # PowerShell wrapper for instrument + trace
│   ├── restore_project.ps1           # Restores source from .bak files
│   ├── trace_logs.jsonl              # Execution trace output (generated)
│   ├── trace_logs.txt                # Test run stdout (generated)
│   └── Instrumenter/                 # Roslyn-based C# source rewriter
│       ├── Instrumenter.csproj
│       ├── Program.cs
│       └── Rewriters/                # CSharpSyntaxRewriter implementations
├── Step2_Constraints/
│   ├── README.md                     # Detailed Step 2 docs
│   ├── analyze_trace.ps1             # PowerShell wrapper for analysis
│   ├── reports/
│   │   └── violations.json           # Violation report (generated)
│   └── Constraints/                  # C# constraint analyser
│       ├── Constraints.csproj
│       ├── Program.cs
│       ├── Models/
│       └── Rules/
└── Step3_Repair/                     # (Placeholder for future auto-repair)
```

---

## Troubleshooting

| Problem | Solution |
| :--- | :--- |
| `dotnet: command not found` | Add to PATH: `$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"` |
| PowerShell scripts blocked | `Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force` |
| GitHub API rate limit (403) | Use `--token ghp_xxx` or set `$env:GITHUB_TOKEN` |
| Build fails after instrumentation | The script auto-reverts broken files. If manual, run `restore_project.ps1` and retry. |
| `net6.0` / `net8.0` runtime not found | The script auto-retargets to your installed runtime. For manual runs, edit `<TargetFramework>` in the `.csproj`. |
| Strong-name signing errors | The script strips signing automatically. For manual runs, remove `<SignAssembly>` and `<AssemblyOriginatorKeyFile>` from `.csproj` files. |

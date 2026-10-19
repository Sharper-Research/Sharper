# Sharper

Sharper is a C# research prototype for trace-guided fault analysis and repair candidates. It uses Roslyn to inspect C# projects, records runtime traces, detects selected violations, propagates predicates, checks supported safety models with Z3, and validates repair candidates against tests.

## Quick start

Requirements: Windows x64, .NET 10 SDK, PowerShell, and NuGet access. From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\run_quick_test.ps1    # one benchmark, prints QUICK TEST PASSED
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1   # every benchmark in benchmarks\
```

Both scripts run the full pipeline (trace, detect, analyze and repair) and write their outputs under `results\`. See [TACAS Artifact Evaluation](#tacas-artifact-evaluation) for details.

## TACAS Artifact Evaluation

### System requirements

| Requirement | Notes |
|---|---|
| Windows 10/11 x64 | Sharper's scripts and the Z3 native library target Windows x64. |
| .NET 10 SDK | `dotnet --list-sdks` should show a `10.0.x` entry. |
| PowerShell | Windows PowerShell 5.1 (built into Windows) or PowerShell 7. |
| NuGet access | Packages are restored on the first build. |
| Docker Desktop in **Windows containers** mode | Only for the Docker route. Windows containers need Windows 10/11 Pro, Enterprise, or Education, or Windows Server 2022. Docker Desktop on Windows Home runs Linux containers only and cannot build this image. |

### Kick the tires (about 2 minutes)

The quick test runs the whole pipeline on the `MicrosoftBugDemo` benchmark and checks that Step 3 produced candidate patches. It prints `QUICK TEST PASSED` and exits with code 0 on success. The first run takes longer while NuGet packages are restored.

**Native:**

```powershell
powershell -ExecutionPolicy Bypass -File .\run_quick_test.ps1
```

**Docker** (Windows containers mode):

```powershell
docker build -t sharper-artifact .
docker run --rm sharper-artifact
```

To keep the container's results on the host, mount the `results` folder:

```powershell
docker run --rm -v "${PWD}\results:C:\sharper\results" sharper-artifact
```

The image is based on `mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022`, installs the Visual C++ runtime required by Z3, builds `Sharper.sln`, and runs `run_quick_test.ps1` by default.

### Reproduce the evaluation

```powershell
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1 -Only MicrosoftBugDemo   # selected benchmarks only
```

The script runs the pipeline on every folder in `benchmarks\`, starting with `MicrosoftBugDemo`. For each benchmark it writes:

```
results\experiments\
├── summary.csv                       one row per benchmark: status, violations, repair items, patches
└── <BenchmarkName>\
    ├── pipeline.log                  full console output of the run
    ├── step1_traces\                 trace_logs.jsonl (recorded execution events)
    ├── step2_constraints\            violations.json (detected violations and their predicates)
    └── step3_repairs\                cfg_analyzer.log, repair_plan.json (model checks, candidate status)
        └── patches\                  generated *.diff and *.patched.cs files
```

The script exits with code 1 if any benchmark fails. A failing benchmark does not stop the others.

### Benchmark mapping

Benchmarks live under `benchmarks/`, one folder per benchmark. For each folder, the scripts use the test project that references `Microsoft.NET.Test.Sdk` as the trace driver, and the first project it references as the library to analyze and repair.

| Benchmark | Library under repair | Test driver | Fault exercised |
|---|---|---|---|
| `benchmarks/MicrosoftBugDemo` | `RuntimeCode/RuntimeCode.csproj` | `RuntimeCode.Tests/RuntimeCode.Tests.csproj` | Null dereference in `MemberMethodResolver.CheckShouldPersist` when the `ShouldSerialize*` method is declared on a base class |

Generated patches and logs land under `results/<run-id>/step3_repairs/patches`, where `<run-id>` is:

| Command | `<run-id>` |
|---|---|
| `run_quick_test.ps1` | `quick-test` |
| `run_experiments.ps1` | `experiments/<BenchmarkName>` |
| `Sharper/run_all.ps1` called directly | `run-<yyyyMMdd-HHmmss>-<source folder>` |

Each folder has the same `step1_traces`, `step2_constraints`, and `step3_repairs` layout. Fixed run IDs are cleared before each run. The contents of `results/` are git-ignored.

The instrumenter temporarily rewrites benchmark source files. Both scripts restore the original sources (`*.cs.bak` back to `*.cs`, generated `TraceLogger.cs` removed) before and after every run, including runs that fail or are interrupted.

## How it works

1. **Trace:** the Roslyn instrumenter records selected execution events from a failing test.
2. **Detect:** the constraint analyzer reports supported bounds, null, cast, overflow, and exception violations.
3. **Analyze and repair:** the Roslyn analyzer builds control-flow and call graphs, propagates predicates, explores supported models with Z3, and emits candidate patches.
4. **Validate:** the candidate gate runs the reproducer and regression suite against baseline and candidate copies. Test acceptance is not a proof of semantic correctness.

To run the pipeline on a project of your own, call `Sharper\run_all.ps1` directly:

```powershell
.\Sharper\run_all.ps1 `
  -SourceDir     .\benchmarks\MicrosoftBugDemo\RuntimeCode `
  -DriverProject .\benchmarks\MicrosoftBugDemo\RuntimeCode.Tests\RuntimeCode.Tests.csproj `
  -Solution      .\benchmarks\MicrosoftBugDemo\RuntimeCode\RuntimeCode.csproj
```

Optional parameters: `-OutputDir <folder>` to choose the results folder, and `-ValidationRoot <folder> -ReproducerFilter <test filter>` together to validate candidates against tests.

## Repository layout

| Path | Contents |
|---|---|
| `Sharper/` | Pipeline implementation: `Step1_Tracing` (instrumenter), `Step2_Constraints` (violation detection), `Step3_Repair/CFGAnalyzer` (Roslyn analysis, Z3 checks, patch generation and validation), `run_all.ps1` |
| `benchmarks/` | Programs under evaluation |
| `results/` | Run outputs (git-ignored) |
| `review/` | Fixtures, models, and saved evidence from an earlier recorded artifact run |
| `run_quick_test.ps1`, `run_experiments.ps1` | Entry points for artifact evaluation |
| `Dockerfile` | Windows container image for the quick test |

## Scope

The verifier supports a restricted C# subset and reports unsupported effects, unresolved queries, and exploration cutoffs as incomplete. Z3 checks the supported symbolic obligations; it does not verify arbitrary C# programs. Passing tests for a candidate patch is not a proof of semantic correctness.

## License

Sharper is released under the MIT License. See [LICENSE](LICENSE) for the full text.

Benchmark programs under `benchmarks/` keep their original licenses. For example, `MicrosoftBugDemo` contains code derived from .NET Foundation sources, which are licensed under the MIT License.

## Project authors and contributors

- Jyoti Prakash
- Manish Raj Moriche
- Sahal Abdullah
- Dimitrios Dafnis
- Adam Alami
- Abhishek Tiwari

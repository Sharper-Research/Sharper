# Sharper

Sharper is a C# research prototype for trace-guided fault analysis and repair candidates. It uses Roslyn to inspect C# projects, records runtime traces, detects selected violations, propagates predicates, checks supported safety models with Z3, and validates repair candidates against tests.

## Quick start

Requirements for the native route: Windows x64, .NET 10 SDK, PowerShell, and NuGet access (or use [Docker](#kick-the-tires-about-2-minutes) on any OS). `setup.ps1` installs the .NET SDK and Visual C++ runtime if they are missing. From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup.ps1             # install what is missing, build, then call run_quick_test.ps1
powershell -ExecutionPolicy Bypass -File .\run_quick_test.ps1    # one benchmark, prints QUICK TEST PASSED
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1   # every benchmark in benchmarks\
```

Both scripts run the full pipeline (trace, detect, analyze and repair) and write their outputs under `results\`. See [TACAS Artifact Evaluation](#tacas-artifact-evaluation) for details.

## TACAS Artifact Evaluation

### System requirements

| Requirement | Notes |
|---|---|
| Windows 10/11 x64 | Sharper's scripts and the Z3 native library target Windows x64. |
| .NET 10 SDK | `dotnet --list-sdks` should show a `10.0.x` entry. `setup.ps1` installs it per user if missing. |
| PowerShell | Windows PowerShell 5.1 (built into Windows) or PowerShell 7. |
| Visual C++ 2015-2022 Redistributable, x64 | Required by Z3's native DLL. `setup.ps1` and `Dockerfile.windows` install it if missing; [Microsoft installer](https://aka.ms/vs/17/release/vc_redist.x64.exe). |
| NuGet access | Packages are restored on the first build. |
| Docker (Linux containers) | Only for the Docker route; replaces all requirements above. Works with Docker on Linux, macOS, and Windows (Docker Desktop's default Linux-containers mode). |
| Docker in **Windows containers** mode | Only for the optional `Dockerfile.windows` route. Needs Windows 10/11 Pro, Enterprise, or Education (with the Hyper-V and Containers features enabled) or Windows Server 2022. |

### Artifact information

- **Artifact source:** <https://github.com/Sharper-Research/Sharper>. Permanent archive (Zenodo DOI): **TODO-DOI**.
- **Paper:** *Sharper: Towards an Automated Verified Repair Tool for C#* (TACAS 2027 tool paper). `run_experiments.ps1` reproduces the JsonConverter results of Section 3 (see [Paper claims](#paper-claims-section-3-jsonconverter)). Listing 1.1 is included as a reference, with a runnable array adaptation; the original span semantics are not verified.
- **Intended badge applications:** Available, Functional, and Reusable (see [Run Sharper on your own repository](#run-sharper-on-your-own-repository)).
- **Hardware:** x64 (amd64) CPU, 8 GB RAM, about 5 GB free disk space for the Docker image (about 15 GB for the Windows container image). No GPU or special hardware is needed. Internet access is needed to restore pinned NuGet packages (and, for Docker, the base image and Z3).
- **Time:** quick test about 2 minutes; all benchmarks about 5 minutes after the first build; first Docker build about 5 minutes (Linux) or 10 minutes (Windows containers).

Tested platforms:

| Route | Platform | Tool versions |
|---|---|---|
| Native | Windows 11 x64 | .NET SDK 10.0.401, Windows PowerShell 5.1 |
| Docker (`Dockerfile`) | Linux container, `linux/amd64` (Ubuntu 24.04) on Docker Desktop, Windows 11 x64 host | .NET SDK 10.0, PowerShell 7 |
| Docker (`Dockerfile.windows`, optional) | Windows Server 2022 container on Windows 11 x64 | .NET SDK 10.0, Windows PowerShell 5.1; not re-run for the current revision |

The current native and Linux-container checks are described below; do not infer validation of the corrected Windows-container route from earlier runs. On arm64 hosts (for example Apple silicon) Docker runs the amd64 image under emulation, which is slower; this was not tested.

### Kick the tires (about 2 minutes)

The quick test runs the whole pipeline on the `MicrosoftBugDemo` benchmark and checks that Step 3 produced candidate patches. It prints `QUICK TEST PASSED` and exits with code 0 on success. The first run takes longer while NuGet packages are restored.

**Native (Windows), one command:**

```powershell
powershell -ExecutionPolicy Bypass -File .\setup.ps1
```

`setup.ps1` prepares a Windows x64 machine and then calls `run_quick_test.ps1`:

1. Installs the .NET 10 SDK into `%LOCALAPPDATA%\Microsoft\dotnet` if no 10.0 SDK is found (per user, no administrator rights) and adds it to the user `PATH`.
2. Installs the Visual C++ 2015-2022 x64 runtime if it is missing. This is the only step that asks for administrator permission.
3. Restores NuGet packages and builds `Sharper.sln`.
4. Runs `run_quick_test.ps1` and exits with its code.

Steps that are already satisfied are skipped, so it is safe to run again. Options: `-NoPersistPath` (do not change the user `PATH`), `-DotnetInstallDir <folder>`. If the tools are already installed, the quick test can also be run on its own:

```powershell
powershell -ExecutionPolicy Bypass -File .\run_quick_test.ps1
```

**Docker** (Linux containers; any OS):

```sh
docker build -t sharper-artifact .
docker run --rm sharper-artifact
```

To keep the container's results on the host, mount the `results` folder
(PowerShell: use `"${PWD}/results:/sharper/results"`):

```sh
docker run --rm -v "$PWD/results:/sharper/results" sharper-artifact
```

The image is based on `mcr.microsoft.com/dotnet/sdk:10.0` (Ubuntu, includes `pwsh`) for `linux/amd64`, pinned by its manifest SHA-256 in `Dockerfile`.
Because the Microsoft.Z3 NuGet package has no Linux native library, the image downloads the
official Z3 4.12.2 Linux build (SHA-256 checked) and its OpenMP runtime. It then builds
`Sharper.sln` and runs `run_quick_test.ps1` by default. On Apple silicon, Docker runs the
image under amd64 emulation, which is slower.
The solution includes all five benchmark libraries and test drivers, so their
packages are restored during the image build.

**Docker** (Windows containers mode, optional):

```powershell
docker build -f Dockerfile.windows -t sharper-artifact-win .
docker run --rm -v "${PWD}\results:C:\sharper\results" sharper-artifact-win
```

`Dockerfile.windows` is based on `mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2022` and installs the Visual C++ runtime required by Z3.

### Reproduce the evaluation

```powershell
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1 -Only JsonConverter      # one benchmark only
```

To run all five benchmarks in Docker and retain the results:

```sh
mkdir -p results
docker run --rm -v "$PWD/results:/sharper/results" sharper-artifact ./run_experiments.ps1
```

The script runs the pipeline on every folder in `benchmarks\`, starting with `MicrosoftBugDemo`. For each benchmark it writes:

```
results\experiments\
├── summary.csv                       one row per benchmark: status, violations, repair items, patches
├── environment.json                  OS, architecture, SDK and PowerShell versions
└── <BenchmarkName>\
    ├── original-source-sha256.json    source hashes checked after restoring instrumentation
    ├── pipeline.log                  full console output of the run
    ├── baseline\                     baseline.trx (original, uninstrumented tests)
    ├── step1_traces\                 trace_logs.jsonl and instrumented.trx
    ├── step2_constraints\            violations.json (detected violations and their predicates)
    └── step3_repairs\                cfg_analyzer.log, repair_plan.json (model checks, candidate status)
        └── patches\                  generated *.diff and *.patched.cs files
```

The script exits with code 1 if any benchmark fails. A failing benchmark does not stop the others.

Expected `summary.csv` (identical on Windows and in the Linux Docker image; the full run takes a few minutes):

| Benchmark | Status | Baseline passed / failed | Violations | Repair items | Patches |
|---|---|---|---:|---:|---:|
| MicrosoftBugDemo | Passed | 2 / 1 | 1 | 1 | 1 |
| HtmlSanitizer | Passed | 198 / 0 | 2 | 2 | 2 |
| JsonConverter | Passed | 174 / 0 (6 skipped) | 3 | 3 | 2 |
| PaperArrayBounds | Passed | 1 / 3 | 4 | 4 | 3 |
| ThrottleDebounce | Passed | 110 / 0 | 10 | 10 | 0 |

It rejects unknown benchmark names, missing/empty traces, missing reports, and
unexpected baseline test failures. `Passed` means that the benchmark's artifact
checks completed, not that the faulty baseline passed its tests or that every
candidate is correct. The summary records baseline passing/failing test counts.
The array example additionally requires a model-checked, test-accepted candidate.
JsonConverter additionally checks every Section 3 claim of the paper (see
[Paper claims](#paper-claims-section-3-jsonconverter)).
ThrottleDebounce is a negative control: its expected outcome is intentional
exception classifications and zero patches, not a fabricated repair.
All outputs are written directly beneath the selected results directory, including
partial evidence from a failed pipeline; no intermediate reports are written into
the tool's source directories.

### Paper claims (Section 3, JsonConverter)

`benchmarks/JsonConverter` is StefH/JsonConverter at the commit evaluated in the paper. `run_experiments.ps1` checks each claim of Section 3 and writes the evidence to `results/experiments/JsonConverter/paper-claims.json`; the benchmark fails if any claim does not hold.

| Claim in the paper | Evidence in `results/experiments/JsonConverter/` |
|---|---|
| The recorded run reports that `ArrayAndListDeserializationTests` throws an `IndexOutOfRangeException` | `step1_traces/instrumented.trx` |
| The exception is raised by the array write `list[i++]` in the `type.IsArray` branch of `PocoJsonSerializerStrategy.DeserializeObject` | `step2_constraints/violations.json` (line 235) |
| The trace shows the destination array `[null, English]` of length 2 and write index 3, violating the array-bounds predicate | `step2_constraints/violations.json` (`BufferInBounds`, `index:list` = 3, `heap.size(list)` = 2) |
| Sharper synthesises a guard that wraps the method body in a `try/catch` for `IndexOutOfRangeException`, logs the exception, and returns `default` | `step3_repairs/patches/PocoJsonSerializerStrategy.L102.diff` |
| With this patch the full test suite passes (174 passed, 6 skipped) | `patched-suite/patched.trx` (patch applied to an isolated copy of the benchmark) |
| The patch was submitted upstream | [StefH/JsonConverter#27](https://github.com/StefH/JsonConverter/pull/27) (pull request from commit `e0a064c` of jim-daf/JsonConverter) |

**Tracing modes.** The paper's results were produced with the variable tracer as it was at submission. The runner uses this tracer for JsonConverter by setting `SHARPER_TRACING=paper` (implemented in `Sharper/Step1_Tracing/Instrumenter/Rewriters/PaperVariableRewriter.cs`). When it logs the index of `list[i++]`, this tracer evaluates `i++` again, so the exception occurs in the recorded (instrumented) run; the uninstrumented suite already passes (the `174 / 0` baseline above). The other benchmarks, and Sharper by default, use the current tracer (`VariableRewriter.cs`), which evaluates each index once. With the current tracer, JsonConverter reports no violation, which is why the runner selects the paper tracer for this benchmark.

### Benchmark mapping

Benchmarks live under `benchmarks/`, one folder per benchmark. For each folder, the scripts use the test project that references `Microsoft.NET.Test.Sdk` as the trace driver, and the first project it references as the library to analyze and repair.

| Benchmark | Library under repair | Test driver | Fault exercised |
|---|---|---|---|
| `benchmarks/MicrosoftBugDemo` | `RuntimeCode/RuntimeCode.csproj` | `RuntimeCode.Tests/RuntimeCode.Tests.csproj` | Null dereference in `MemberMethodResolver.CheckShouldPersist` when the `ShouldSerialize*` method is declared on a base class |
| `benchmarks/HtmlSanitizer` | `src/HtmlSanitizer/HtmlSanitizer.csproj` | `test/HtmlSanitizer.Tests/HtmlSanitizer.Tests.csproj` | Historical sanitizer exception-trace case; upstream tests include intentionally thrown exceptions |
| `benchmarks/JsonConverter` | `src/JsonConverter.SimpleJson/JsonConverter.SimpleJson.csproj` | `tests/SimpleJson.Tests/SimpleJson.Tests.csproj` | Paper Section 3: `IndexOutOfRangeException` at the array write `list[i++]` in `PocoJsonSerializerStrategy.DeserializeObject` (recorded with `SHARPER_TRACING=paper`) |
| `benchmarks/ThrottleDebounce` | `ThrottleDebounce/ThrottleDebounce.csproj` | `Tests/Tests.csproj` | Negative control: intentional argument checks, test-fixture exceptions, and documented cancellation must not be patched |
| `benchmarks/PaperArrayBounds` | `Listing.csproj` | `Listing.Tests/Listing.Tests.csproj` | Runnable `int[]` adaptation of paper Listing 1.1; out-of-bounds writes in both loops |

See [benchmark provenance and expected behavior](benchmarks/README.md) for exact
historical commits, local project adaptations, licensing, and interpretation.
The sanitizer and throttle/debounce cases are not claims of verified upstream
bugs or repairs. Their original tests must pass before instrumentation.
HtmlSanitizer runs all 198 tests in the clean baseline; its instrumented run
excludes only the 16,000-invocation `ThreadTest` stress harness (197 tests traced).
Tracing still takes longer than ordinary test execution and can produce roughly
100 MB for this benchmark; allow several minutes for the full evaluation.

Generated patches and logs land under `results/<run-id>/step3_repairs/patches`, where `<run-id>` is:

| Command | `<run-id>` |
|---|---|
| `run_quick_test.ps1` | `quick-test` |
| `run_experiments.ps1` | `experiments/<BenchmarkName>` |
| `Sharper/run_all.ps1` called directly | `run-<yyyyMMdd-HHmmss>-<source folder>` |

Each folder has the same `step1_traces`, `step2_constraints`, and `step3_repairs` layout. Fixed run IDs are cleared before each run. The contents of `results/` are git-ignored.
Copy the desired results into the submission archive explicitly if saved evidence
is required; a source checkout alone does not include ignored results.

The instrumenter temporarily rewrites benchmark source files. Both scripts restore the original sources (`*.cs.bak` back to `*.cs`, generated `TraceLogger.cs` removed) before and after every run, including runs that fail or are interrupted.

### Run Sharper on your own repository

Sharper can analyze supported C# libraries with a test project; arbitrary C# semantics are not supported (see [Scope](#scope)). The Docker image provides the environment (.NET 10 SDK, PowerShell 7, Z3), so only Docker and git are needed on the host.

**1. Get Sharper and build the image** (once):

```sh
git clone https://github.com/Sharper-Research/Sharper.git
cd Sharper
docker build -t sharper-artifact .
```

**2. Clone the repository to analyze** into a separate folder. Sharper temporarily rewrites its source files, so use a fresh clone, not a working copy with uncommitted changes. This example uses a small Roman-numeral converter:

```sh
mkdir -p ../sharper-targets
git clone https://github.com/deinacademy/tdd_roman_csharp.git ../sharper-targets/tdd_roman_csharp
```

**3. Identify three paths** in the cloned repository:

| Parameter | What to pass | Example |
|---|---|---|
| `-SourceDir` | Folder of the library under analysis | `Number` |
| `-DriverProject` | Test project (`.csproj`) whose tests drive the trace | `Number.Tests/Number.Tests.csproj` |
| `-Solution` | The library's `.csproj` | `Number/Number.csproj` |

**4. Run the pipeline** from the Sharper folder. The target folder is mounted at `/work` and the results folder at `/sharper/results`:

```sh
docker run --rm \
  -e DOTNET_ROLL_FORWARD=Major -e LangVersion=latest \
  -v "$PWD/../sharper-targets:/work" \
  -v "$PWD/results:/sharper/results" \
  sharper-artifact ./Sharper/run_all.ps1 \
  -SourceDir     /work/tdd_roman_csharp/Number \
  -DriverProject /work/tdd_roman_csharp/Number.Tests/Number.Tests.csproj \
  -Solution      /work/tdd_roman_csharp/Number/Number.csproj \
  -OutputDir     results/tdd_roman_csharp
```

In Windows PowerShell, end lines with a backtick (`` ` ``) instead of `\` and write the mounts as `"${PWD}/../sharper-targets:/work"` and `"${PWD}/results:/sharper/results"`.

The two environment variables let older projects run in the image without editing them:

- `DOTNET_ROLL_FORWARD=Major` runs test projects that target an older .NET (for example `netcoreapp2.0`) on the image's .NET 10 runtime.
- `LangVersion=latest` lets the generated trace logger compile in projects that default to C# 7.3 (`netstandard2.0` and .NET Framework). Without it, Step 1 fails with `error CS8370: Feature 'nullable reference types' is not available in C# 7.3`.

**5. Read the results** in `results/tdd_roman_csharp/`. The layout is the same as for the benchmarks: `step1_traces/`, `step2_constraints/violations.json`, and `step3_repairs/` (with `patches/` if candidates were generated). Use a new `-OutputDir` for each run; Sharper refuses to overwrite earlier step outputs.

For this example, the expected outcome is 17 passing tests, about 1,500 trace events, and one violation: the `ArgumentException` that `ConvertToRoman` deliberately throws for inputs it cannot convert. Sharper classifies it as `IntentionalValidator` and generates no patch, because the exception is intended input validation that a test expects.

The cloned repository is restored to its original sources after the run (`git status` in the clone shows no changes).

**Requirements for the target repository:**

- A test project that references `Microsoft.NET.Test.Sdk` (xUnit, MSTest, or NUnit) and builds with the .NET 10 SDK.
- Tests that run on Linux, because the image is Linux-based. Projects that only build for .NET Framework or Windows desktop (WPF, WinForms) need the native Windows route instead.
- The library's tests should pass, or fail only in the tests that exercise the fault under study.

**Native alternative (Windows):** run the same command without Docker, using local paths:

```powershell
powershell -ExecutionPolicy Bypass -File .\Sharper\run_all.ps1 `
  -SourceDir     ..\sharper-targets\tdd_roman_csharp\Number `
  -DriverProject ..\sharper-targets\tdd_roman_csharp\Number.Tests\Number.Tests.csproj `
  -Solution      ..\sharper-targets\tdd_roman_csharp\Number\Number.csproj `
  -OutputDir     results\tdd_roman_csharp
```

This needs the native requirements listed above. Set `$env:LangVersion = "latest"` first for C# 7.3 projects, and `$env:DOTNET_ROLL_FORWARD = "Major"` for test projects that target an older .NET.

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
`-TestFilter <VSTest filter>` limits only the instrumented trace-driving tests.

## Repository layout

| Path | Contents |
|---|---|
| `Sharper/` | Pipeline implementation: `Step1_Tracing` (instrumenter), `Step2_Constraints` (violation detection), `Step3_Repair/CFGAnalyzer` (Roslyn analysis, Z3 checks, patch generation and validation), `run_all.ps1` |
| `benchmarks/` | Programs under evaluation |
| `results/` | Run outputs (git-ignored) |
| `setup.ps1` | One-command Windows setup: installs missing tools, builds, then calls `run_quick_test.ps1` |
| `run_quick_test.ps1`, `run_experiments.ps1` | Entry points for artifact evaluation |
| `Dockerfile` | Linux container image (default Docker route) |
| `Dockerfile.windows` | Windows container image (optional Docker route) |

The original paper Listing 1.1 is included as
[Listing.paper.cs.txt](benchmarks/PaperArrayBounds/Listing.paper.cs.txt), a reference
file excluded from compilation and instrumentation. It uses `Span<int>` with
`stackalloc`; the runnable [Listing.cs](benchmarks/PaperArrayBounds/Listing.cs)
uses `int[]` instead. The source model supports fixed-size `Int32` arrays, not
span/stack-allocation semantics. Reported test and model-check results apply only
to the array adaptation. See the [benchmark notes](benchmarks/README.md#paper-example)
for the exact differences.

### Submission checks

```powershell
dotnet build .\Sharper.sln
dotnet test .\Sharper\Step3_Repair\CFGAnalyzer\CFGAnalyzer.Tests\CFGAnalyzer.Tests.csproj `
  --logger "trx;LogFileName=analyzer-suite.trx" --results-directory .\results\tool-tests
powershell -ExecutionPolicy Bypass -File .\run_quick_test.ps1
powershell -ExecutionPolicy Bypass -File .\run_experiments.ps1
```

Do not use `dotnet test Sharper.sln` as a success check: the Microsoft and array
benchmarks intentionally contain failing baseline regression tests. Inspect
`results/experiments/summary.csv`, the TRX reports, and candidate model/validation
statuses. Keep build and run evidence with the submission. Restoring historical benchmarks can report NuGet advisories;
their pinned dependencies are retained for research reproducibility.

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

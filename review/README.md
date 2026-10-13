# Sharper Artifact

This directory contains runnable examples and saved outputs for evaluating the tool. It contains no manuscript or paper-review files.

## Requirements

- Windows x64
- .NET 10 SDK
- PowerShell 5.1 or later
- NuGet access

## Reproduce

From the repository root, choose a new output path each run:

```powershell
.\review\reproduce.ps1 -Output review\rerun
```

The script builds the solution and runs the analyzer tests, checks the model and trace-activation examples, instruments a disposable copy of the runtime fixture, and exercises one accepted synthetic repair, one rejected runtime candidate, and the two-loop array example. The deliberately faulty baseline tests are expected to fail; the script checks their expected outcomes and verifies that source fixtures remain unchanged.

The optional JsonConverter reproduction uses pinned upstream commits and requires GitHub and NuGet access:

```powershell
.\review\verify-jsonconverter.ps1
```

It separately records the clean baseline test result, the instrumented-baseline failure, Sharper's detected violations and incomplete candidate-model checks, and the pull-request candidate's test result. The pinned clean baseline already passes the selected test; the observed exception occurs after instrumentation. Passing tests for the open upstream pull request do not prove semantic correctness or establish a defect in clean upstream code. This case is therefore a transparent reproduction of the observed pipeline behavior, not a verified repair.

To package the source, fixtures, models, and saved run evidence without manuscript material:

```powershell
.\review\package.ps1 -Evidence review\verified-run
```

The output ZIP is created under `review\Sharper-tool-artifact.zip`; existing output is never overwritten.

## What the examples demonstrate

- `fixtures/bounds`: a direct-return bounds guard accepted against an explicit test contract.
- `fixtures/runtime`: an extracted null-dereference regression whose catch/default candidate is rejected because a regression test still fails.
- `fixtures/listing`: supported fixed-size `Int32[]` loop accesses and iterative guard refinement.
- `models`: explicit procedure/loop checks and fixed-predicate trace activation.
- `verified-run`: recorded CLI outputs, test results, candidate diffs, and hashes.
- `jsonconverter-run`: optional pinned upstream reproduction outputs, created by `verify-jsonconverter.ps1`.

The project uses Roslyn to load C# and build control-flow models, then uses the Z3 SMT solver for supported symbolic safety obligations. This is an integrated verifier for a restricted subset, not whole-C# verification. Unsupported semantics and bounded-out exploration return `Incomplete`; test acceptance is not a correctness proof.

## Main implementation

- Trace instrumentation: `Sharper/Step1_Tracing`
- Violation detection: `Sharper/Step2_Constraints`
- Roslyn analysis, Z3 checks, and patch validation: `Sharper/Step3_Repair/CFGAnalyzer`
- Reproduction and packaging scripts: `review/reproduce.ps1`, `review/package.ps1`
- Pinned upstream case: `review/verify-jsonconverter.ps1`

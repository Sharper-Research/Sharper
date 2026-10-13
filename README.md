# Sharper

Sharper is a C# research prototype for trace-guided fault analysis and repair candidates. It uses Roslyn to inspect C# projects, records runtime traces, detects selected violations, propagates predicates, checks supported safety models with Z3, and validates repair candidates against tests.

## Quick start

Requirements: Windows x64, .NET 10 SDK, PowerShell 5.1 or later, and NuGet access.

```powershell
dotnet build .\Sharper.sln
dotnet test .\Sharper\Step3_Repair\CFGAnalyzer\CFGAnalyzer.Tests\CFGAnalyzer.Tests.csproj
dotnet run --project .\Sharper\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj -- --help
```

For the reproducible end-to-end demonstration, start with [the artifact guide](review/README.md). It includes a trace fixture, model-checking examples, accepted and rejected repair candidates, and a script that checks the recorded outcomes.

## How it works

1. **Trace:** the Roslyn instrumenter records selected execution events from a failing test.
2. **Detect:** the constraint analyzer reports supported bounds, null, cast, overflow, and exception violations.
3. **Analyze and repair:** the Roslyn analyzer builds control-flow and call graphs, propagates predicates, explores supported models with Z3, and emits candidate patches.
4. **Validate:** the candidate gate runs the reproducer and regression suite against baseline and candidate copies. Test acceptance is not a proof of semantic correctness.

## Scope

The verifier supports a restricted C# subset and reports unsupported effects, unresolved queries, and exploration cutoffs as incomplete. Z3 checks the supported symbolic obligations; it does not verify arbitrary C# programs. The trace instrumenter temporarily rewrites source files, so use a disposable checkout for manual tracing. See the artifact guide for reproducible commands and limitations.

## Project authors and contributors

- Jyoti Prakash
- Manish Raj Moriche
- Sahal Abdullah
- Dimitrios Dafnis
- Adam Alami
- Abhishek Tiwari

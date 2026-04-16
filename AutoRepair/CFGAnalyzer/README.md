# CFGAnalyzer

CFGAnalyzer is a CLI analyzer that builds Roslyn control-flow graphs (CFGs), constructs an interprocedural call graph, and reports path-oriented analysis data.

## Quick Start

```zsh
dotnet build CFGAnalyzer.csproj
dotnet run --project CFGAnalyzer.csproj -- /abs/path/to/Target.csproj
```

Exclude external `System.*` call targets except `System.Console.*`:

```zsh
dotnet run --project CFGAnalyzer.csproj -- --exclude-system-library-targets /abs/path/to/Target.csproj
```

## Samples (Brief)

- `samples/RtaNarrowingDemo/`: validates `RapidTypeAnalysisCallResolver` narrowing.
  - Expected include: `WorkerA.Run()`, `WorkerA.Tick()`
  - Expected exclude: `WorkerB.Run()`, `WorkerB.Tick()`
- `samples/LambdaLocalFunctionDemo/`: validates lambda and local-function resolution.
  - Expected include: `LocalWork()`, `lambda expression`
  - Expected exclude: `NeverCalled`

Use the cross-platform verifier scripts:

```zsh
./scripts/verify-rta-demo.sh
```

```bat
scripts\verify-rta-demo.bat
```

Snapshot tests for JSON callgraph output:

```zsh
./scripts/verify-snapshots.sh
```

```bat
scripts\verify-snapshots.bat
```

## Project Design

### Pipeline

1. Open target `.csproj` or `.sln` with Roslyn/MSBuild.
2. Discover method-like members (methods, constructors, accessors, local functions, lambdas).
3. Build per-symbol CFGs from Roslyn operations.
4. Extract call sites from CFG operations and resolve targets through resolver priority order.
5. Build interprocedural call graph edges from resolved call sites.
6. Compute path/dominator data and attach it to method analysis results.
7. Traverse/report from selected entry nodes.

### Call Resolvers

Call resolvers implement `ICallResolver` and are tried in descending priority order. The first resolver that returns a non-empty result wins. Resolvers can be disabled via `CallGraphConfig.DisabledResolvers` (using the `CallResolver` enum) or the `--disable-resolver` CLI flag.

| Enum value | Class | Priority | What it does |
|---|---|---|---|
| `Static` | `StaticCallResolver` | 0 (lowest) | Uses Roslyn static semantics to resolve direct invocations, object creations, property accessors, local functions, anonymous functions, and delegate creations. This is the baseline resolver and handles the majority of calls. |
| `InterfaceImplementation` | `InterfaceImplementationCallResolver` | 100 | When the call target is declared on an interface, uses `SymbolFinder.FindImplementationsAsync` to locate all concrete implementations in the project. More precise than RTA for interface calls but requires a full solution search. |
| `RapidTypeAnalysis` | `RapidTypeAnalysisCallResolver` | 200 (highest) | Narrows virtual and interface dispatch to only the types actually instantiated in the project (RTA set). Avoids including dead subtypes in the call graph edges. See `samples/RtaNarrowingDemo/` for a worked example. |

To disable a resolver at the CLI:

```zsh
dotnet run --project CFGAnalyzer.csproj -- --disable-resolver RapidTypeAnalysis /abs/path/to/Target.csproj
```

To disable programmatically:

```csharp
var config = new CallGraphConfig
{
    DisabledResolvers = new HashSet<CallResolver> { CallResolver.InterfaceImplementation }
};
```

### Design Decision 

- Resolver logic is isolated behind `ICallResolver`, so precision can improve without rewriting graph construction.
- Symbol identity uses Roslyn symbol equality comparers to avoid fragile string matching.
- Analysis stages are separated (CFG, callgraph, dataflow, traversal), making each stage easier to modify and test.

### Future Work

- Implement Interprocedural pointer analysis to further narrow call targets.
- Implement CFG construction algorithms for delegates, events, dynamic calls, extension methods, operator overloading, partial methods, function pointers, and linq.
- Add a test project with unit tests for core components (e.g., CFG construction, call resolution, dominator computation).
- Expand sample projects to cover more C# features and edge cases (e.g., generics, async/await, dynamic calls).

For repository-specific AI/developer guidance, see `AGENTS.md`.


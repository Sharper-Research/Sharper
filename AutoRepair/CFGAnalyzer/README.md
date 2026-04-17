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
- `samples/GenericsDemo/`: validates generic-method and generic-type callgraph support.
  - Expected include: `PrintValue<T>`, `Consume<T>`, `Box<T>.Update(T)`, `Box<T>.Log(T)`
  - Current behavior:
    - Generic method/type calls are resolved and linked to internal generic method/type definitions.
    - Generic call edges include caller/callee line numbers in snapshot output.
  - Limitations:
    - Constructed generic targets are normalized to generic definitions (`<T>`) instead of being preserved as separate concrete targets (`<int>`, `<string>`).
    - The call graph does not currently keep per-instantiation specialization in node identity.
    - Generic-heavy scenarios not covered yet include complex constraints, nested generic inference chains, and overload resolution corner cases.
- `samples/ExtensionMethodDemo/`: validates extension-method resolution.
  - Expected include: `TextExtensions.WrapInBrackets(string)`, `TextExtensions.Duplicate(string)` as internal edges from `Program.Main()`.
- `samples/OperatorOverloadDemo/`: validates user-defined operator and conversion resolution.
  - Expected include from `Program.Main()`: `op_Addition`, `op_UnaryNegation`, `op_Equality`, `op_Inequality`, `op_Implicit`, `op_Explicit`.
  - Expected exclude: built-in numeric conversion edges (for example `int -> double`).
- `samples/FunctionPointerDemo/`: demonstrates unsafe function pointer syntax support.
  - Contains: `Operations.Add`, `Operations.Multiply`, `Operations.Square` methods and `Program.Main()` with pointer assignments.
  - Current behavior: CFG and symbol collection work correctly for function pointer code.
  - Limitations: Roslyn's operation tree does not model unsafe pointer dereference invocations as regular call operations, so call site resolution for `ptr()` syntax is not yet possible. The infrastructure is in place for potential future resolution if Roslyn changes its modeling.

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

### Reachability API

`CallGraphTraverser` exposes a reachability helper for graph queries:

```csharp
IReadOnlyList<CallGraphNode> reachable =
    CallGraphTraverser.GetReachableCallNodes(startNode, ReachabilityDirection.Forward);
```

- `ReachabilityDirection.Forward`: follows `OutgoingEdges` (`caller -> callee`).
- `ReachabilityDirection.Backward`: follows `IncomingEdges` (`callee <- caller`).
- Results are deduplicated by method symbol and do not include the start node itself.

### Call Resolvers

Call resolvers implement `ICallResolver` and are tried in descending priority order. The first resolver that returns a non-empty result wins. Resolvers can be disabled via `CallGraphConfig.DisabledResolvers` (using the `CallResolver` enum) or the `--disable-resolver` CLI flag.

| Enum value                       | Class | Priority | What it does |
|----------------------------------|---|---|---|
| `Static`                         | `StaticCallResolver` | 0 (lowest) | Uses Roslyn static semantics to resolve direct invocations, object creations, property accessors, local functions, anonymous functions, and delegate creations. This is the baseline resolver and handles the majority of calls. |
| `IntraproceduralFunctionPointer` | `FunctionPointerResolver` | 30 | Resolves unsafe function pointer invocations (C# 9+ `delegate*` and `&` operator) by tracking `&MethodName` assignments within the method CFG (intraprocedural, stops at function boundary). |
| `IntraproceduralDelegate`        | `IntraproceduralDelegateResolver` | 50 | Resolves delegate invoke calls by tracking delegate assignments inside the current method CFG only (intraprocedural backward tracking). |
| `OperatorOverload`               | `OperatorOverloadCallResolver` | 75 | Resolves user-defined operator and conversion methods (`op_Addition`, `op_Equality`, `op_Implicit`, `op_Explicit`, etc.) from Roslyn operator/conversion operations. |
| `InterfaceImplementation`        | `InterfaceImplementationCallResolver` | 100 | When the call target is declared on an interface, uses `SymbolFinder.FindImplementationsAsync` to locate all concrete implementations in the project. More precise than RTA for interface calls but requires a full solution search. |
| `RapidTypeAnalysis`              | `RapidTypeAnalysisCallResolver` | 200 (highest) | Narrows virtual and interface dispatch to only the types actually instantiated in the project (RTA set). Avoids including dead subtypes in the call graph edges. See `samples/RtaNarrowingDemo/` for a worked example. |

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
- Implement CFG construction algorithms for:
  - delegates (intraprocedural done; interprocedural not yet supported), 
  - events, 
  - dynamic calls (will not be supported, requires dynamic trace), 
  - async/await (will not be supported),
  - advanced extension-method scenarios (e.g., complex generic constraints and ambiguous overload corner cases),
  - advanced operator-overloading scenarios (e.g., lifted/nullability and complex overload-selection corner cases),
  - unsafe function pointers (infrastructure ready; block on Roslyn `ptr()` invocation operation modeling),
  - advanced generic-method scenarios (e.g., per-instantiation node specialization, complex constraint solving, and deep inference chains),
  - partial methods, 
  - and linq.
- Add a test project with unit tests for core components (e.g., CFG construction, call resolution, dominator computation).
- Expand sample projects to cover more C# features and edge cases (e.g., generics, async/await, dynamic calls).

For repository-specific AI/developer guidance, see `AGENTS.md`.

# CFGAnalyzer

CFGAnalyzer is a CLI analyzer that builds Roslyn control-flow graphs (CFGs), constructs an interprocedural call graph, and reports path-oriented analysis data.

## Quick Start

```zsh
dotnet build CFGAnalyzer.csproj
dotnet run --project CFGAnalyzer.csproj -- /abs/path/to/Target.csproj
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

## Project Design

### Project Structure

- `Program.cs`: CLI entrypoint and orchestration (workspace setup, project loading, analysis trigger).
- `Analysis/ProjectAnalyzer.cs`: core pipeline coordinator that collects method-like declarations, builds CFGs, discovers call sites, and stores per-method analysis.
- `Analysis/CallGraphBuilder.cs`: turns discovered `CallSite` entries into `CallGraph` edges.
- `Analysis/CallResolver.cs`: pluggable resolution strategies via `ICallResolver` (static, interface implementation, and RTA narrowing).
- `Flow/FlowAnalysis.cs` + `Analysis/DataFlowAnalyzer.cs`: dominators and path constraint analysis.
- `Models/CFGModels.cs`: shared contracts (`CallGraph`, `CallGraphNode`, `CallGraphEdge`, `MethodAnalysisResult`, `CallSite`).
- `Traversal/CallGraphTraverser.cs`: traverses and prints callgraph-oriented analysis output.

### Pipeline

1. Open target `.csproj` or `.sln` with Roslyn/MSBuild.
2. Discover method-like members (methods, constructors, accessors, local functions, lambdas).
3. Build per-symbol CFGs from Roslyn operations.
4. Extract call sites from CFG operations and resolve targets through resolver priority order.
5. Build interprocedural call graph edges from resolved call sites.
6. Compute path/dominator data and attach it to method analysis results.
7. Traverse/report from selected entry nodes.

### Design Decision 

- Resolver logic is isolated behind `ICallResolver`, so precision can improve without rewriting graph construction.
- Symbol identity uses Roslyn symbol equality comparers to avoid fragile string matching.
- Analysis stages are separated (CFG, callgraph, dataflow, traversal), making each stage easier to modify and test.

### Future Work

- Implement Interprocedural pointer analysis to further narrow call targets.
- Add a test project with unit tests for core components (e.g., CFG construction, call resolution, dominator computation).
- Expand sample projects to cover more C# features and edge cases (e.g., generics, async/await, dynamic calls).

For repository-specific AI/developer guidance, see `AGENTS.md`.


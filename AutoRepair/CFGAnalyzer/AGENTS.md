# AGENTS.md

## Scope and intent
- This repo is a CLI analyzer that builds Roslyn CFGs for C# methods, then traverses an interprocedural call graph to print path constraints.
- Entry command accepts a `.sln` or `.csproj`; see `Program.cs` (`Main`).

## Big-picture architecture
- `Program.cs` orchestrates: register MSBuild, open workspace/project, call analysis, then traversal from entry nodes.
- `Analysis/ProjectAnalyzer.cs` is the analysis pipeline:
  1. Scan documents and collect method-like declarations (`MethodDeclarationSyntax`, constructors, accessors, local functions).
  2. Build `ControlFlowGraph` instances from Roslyn operations.
  3. Build interprocedural edges by visiting CFG operations and extracting call targets.
  4. Attach per-method analysis (`PathConstraints`, `CallSites`, `DominatorTree`) into `CallGraphNode.Analysis`.
- `Flow/FlowAnalysis.cs` contains CFG algorithms:
  - iterative dominator-set computation (`ComputeDominatorSets`),
  - immediate-dominator tree construction (`BuildDominatorTree`),
  - acyclic path exploration (`GetPathConstraintsAcyclic`, `GetPathsToBlockAcyclic`) with back-edge suppression via dominator checks.
- `Traversal/CallGraphTraverser.cs` performs DFS across internal calls and carries context (`prefixContext`) from caller paths to callee traversal.
- `Models/CFGModels.cs` defines the shared graph/model contracts (`CallGraph`, `CallGraphNode`, `CallGraphEdge`, `MethodAnalysisResult`, dominator tree types).

## Developer workflows (verified)
- Build: `dotnet build CFGAnalyzer.csproj`
- Run (usage): `dotnet run --project CFGAnalyzer.csproj --`
- Run analyzer on a target: `dotnet run --project CFGAnalyzer.csproj -- /abs/path/to/Target.sln`
- No test project is currently present in this workspace; validate changes by building and running against a small `.csproj`/`.sln`.

## Project-specific conventions and patterns
- Symbol identity always uses Roslyn symbol equality comparers (e.g., `Dictionary<IMethodSymbol,...>(SymbolEqualityComparer.Default)` in `CallGraph` and `methodCfgs`).
- Internal vs external calls are determined structurally: `methodCfgs.ContainsKey(target)` when creating `CallSite`.
- Traversal cycle control is symbol-based (`visited` `HashSet<IMethodSymbol>`), and each recursive branch clones visited/context state.
- Entry-point heuristic: prefer methods named `Main`; fallback to first node with available analysis (`Program.AnalyzeAndTraverse`).
- Nullable and implicit usings are enabled in `CFGAnalyzer.csproj`; keep new code nullable-aware.

## Integration points and dependencies
- Roslyn APIs: `Microsoft.CodeAnalysis.*`, especially `FlowAnalysis` and operation tree APIs.
- MSBuild integration: `MSBuildWorkspace` + `Microsoft.Build.Locator.RegisterDefaults()` must occur before opening solutions/projects.
- Workspace diagnostics are surfaced to console (`ReportDiagnostics` and `workspace.WorkspaceFailed`); preserve this behavior when modifying load flow.

## Practical guidance for agents
- Prefer extending `ProjectAnalyzer` for new extraction/analysis features; keep `Program` as thin orchestration.
- If adding new path logic, update both whole-method path generation and paths-to-call-site helpers to keep traversal context consistent.
- When changing graph models in `Models/CFGModels.cs`, check both analysis population and traversal consumers for breakage.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Sharper.CFGAnalyzer.Analysis.CallResolvers;

namespace Sharper.CFGAnalyzer.Analysis;

public static class ProjectAnalyzer
{
    public static async Task<string> AnalyzeProjectAsJsonAsync(Project project, bool indented = true,
        CallGraphConfig? config = null)
    {
        var callGraph = await AnalyzeProjectAsync(project, config);
        return CallGraphJsonExporter.ToJson(callGraph, project.Name, indented);
    }

    public static async Task<CallGraph> AnalyzeProjectAsync(Project project, CallGraphConfig? config = null)
    {
        config ??= new CallGraphConfig();
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return new CallGraph();

        var callGraph = new CallGraph();
        var methodCfgs = new Dictionary<IMethodSymbol, ControlFlowGraph>(SymbolEqualityComparer.Default);
        var methodAnalysisResults = new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);

        void Register(IMethodSymbol symbol, ControlFlowGraph cfg)
        {
            if (!methodCfgs.TryAdd(symbol, cfg)) return;
            methodAnalysisResults[symbol] = new MethodAnalysisResult(cfg, new List<CallSite>());
            foreach (var local in cfg.LocalFunctions)
                Register(local, cfg.GetLocalFunctionControlFlowGraph(local));
            var operations = cfg.Blocks.SelectMany(block => block.Operations.Concat(
                block.BranchValue == null ? [] : new[] { block.BranchValue }));
            foreach (var anonymous in operations.SelectMany(operation => operation.DescendantsAndSelf())
                         .OfType<IFlowAnonymousFunctionOperation>())
                Register(anonymous.Symbol, cfg.GetAnonymousFunctionControlFlowGraph(anonymous));
        }

        // 1. Collect all internal CFGs
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            if (root is CompilationUnitSyntax unit && unit.Members.Any(member => member is GlobalStatementSyntax)
                && semanticModel.GetOperation(unit) is IMethodBodyOperation topLevelBody
                && compilation.GetEntryPoint(default) is { } entryPoint)
                Register(entryPoint, ControlFlowGraph.Create(topLevelBody));

            var members = root.DescendantNodes().Where(n =>
                n is MethodDeclarationSyntax or ConstructorDeclarationSyntax or AccessorDeclarationSyntax
                    or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax
                    or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax);

            foreach (var member in members)
            {
                var operation = semanticModel.GetOperation(member);
                var symbol = semanticModel.GetDeclaredSymbol(member) as IMethodSymbol;

                // Skip early if there is no symbol (e.g. lambdas) to avoid building a CFG we can't store.
                if (symbol == null || methodCfgs.ContainsKey(symbol) || operation is ILocalFunctionOperation or IAnonymousFunctionOperation) continue;

                ControlFlowGraph? cfg = null;
                try
                {
                    cfg = operation switch
                    {
                        IBlockOperation blockOp => ControlFlowGraph.Create(blockOp),
                        IMethodBodyOperation mb => ControlFlowGraph.Create(mb),
                        IConstructorBodyOperation cb => ControlFlowGraph.Create(cb),
                        _ => cfg
                    };
                }
                catch (ArgumentException ex)
                {
                    var location = member.GetLocation().GetLineSpan().StartLinePosition;
                    var memberName = symbol.ToDisplayString();
                    Console.WriteLine(
                        $"[CFG Warning] Unable to build CFG for {memberName} at {document.Name}:{location.Line + 1}:{location.Character + 1}. {ex.Message}");
                    cfg = null;
                }

                if (cfg == null) continue;
                Register(symbol, cfg);
            }
        }

        // 2. Build Call Graph and discover Call Sites
        var instantiatedTypes = CollectInstantiatedTypes(methodCfgs.Values);
        var allResolvers = new List<ICallResolver>
        {
            new RapidTypeAnalysisCallResolver(instantiatedTypes),
            new InterfaceImplementationCallResolver(),
            new OperatorOverloadCallResolver(),
            new FunctionPointerResolver(),
            new IntraproceduralDelegateResolver(),
            new StaticCallResolver()
        };
        var activeResolvers = config.DisabledResolvers.Count == 0
            ? allResolvers
            : allResolvers.Where(r => !config.DisabledResolvers.Contains(r.Kind)).ToList();
        var callGraphBuilder = new CallGraphBuilder(activeResolvers, config);
        foreach (var kvp in methodCfgs)
        {
            var symbol = kvp.Key;
            var cfg = kvp.Value;
            var analysisResult = methodAnalysisResults[symbol];

            analysisResult.CallSites.AddRange(await callGraphBuilder.DiscoverCallSitesAsync(cfg, methodCfgs, project));
        }

        callGraphBuilder.BuildEdges(callGraph, methodAnalysisResults);

        // 3. Perform Data Flow Analysis
        var dataFlowAnalyzer = new DataFlowAnalyzer();
        foreach (var (symbol, analysisResult) in methodAnalysisResults)
        {
            analysisResult.DataFlow = dataFlowAnalyzer.Analyze(analysisResult.Cfg);
            callGraph.GetOrAddNode(symbol).Analysis = analysisResult;
        }

        return callGraph;
    }

    private static IEnumerable<INamedTypeSymbol> CollectInstantiatedTypes(IEnumerable<ControlFlowGraph> cfgs) =>
        cfgs.SelectMany(cfg => cfg.Blocks)
            .SelectMany(block => block.Operations)
            .SelectMany(op => op.DescendantsAndSelf())
            .OfType<IObjectCreationOperation>()
            .Select(op => op.Type)
            .OfType<INamedTypeSymbol>()
            .ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
}
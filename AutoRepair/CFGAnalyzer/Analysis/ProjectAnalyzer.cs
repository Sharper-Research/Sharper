using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations; // Add this using directive
using Microsoft.CodeAnalysis.CSharp.Syntax; // Add this using directive
using AutoRepair.CFGAnalyzer.Models;
using AutoRepair.CFGAnalyzer.Analysis; 
using AutoRepair.CFGAnalyzer.Flow; // Add this using directive for FlowAnalysis

namespace AutoRepair.CFGAnalyzer.Analysis;

public static class ProjectAnalyzer
{
    public static async Task<CallGraph> AnalyzeProjectAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return new CallGraph();

        var callGraph = new CallGraph();
        var methodCfgs = new Dictionary<IMethodSymbol, ControlFlowGraph>(SymbolEqualityComparer.Default);
        var methodAnalysisResults = new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);

        // 1. Collect all internal CFGs
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            var members = root.DescendantNodes().Where(n => n is MethodDeclarationSyntax or ConstructorDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

            foreach (var member in members)
            {
                var operation = semanticModel.GetOperation(member);
                IMethodSymbol? symbol = semanticModel.GetDeclaredSymbol(member) as IMethodSymbol;
                
                ControlFlowGraph? cfg = null;
                try
                {
                    if (operation is IBlockOperation blockOp) cfg = ControlFlowGraph.Create(blockOp);
                    else if (operation is IMethodBodyOperation mb) cfg = ControlFlowGraph.Create(mb);
                    else if (operation is IConstructorBodyOperation cb) cfg = ControlFlowGraph.Create(cb);
                    else if (operation is ILocalFunctionOperation local && local.Body != null) cfg = ControlFlowGraph.Create(local.Body);
                    else if (operation is IAnonymousFunctionOperation anon && anon.Body != null) cfg = ControlFlowGraph.Create(anon.Body);
                }
                catch (ArgumentException)
                {
                    cfg = null;
                }

                if (cfg != null && symbol != null)
                {
                    methodCfgs[symbol] = cfg;
                    methodAnalysisResults[symbol] = new MethodAnalysisResult(cfg, new List<CallSite>());
                }
            }
        }

        // 2. Build Call Graph and discover Call Sites
        var instantiatedTypes = CollectInstantiatedTypes(methodCfgs.Values);
        var callGraphBuilder = new CallGraphBuilder(new List<ICallResolver>
        {
            new RapidTypeAnalysisCallResolver(instantiatedTypes),
            new InterfaceImplementationCallResolver(),
            new StaticCallResolver()
        });
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
        foreach (var kvp in methodAnalysisResults)
        {
            var symbol = kvp.Key;
            var analysisResult = kvp.Value;
            
            analysisResult.DataFlow = dataFlowAnalyzer.Analyze(analysisResult.Cfg);
        }

        return callGraph;
    }

    private static IEnumerable<INamedTypeSymbol> CollectInstantiatedTypes(IEnumerable<ControlFlowGraph> cfgs)
    {
        var instantiated = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var cfg in cfgs)
        {
            foreach (var block in cfg.Blocks)
            {
                foreach (var op in block.Operations)
                {
                    foreach (var descendant in op.DescendantsAndSelf())
                    {
                        if (descendant is IObjectCreationOperation { Type: INamedTypeSymbol createdType })
                        {
                            instantiated.Add(createdType);
                        }
                    }
                }
            }
        }

        return instantiated;
    }
}

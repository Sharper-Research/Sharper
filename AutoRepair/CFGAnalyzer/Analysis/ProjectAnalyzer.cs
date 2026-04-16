using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using AutoRepair.CFGAnalyzer.Models;
using AutoRepair.CFGAnalyzer.Flow;

namespace AutoRepair.CFGAnalyzer.Analysis;

public static class ProjectAnalyzer
{
    public static async Task<CallGraph> AnalyzeProjectAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return new CallGraph();

        var callGraph = new CallGraph();
        var methodCfgs = new Dictionary<IMethodSymbol, ControlFlowGraph>(SymbolEqualityComparer.Default);

        // 1. Collect all internal CFGs
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            var members = root.DescendantNodes().Where(n => n is MethodDeclarationSyntax or ConstructorDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax);

            foreach (var member in members)
            {
                var operation = semanticModel.GetOperation(member);
                IMethodSymbol? symbol = semanticModel.GetDeclaredSymbol(member) as IMethodSymbol;
                
                ControlFlowGraph? cfg = null;
                if (operation is IBlockOperation blockOp) cfg = ControlFlowGraph.Create(blockOp);
                else if (operation is IMethodBodyOperation mb) cfg = ControlFlowGraph.Create(mb);
                else if (operation is IConstructorBodyOperation cb) cfg = ControlFlowGraph.Create(cb);
                else if (operation is ILocalFunctionOperation local && local.Body != null) cfg = ControlFlowGraph.Create(local.Body);

                if (cfg != null && symbol != null)
                {
                    methodCfgs[symbol] = cfg;
                }
            }
        }

        // 2. Perform analysis and build the Call Graph structure
        foreach (var kvp in methodCfgs)
        {
            var symbol = kvp.Key;
            var cfg = kvp.Value;
            var node = callGraph.GetOrAddNode(symbol);

            var callSites = new List<CallSite>();
            foreach (var block in cfg.Blocks)
            {
                foreach (var op in block.Operations)
                {
                    foreach (var descendant in op.DescendantsAndSelf())
                    {
                        IMethodSymbol? target = descendant switch
                        {
                            IInvocationOperation inv => inv.TargetMethod,
                            IObjectCreationOperation oc => oc.Constructor,
                            IPropertyReferenceOperation pr => pr.Property.GetMethod ?? pr.Property.SetMethod,
                            _ => null
                        };

                        if (target != null)
                        {
                            var site = new CallSite(block.Ordinal, target, methodCfgs.ContainsKey(target));
                            callSites.Add(site);
                            callGraph.AddEdge(symbol, target, site);
                        }
                    }
                }
            }

            var pathConstraints = Flow.FlowAnalysis.GetPathConstraintsAcyclic(cfg);
            var dominatorTree = Flow.FlowAnalysis.BuildDominatorTree(cfg);
            node.Analysis = new MethodAnalysisResult(cfg, pathConstraints, callSites, dominatorTree);
        }

        return callGraph;
    }
}

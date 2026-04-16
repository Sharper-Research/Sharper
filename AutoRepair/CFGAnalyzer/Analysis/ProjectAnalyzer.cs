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
    public static async Task<Dictionary<IMethodSymbol, MethodAnalysisResult>> AnalyzeProjectAsync(Project project)
    {
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);

        var results = new Dictionary<IMethodSymbol, MethodAnalysisResult>(SymbolEqualityComparer.Default);
        var methodCfgs = new Dictionary<IMethodSymbol, ControlFlowGraph>(SymbolEqualityComparer.Default);

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

        foreach (var kvp in methodCfgs)
        {
            var symbol = kvp.Key;
            var cfg = kvp.Value;

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
                            callSites.Add(new CallSite(block.Ordinal, target, methodCfgs.ContainsKey(target)));
                        }
                    }
                }
            }

            var pathConstraints = Flow.FlowAnalysis.GetPathConstraintsAcyclic(cfg);
            results[symbol] = new MethodAnalysisResult(cfg, pathConstraints, callSites);
        }

        return results;
    }
}

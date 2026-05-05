using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis.CallResolvers;

/// <summary>
///     Resolves calls using standard Roslyn static semantic analysis.
/// </summary>
public class StaticCallResolver : ICallResolver
{
    public CallResolver Kind => CallResolver.Static;
    public int Priority => 0;

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var target = operation switch
        {
            IInvocationOperation inv => inv.TargetMethod,
            IObjectCreationOperation oc => oc.Constructor,
            IPropertyReferenceOperation pr => pr.Property.GetMethod ?? pr.Property.SetMethod,
            ILocalFunctionOperation lf => lf.Symbol,
            IAnonymousFunctionOperation af => af.Symbol,
            IFlowAnonymousFunctionOperation faf => faf.Symbol,
            IDelegateCreationOperation dc => ResolveDelegateTarget(dc),
            _ => null
        };

        var result = target != null
            ? new[] { target }
            : Enumerable.Empty<IMethodSymbol>();

        return Task.FromResult(result);
    }

    private static IMethodSymbol? ResolveDelegateTarget(IDelegateCreationOperation delegateCreation)
    {
        return delegateCreation.Target switch
        {
            IMethodReferenceOperation methodRef => methodRef.Method,
            IAnonymousFunctionOperation anon => anon.Symbol,
            IFlowAnonymousFunctionOperation flowAnon => flowAnon.Symbol,
            _ => null
        };
    }
}


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis.CallResolvers;

/// <summary>
///     Resolves delegate invocations by performing backward dataflow tracking within the CFG
///     to find the delegate's concrete assignment target. Tracking stops at the function boundary.
/// </summary>
public class IntraproceduralDelegateResolver : ICfgAwareCallResolver
{
    public CallResolver Kind => CallResolver.IntraproceduralDelegate;

    /// <summary>
    ///     Runs at higher priority than <see cref="StaticCallResolver"/> so that when a
    ///     delegate-invoke is detected the concrete targets are returned before the static
    ///     resolver can only report the synthetic <c>Invoke</c> method.
    /// </summary>
    public int Priority => 50;

    private Dictionary<ISymbol, HashSet<IMethodSymbol>> _delegateTargets =
        new(SymbolEqualityComparer.Default);

    public void SetCfg(ControlFlowGraph cfg)
    {
        _delegateTargets = new Dictionary<ISymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var block in cfg.Blocks)
        foreach (var op in block.Operations)
        foreach (var descendant in op.DescendantsAndSelf())
            ScanForDelegateAssignment(descendant);
    }

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation ||
            invocation.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var instanceOp = invocation.Instance;
        if (instanceOp == null)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var symbol = ExtractSymbol(instanceOp);
        if (symbol == null || !_delegateTargets.TryGetValue(symbol, out var targets))
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        return Task.FromResult<IEnumerable<IMethodSymbol>>(targets);
    }

    private void ScanForDelegateAssignment(IOperation operation)
    {
        switch (operation)
        {
            case ISimpleAssignmentOperation { Target: var lhs, Value: var rhs }:
            {
                var sym = ExtractSymbol(lhs);
                var methods = ExtractDelegateMethods(rhs);
                if (sym != null && methods.Count > 0)
                    MergeInto(sym, methods);
                break;
            }

            case IVariableDeclaratorOperation { Initializer: { } init } declarator:
            {
                var methods = ExtractDelegateMethods(init.Value);
                if (methods.Count > 0)
                    MergeInto(declarator.Symbol, methods);
                break;
            }
        }
    }

    private void MergeInto(ISymbol symbol, HashSet<IMethodSymbol> methods)
    {
        if (!_delegateTargets.TryGetValue(symbol, out var existing))
        {
            existing = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            _delegateTargets[symbol] = existing;
        }

        foreach (var m in methods) existing.Add(m);
    }

    private static ISymbol? ExtractSymbol(IOperation operation) => operation switch
    {
        ILocalReferenceOperation local => local.Local,
        IParameterReferenceOperation param => param.Parameter,
        IFieldReferenceOperation field => field.Field,
        IPropertyReferenceOperation prop => prop.Property,
        _ => null
    };

    private static HashSet<IMethodSymbol> ExtractDelegateMethods(IOperation operation)
    {
        var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        var current = operation;
        while (current is IConversionOperation conv) current = conv.Operand;

        switch (current)
        {
            case IDelegateCreationOperation dc:
                var inner = dc.Target switch
                {
                    IMethodReferenceOperation mr => mr.Method,
                    IAnonymousFunctionOperation anon => anon.Symbol,
                    IFlowAnonymousFunctionOperation flowAnon => flowAnon.Symbol,
                    _ => null
                };
                if (inner != null) result.Add(inner);
                break;

            case IMethodReferenceOperation mr:
                result.Add(mr.Method);
                break;

            case IAnonymousFunctionOperation anon:
                result.Add(anon.Symbol);
                break;

            case IFlowAnonymousFunctionOperation flowAnon:
                result.Add(flowAnon.Symbol);
                break;
        }

        return result;
    }
}


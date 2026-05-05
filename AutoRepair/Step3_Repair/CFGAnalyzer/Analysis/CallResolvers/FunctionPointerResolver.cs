using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis.CallResolvers;

/// <summary>
///     Resolves unsafe function pointer invocations by tracking assignments within the CFG.
///     Requires explicit pointer dereference syntax; C# 9+ function pointer types are treated similarly.
/// </summary>
public class FunctionPointerResolver : ICfgAwareCallResolver
{
    public CallResolver Kind => CallResolver.IntraproceduralFunctionPointer;
    public int Priority => 30;

    private Dictionary<ISymbol, HashSet<IMethodSymbol>> _pointerTargets =
        new(SymbolEqualityComparer.Default);

    public void SetCfg(ControlFlowGraph cfg)
    {
        _pointerTargets = new Dictionary<ISymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var descendant in cfg.Blocks.SelectMany(block =>
                     block.Operations.SelectMany(op => op.DescendantsAndSelf())))
        {
            ScanForPointerAssignment(descendant);
        }
    }

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var instanceOp = invocation.Instance;
        if (instanceOp == null)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var symbol = ExtractSymbol(instanceOp);
        if (symbol == null || !_pointerTargets.TryGetValue(symbol, out var targets))
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        return Task.FromResult<IEnumerable<IMethodSymbol>>(targets);
    }

    private void ScanForPointerAssignment(IOperation operation)
    {
        switch (operation)
        {
            case ISimpleAssignmentOperation { Target: var lhs, Value: var rhs }:
            {
                var sym = ExtractSymbol(lhs);
                var methods = ExtractPointerMethods(rhs);
                if (sym != null && methods.Count > 0)
                    MergeInto(sym, methods);
                break;
            }

            case IVariableDeclaratorOperation { Initializer: { } init } declarator:
            {
                var methods = ExtractPointerMethods(init.Value);
                if (methods.Count > 0)
                    MergeInto(declarator.Symbol, methods);
                break;
            }
        }
    }

    private void MergeInto(ISymbol symbol, HashSet<IMethodSymbol> methods)
    {
        if (!_pointerTargets.TryGetValue(symbol, out var existing))
        {
            existing = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            _pointerTargets[symbol] = existing;
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

    private static HashSet<IMethodSymbol> ExtractPointerMethods(IOperation operation)
    {
        var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        var current = operation;
        while (current is IConversionOperation conv) current = conv.Operand;

        if (current is IUnaryOperation { OperatorKind: var opKind } unary && opKind.ToString().Contains("AddressOf"))
        {
            if (unary.Operand is IMethodReferenceOperation mr)
                result.Add(mr.Method);
        }

        if (current is IMethodReferenceOperation methodRef)
            result.Add(methodRef.Method);

        return result;
    }
}



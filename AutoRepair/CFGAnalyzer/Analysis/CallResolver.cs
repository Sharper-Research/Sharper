using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis;

/// <summary>
///     Defines a strategy for resolving the target method of an operation.
/// </summary>
public interface ICallResolver
{
    CallResolver Kind { get; }

    /// <summary>
    ///     Higher priority resolvers are tried first.
    /// </summary>
    int Priority { get; }

    Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Extends <see cref="ICallResolver"/> with the ability to receive the current CFG before
///     call-site discovery begins, enabling intraprocedural dataflow-based resolution.
/// </summary>
public interface ICfgAwareCallResolver : ICallResolver
{
    /// <summary>
    ///     Called once per method CFG before any <see cref="ICallResolver.ResolveAsync"/> calls
    ///     for that method. Implementations should pre-compute any per-CFG state here.
    /// </summary>
    void SetCfg(ControlFlowGraph cfg);
}

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

    // Maps every delegate-holding symbol found in the current CFG to the concrete
    // method(s) it was assigned.  Rebuilt on each SetCfg call.
    private Dictionary<ISymbol, HashSet<IMethodSymbol>> _delegateTargets =
        new(SymbolEqualityComparer.Default);

    /// <inheritdoc/>
    public void SetCfg(ControlFlowGraph cfg)
    {
        _delegateTargets = new Dictionary<ISymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var block in cfg.Blocks)
        foreach (var op in block.Operations)
        foreach (var descendant in op.DescendantsAndSelf())
            ScanForDelegateAssignment(descendant);
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        // We only care about invocations whose target is the synthetic Invoke method
        // of a delegate type – that is how Roslyn models `myDelegate(args)` calls.
        if (operation is not IInvocationOperation invocation ||
            invocation.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        // The delegate instance being invoked
        var instanceOp = invocation.Instance;
        if (instanceOp == null)
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var symbol = ExtractSymbol(instanceOp);
        if (symbol == null || !_delegateTargets.TryGetValue(symbol, out var targets))
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        return Task.FromResult<IEnumerable<IMethodSymbol>>(targets);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ScanForDelegateAssignment(IOperation operation)
    {
        switch (operation)
        {
            // `x = SomeMethod` / `x = () => ...`
            case ISimpleAssignmentOperation { Target: var lhs, Value: var rhs }:
            {
                var sym = ExtractSymbol(lhs);
                var methods = ExtractDelegateMethods(rhs);
                if (sym != null && methods.Count > 0)
                    MergeInto(sym, methods);
                break;
            }

            // `DelegateType x = SomeMethod`  (initialiser inside declaration)
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

    /// <summary>
    ///     Extracts the named symbol from a reference operation (local, parameter, field, property).
    /// </summary>
    private static ISymbol? ExtractSymbol(IOperation operation) => operation switch
    {
        ILocalReferenceOperation local => local.Local,
        IParameterReferenceOperation param => param.Parameter,
        IFieldReferenceOperation field => field.Field,
        IPropertyReferenceOperation prop => prop.Property,
        _ => null
    };

    /// <summary>
    ///     Walks through optional conversion wrappers and returns every concrete
    ///     <see cref="IMethodSymbol"/> reachable from a delegate-valued expression.
    /// </summary>
    private static HashSet<IMethodSymbol> ExtractDelegateMethods(IOperation operation)
    {
        var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        // Strip implicit conversions (e.g. method-group → delegate)
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

/// <summary>
///     Resolves interface invocation targets to concrete implementations in the project.
/// </summary>
public class InterfaceImplementationCallResolver : ICallResolver
{
    public CallResolver Kind => CallResolver.InterfaceImplementation;
    public int Priority => 100;

    public async Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation) return Enumerable.Empty<IMethodSymbol>();

        var target = invocation.TargetMethod;
        if (target.ContainingType.TypeKind != TypeKind.Interface) return Enumerable.Empty<IMethodSymbol>();

        var implementationTypes = await SymbolFinder.FindImplementationsAsync(
            target.ContainingType,
            project.Solution,
            projects: ImmutableHashSet.Create(project),
            cancellationToken: cancellationToken);

        return implementationTypes
            .OfType<INamedTypeSymbol>()
            .Select(type => type.FindImplementationForInterfaceMember(target))
            .OfType<IMethodSymbol>()
            .Where(method => !SymbolEqualityComparer.Default.Equals(method, target));
    }
}

/// <summary>
///     Resolves dynamic dispatch targets using a Rapid Type Analysis (RTA) set of instantiated types.
/// </summary>
public class RapidTypeAnalysisCallResolver : ICallResolver
{
    private readonly HashSet<INamedTypeSymbol> _instantiatedTypes;

    public RapidTypeAnalysisCallResolver(IEnumerable<INamedTypeSymbol> instantiatedTypes)
    {
        _instantiatedTypes = new HashSet<INamedTypeSymbol>(instantiatedTypes, SymbolEqualityComparer.Default);
    }

    public CallResolver Kind => CallResolver.RapidTypeAnalysis;
    public int Priority => 200;

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation) return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var target = invocation.TargetMethod;
        if (target.MethodKind != MethodKind.Ordinary) return Task.FromResult(Enumerable.Empty<IMethodSymbol>());

        var resolved = target.ContainingType.TypeKind == TypeKind.Interface
            ? ResolveInterfaceDispatch(target)
            : ResolveVirtualDispatch(target);

        return Task.FromResult(resolved);
    }

    private IEnumerable<IMethodSymbol> ResolveInterfaceDispatch(IMethodSymbol target)
    {
        var resolved = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in _instantiatedTypes)
        {
            if (type.IsAbstract || type.TypeKind != TypeKind.Class) continue;

            if (!type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target.ContainingType))) continue;

            if (type.FindImplementationForInterfaceMember(target) is IMethodSymbol method &&
                !SymbolEqualityComparer.Default.Equals(method, target))
                resolved.Add(method);
        }

        return resolved;
    }

    private IEnumerable<IMethodSymbol> ResolveVirtualDispatch(IMethodSymbol target)
    {
        if (!target.IsVirtual && !target.IsAbstract && !target.IsOverride) return Enumerable.Empty<IMethodSymbol>();

        var resolved = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in _instantiatedTypes.Where(type => !type.IsAbstract && type.TypeKind == TypeKind.Class)
                     .Where(type => SymbolEqualityComparer.Default.Equals(type, target.ContainingType) ||
                                    InheritsFrom(type, target.ContainingType)))
        {
            resolved.Add(ResolveOverrideOnType(type, target));
        }

        return resolved;
    }

    private static bool InheritsFrom(INamedTypeSymbol candidate, INamedTypeSymbol potentialBase)
    {
        for (var current = candidate.BaseType; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, potentialBase))
                return true;

        return false;
    }

    private static IMethodSymbol ResolveOverrideOnType(INamedTypeSymbol concreteType, IMethodSymbol target)
    {
        var targetDef = target.OriginalDefinition;

        for (var current = concreteType; current != null; current = current.BaseType)
        {
            var candidate = current.GetMembers(target.Name)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m =>
                    m.MethodKind == MethodKind.Ordinary &&
                    IsDispatchMatch(m, target, targetDef));

            if (candidate != null) return candidate;
        }

        return target;
    }

    private static bool IsDispatchMatch(IMethodSymbol candidate, IMethodSymbol target, IMethodSymbol targetDef)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, target) ||
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, targetDef))
            return true;

        for (var current = candidate.OverriddenMethod; current != null; current = current.OverriddenMethod)
            if (SymbolEqualityComparer.Default.Equals(current, target) ||
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, targetDef))
                return true;

        return false;
    }
}

/// <summary>
///     Resolves user-defined operator and conversion calls from Roslyn operator operations.
/// </summary>
public class OperatorOverloadCallResolver : ICallResolver
{
    public CallResolver Kind => CallResolver.OperatorOverload;

    // Run before static fallback so user-defined operators are emitted from
    // IBinary/IUnary/IConversion operations encountered in expression trees.
    public int Priority => 75;

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var target = operation switch
        {
            IBinaryOperation { OperatorMethod: { } op } => op,
            IUnaryOperation { OperatorMethod: { } op } => op,
            IIncrementOrDecrementOperation { OperatorMethod: { } op } => op,
            ICompoundAssignmentOperation { OperatorMethod: { } op } => op,
            IConversionOperation { OperatorMethod: { } op } => op,
            _ => null
        };

        var result = target != null
            ? new[] { target }
            : Enumerable.Empty<IMethodSymbol>();

        return Task.FromResult(result);
    }
}

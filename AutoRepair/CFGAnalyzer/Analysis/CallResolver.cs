using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis;

/// <summary>
/// Defines a strategy for resolving the target method of an operation.
/// </summary>
public interface ICallResolver
{
    /// <summary>
    /// Higher priority resolvers are tried first.
    /// </summary>
    int Priority { get; }

    Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves calls using standard Roslyn static semantic analysis.
/// </summary>
public class StaticCallResolver : ICallResolver
{
    public int Priority => 0;

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        IMethodSymbol? target = operation switch
        {
            IInvocationOperation inv => inv.TargetMethod,
            IObjectCreationOperation oc => oc.Constructor,
            IPropertyReferenceOperation pr => pr.Property.GetMethod ?? pr.Property.SetMethod,
            _ => null
        };

        IEnumerable<IMethodSymbol> result = target != null
            ? new[] { target }
            : Enumerable.Empty<IMethodSymbol>();

        return Task.FromResult(result);
    }
}

/// <summary>
/// Resolves interface invocation targets to concrete implementations in the project.
/// </summary>
public class InterfaceImplementationCallResolver : ICallResolver
{
    public int Priority => 100;

    public async Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation)
        {
            return Enumerable.Empty<IMethodSymbol>();
        }

        var target = invocation.TargetMethod;
        if (target.ContainingType.TypeKind != TypeKind.Interface)
        {
            return Enumerable.Empty<IMethodSymbol>();
        }

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
/// Resolves dynamic dispatch targets using a Rapid Type Analysis (RTA) set of instantiated types.
/// </summary>
public class RapidTypeAnalysisCallResolver : ICallResolver
{
    private readonly HashSet<INamedTypeSymbol> _instantiatedTypes;

    public int Priority => 200;

    public RapidTypeAnalysisCallResolver(IEnumerable<INamedTypeSymbol> instantiatedTypes)
    {
        _instantiatedTypes = new HashSet<INamedTypeSymbol>(instantiatedTypes, SymbolEqualityComparer.Default);
    }

    public Task<IEnumerable<IMethodSymbol>> ResolveAsync(
        IOperation operation,
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (operation is not IInvocationOperation invocation)
        {
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());
        }

        var target = invocation.TargetMethod;
        if (target.MethodKind != MethodKind.Ordinary)
        {
            return Task.FromResult(Enumerable.Empty<IMethodSymbol>());
        }

        IEnumerable<IMethodSymbol> resolved = target.ContainingType.TypeKind == TypeKind.Interface
            ? ResolveInterfaceDispatch(target)
            : ResolveVirtualDispatch(target);

        return Task.FromResult(resolved);
    }

    private IEnumerable<IMethodSymbol> ResolveInterfaceDispatch(IMethodSymbol target)
    {
        var resolved = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in _instantiatedTypes)
        {
            if (type.IsAbstract || type.TypeKind != TypeKind.Class)
            {
                continue;
            }

            if (!type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target.ContainingType)))
            {
                continue;
            }

            if (type.FindImplementationForInterfaceMember(target) is IMethodSymbol method &&
                !SymbolEqualityComparer.Default.Equals(method, target))
            {
                resolved.Add(method);
            }
        }

        return resolved;
    }

    private IEnumerable<IMethodSymbol> ResolveVirtualDispatch(IMethodSymbol target)
    {
        if (!target.IsVirtual && !target.IsAbstract && !target.IsOverride)
        {
            return Enumerable.Empty<IMethodSymbol>();
        }

        var resolved = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var type in _instantiatedTypes)
        {
            if (type.IsAbstract || type.TypeKind != TypeKind.Class)
            {
                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(type, target.ContainingType) &&
                !InheritsFrom(type, target.ContainingType))
            {
                continue;
            }

            var resolvedMethod = ResolveOverrideOnType(type, target);
            if (resolvedMethod != null)
            {
                resolved.Add(resolvedMethod);
            }
        }

        return resolved;
    }

    private static bool InheritsFrom(INamedTypeSymbol candidate, INamedTypeSymbol potentialBase)
    {
        for (var current = candidate.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, potentialBase))
            {
                return true;
            }
        }

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

            if (candidate != null)
            {
                return candidate;
            }
        }

        return target;
    }

    private static bool IsDispatchMatch(IMethodSymbol candidate, IMethodSymbol target, IMethodSymbol targetDef)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, target) ||
            SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, targetDef))
        {
            return true;
        }

        for (var current = candidate.OverriddenMethod; current != null; current = current.OverriddenMethod)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target) ||
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, targetDef))
            {
                return true;
            }
        }

        return false;
    }
}


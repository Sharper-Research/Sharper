using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace Sharper.CFGAnalyzer.Analysis.CallResolvers;

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



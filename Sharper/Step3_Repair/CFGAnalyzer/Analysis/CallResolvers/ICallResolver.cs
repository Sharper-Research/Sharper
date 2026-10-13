using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Sharper.CFGAnalyzer.Analysis.CallResolvers;

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


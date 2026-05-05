using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace AutoRepair.CFGAnalyzer.Analysis.CallResolvers;

/// <summary>
///     Resolves user-defined operator and conversion calls from Roslyn operator operations.
/// </summary>
public class OperatorOverloadCallResolver : ICallResolver
{
    public CallResolver Kind => CallResolver.OperatorOverload;
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


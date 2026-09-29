using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Sharper.CFGAnalyzer.Analysis;

internal sealed class PaperSourceModel
{
    private readonly List<PaperNode> nodes = [];
    private readonly Dictionary<string, PaperSort> variables = new(StringComparer.Ordinal);
    private readonly HashSet<IMethodSymbol> activeCalls = new(SymbolEqualityComparer.Default);
    private int sequence;
    private int frames;

    public static PaperPropagationResult Analyze(SemanticModel model, MethodDeclarationSyntax method,
        StatementSyntax failureStatement, PaperExpression predicate)
    {
        try
        {
            if (model.Compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                throw new NotSupportedException("Source contains compilation errors");
            var builder = new PaperSourceModel();
            builder.nodes.Add(new("exit", 0, []));
            builder.nodes.Add(new("failure", failureStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1, []));
            var entry = builder.LowerMethod(model, method, "exit", null, [], failureStatement);
            var result = PaperForwardVerifier.Verify(builder.nodes, entry,
                new Dictionary<string, PaperExpression> { ["failure"] = predicate },
                PaperExpression.Constant("true"), builder.variables, exploreCycles: true);
            return new("ForwardOnlySupportedSource", null, [], [], "Exact source exploration; no cyclic backward predicate claimed")
            {
                ProposedEntryCondition = "true",
                ForwardModelCheck = result,
                ForwardVerificationScope = "Selected failure and all lowered Int32 array element accesses in supported source; not generated-patch correctness, functional equivalence, or resource-exhaustion safety"
            };
        }
        catch (NotSupportedException exception)
        {
            return new("Incomplete", null, [], [], exception.Message);
        }
    }

    private static PaperSort Sort(ITypeSymbol? type) => type switch
    {
        { SpecialType: SpecialType.System_Int32 } => PaperSort.Int32,
        { SpecialType: SpecialType.System_Boolean } => PaperSort.Boolean,
        { IsReferenceType: true } => PaperSort.Reference,
        _ => throw new NotSupportedException("Source model supports only Int32, Boolean and reference variables")
    };

    private string Add(SyntaxNode syntax, params PaperEdge[] successors)
    {
        if (nodes.Count >= 1000) throw new NotSupportedException("Source expansion budget exceeded");
        var identity = "source-" + ++sequence;
        nodes.Add(new(identity, syntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1, successors));
        return identity;
    }

    private string LowerMethod(SemanticModel model, MethodDeclarationSyntax method, string exit,
        string? destination, IReadOnlyList<PaperExpression> arguments, StatementSyntax? failureStatement)
    {
        var symbol = model.GetDeclaredSymbol(method) ?? throw new NotSupportedException("Missing method symbol");
        if (!activeCalls.Add(symbol)) throw new NotSupportedException("Recursive source calls require a verified recursive contract");
        try
        {
            if (method.Body == null || symbol.IsAsync || method.DescendantNodes().Any(node => node is
                LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax or AwaitExpressionSyntax or YieldStatementSyntax))
                throw new NotSupportedException("Source calls require synchronous block bodies without nested functions");
            var prefix = destination == null ? "" : "$source-call" + ++frames + "/";
            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var declaration in method.DescendantNodes().Where(node => node is ParameterSyntax or VariableDeclaratorSyntax))
            {
                var declared = model.GetDeclaredSymbol(declaration);
                var type = declared switch { ILocalSymbol local => local.Type, IParameterSymbol parameter => parameter.Type, _ => null };
                var name = PaperSourceAnalysis.Identity(declared);
                names.Add(name, prefix + name);
                variables.Add(prefix + name, Sort(type));
            }

            PaperExpression Rename(PaperExpression expression) => expression.Operator == "variable"
                ? PaperExpression.Variable(names[expression.Atom!])
                : expression with { Operands = [.. expression.Operands.Select(Rename)] };
            PaperExpression Expression(IOperation? operation) => Rename(PaperSourceAnalysis.Expression(operation));
            string Target(IOperation operation) => operation switch
            {
                ILocalReferenceOperation local => names[PaperSourceAnalysis.Identity(local.Local)],
                IParameterReferenceOperation parameter => names[PaperSourceAnalysis.Identity(parameter.Parameter)],
                _ => throw new NotSupportedException("Source model rejects heap writes and ref aliases")
            };

            string Value(SyntaxNode syntax, string target, IOperation? operation, string next)
            {
                if (operation is IArrayCreationOperation allocation)
                {
                    if (allocation.Type is not IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Int32 }
                        || allocation.Initializer != null || allocation.DimensionSizes.Length != 1
                        || allocation.DimensionSizes[0].ConstantValue is not { HasValue: true, Value: int size } || size < 0)
                        throw new NotSupportedException("Only fixed nonnegative-size Int32 array allocations without initializers are supported");
                    return Add(syntax, new PaperEdge(next, new PaperArrayAllocation(target, size)));
                }
                if (operation is IArrayElementReferenceOperation { Indices.Length: 1, Type.SpecialType: SpecialType.System_Int32 } read)
                    return Add(syntax, new PaperEdge(next, new PaperArrayRead(target, Expression(read.ArrayReference), Expression(read.Indices[0]))));
                if (operation is not IInvocationOperation call)
                    return Add(syntax, new PaperEdge(next, new PaperAssignment(target, Expression(operation))));
                if (!call.TargetMethod.IsStatic || call.TargetMethod.IsGenericMethod || call.TargetMethod.IsExtern
                    || call.TargetMethod.Parameters.Any(parameter => parameter.RefKind != RefKind.None)
                    || call.Arguments.Any(argument => argument.ArgumentKind != ArgumentKind.Explicit))
                    throw new NotSupportedException("Only direct non-generic static calls with explicit value arguments are supported");
                var declaration = call.TargetMethod.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() as MethodDeclarationSyntax
                    ?? throw new NotSupportedException("Called method body is unavailable");
                if (call.TargetMethod.ContainingType.GetMembers().Any(member => member is IFieldSymbol { IsConst: false }
                    or IMethodSymbol { MethodKind: MethodKind.StaticConstructor }))
                    throw new NotSupportedException("Type initialization and static state effects require additional semantics");
                var ordered = call.Arguments.OrderBy(argument => argument.Parameter!.Ordinal).Select(argument => Expression(argument.Value)).ToArray();
                return LowerMethod(model.Compilation.GetSemanticModel(declaration.SyntaxTree), declaration, next, target, ordered, null);
            }

            string Effect(SyntaxNode syntax, IOperation? operation, string next) => operation switch
            {
                ISimpleAssignmentOperation { Target: IArrayElementReferenceOperation { Indices.Length: 1, Type.SpecialType: SpecialType.System_Int32 } write } assignment =>
                    Add(syntax, new PaperEdge(next, new PaperArrayWrite(Expression(write.ArrayReference), Expression(write.Indices[0]), Expression(assignment.Value)))),
                ISimpleAssignmentOperation assignment => Value(syntax, Target(assignment.Target), assignment.Value, next),
                IIncrementOrDecrementOperation increment when increment.OperatorMethod == null && increment.Type?.SpecialType == SpecialType.System_Int32 =>
                    Add(syntax, new PaperEdge(next, new PaperAssignment(Target(increment.Target), PaperExpression.Apply(
                        (increment.IsChecked ? "int32.checked" : "int32.unchecked") + (increment.Kind == OperationKind.Increment ? "+" : "-"),
                        Expression(increment.Target), PaperExpression.Constant("1"))))),
                _ => throw new NotSupportedException("Unsupported source expression statement")
            };

            string Declare(VariableDeclarationSyntax declaration, string next)
            {
                foreach (var variable in declaration.Variables.Reverse())
                {
                    if (variable.Initializer == null) throw new NotSupportedException("Uninitialized locals are unsupported");
                    next = Value(variable, names[PaperSourceAnalysis.Identity(model.GetDeclaredSymbol(variable))],
                        model.GetOperation(variable.Initializer.Value), next);
                }
                return next;
            }

            string Lower(StatementSyntax statement, string next, string? breakTarget = null, string? continueTarget = null)
            {
                if (statement == failureStatement)
                {
                    if (statement is LocalDeclarationStatementSyntax localFailure
                        && localFailure.UsingKeyword.IsKind(SyntaxKind.None)
                        && model.GetOperation(localFailure.Declaration.Variables[0].Initializer!.Value)
                            is IArrayElementReferenceOperation { Indices.Length: 1, Type.SpecialType: SpecialType.System_Int32 } access)
                    {
                        var target = names[PaperSourceAnalysis.Identity(model.GetDeclaredSymbol(localFailure.Declaration.Variables[0]))];
                        var failureIndex = nodes.FindIndex(node => node.Id == "failure");
                        nodes[failureIndex] = nodes[failureIndex] with { Successors = [new(next,
                            new PaperArrayRead(target, Expression(access.ArrayReference), Expression(access.Indices[0])))] };
                    }
                    else if (statement is ExpressionStatementSyntax expression
                        && model.GetOperation(expression.Expression) is ISimpleAssignmentOperation { Target: IArrayElementReferenceOperation })
                    {
                        var continuation = Effect(statement, model.GetOperation(expression.Expression), next);
                        var failureIndex = nodes.FindIndex(node => node.Id == "failure");
                        nodes[failureIndex] = nodes[failureIndex] with { Successors = [new(continuation, new PaperFrame([]))] };
                    }
                    else if (statement is not ReturnStatementSyntax)
                        throw new NotSupportedException("Non-return failure sites require modeled continuation effects");
                    return "failure";
                }
                switch (statement)
                {
                    case BlockSyntax block:
                        foreach (var child in block.Statements.Reverse()) next = Lower(child, next, breakTarget, continueTarget);
                        return next;
                    case IfStatementSyntax branch:
                        var condition = Expression(model.GetOperation(branch.Condition));
                        return Add(branch, new(Lower(branch.Statement, next, breakTarget, continueTarget), new PaperGuard(condition, true)),
                            new(branch.Else == null ? next : Lower(branch.Else.Statement, next, breakTarget, continueTarget), new PaperGuard(condition, false)));
                    case WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax:
                        var head = Add(statement);
                        var conditionSyntax = statement switch { WhileStatementSyntax loop => loop.Condition, DoStatementSyntax loop => loop.Condition, ForStatementSyntax loop => loop.Condition, _ => null };
                        var test = conditionSyntax == null ? PaperExpression.Constant("true") : Expression(model.GetOperation(conditionSyntax));
                        var body = statement switch { WhileStatementSyntax loop => loop.Statement, DoStatementSyntax loop => loop.Statement, ForStatementSyntax loop => loop.Statement, _ => throw new NotSupportedException() };
                        var iteration = head;
                        if (statement is ForStatementSyntax forLoop)
                            foreach (var increment in forLoop.Incrementors.Reverse()) iteration = Effect(increment, model.GetOperation(increment), iteration);
                        var bodyEntry = Lower(body, iteration, next, iteration);
                        var headIndex = nodes.FindIndex(node => node.Id == head);
                        nodes[headIndex] = nodes[headIndex] with { Successors = [new(bodyEntry, new PaperGuard(test, true)), new(next, new PaperGuard(test, false))] };
                        var loopEntry = statement is DoStatementSyntax ? bodyEntry : head;
                        if (statement is ForStatementSyntax initialized)
                        {
                            foreach (var initializer in initialized.Initializers.Reverse()) loopEntry = Effect(initializer, model.GetOperation(initializer), loopEntry);
                            if (initialized.Declaration != null) loopEntry = Declare(initialized.Declaration, loopEntry);
                        }
                        return loopEntry;
                    case BreakStatementSyntax when breakTarget != null: return breakTarget;
                    case ContinueStatementSyntax when continueTarget != null: return continueTarget;
                    case ReturnStatementSyntax returned:
                        if (destination != null) return Value(returned, destination, model.GetOperation(returned.Expression!), exit);
                        if (returned.Expression != null) _ = Expression(model.GetOperation(returned.Expression));
                        return exit;
                    case LocalDeclarationStatementSyntax local when local.UsingKeyword.IsKind(SyntaxKind.None): return Declare(local.Declaration, next);
                    case ExpressionStatementSyntax expression: return Effect(expression, model.GetOperation(expression.Expression), next);
                    case EmptyStatementSyntax: return next;
                    default: throw new NotSupportedException("Unsupported source statement: " + statement.Kind());
                }
            }

            var entry = Lower(method.Body, exit);
            if (destination != null)
                for (var argument = symbol.Parameters.Length - 1; argument >= 0; argument--)
                    entry = Add(method, new PaperEdge(entry, new PaperAssignment(names[PaperSourceAnalysis.Identity(symbol.Parameters[argument])], arguments[argument])));
            return entry;
        }
        finally { activeCalls.Remove(symbol); }
    }
}
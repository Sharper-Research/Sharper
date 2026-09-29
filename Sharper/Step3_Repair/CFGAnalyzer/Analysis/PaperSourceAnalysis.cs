using System.Globalization;
using Sharper.CFGAnalyzer.Repair;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Sharper.CFGAnalyzer.Analysis;

public static class PaperSourceAnalysis
{
    public static async Task<PaperPropagationResult> AnalyzeAsync(Document document, Violation violation)
    {
        var root = await document.GetSyntaxRootAsync();
        var model = await document.GetSemanticModelAsync();
        if (root == null || model == null) return Incomplete("No semantic model");
        try
        {
            var lineStatements = root.DescendantNodes().OfType<StatementSyntax>()
                .Where(statement => statement is not BlockSyntax && statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1 == violation.Line)
                .ToArray();
            if (lineStatements.Length != 1) return Incomplete("Failure line must identify exactly one statement");
            var failureStatement = lineStatements[0];
            var method = failureStatement.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (method?.Body == null) return Incomplete("Only block-bodied methods are supported");
            if (method.Modifiers.Any(SyntaxKind.AsyncKeyword)
                || method.DescendantNodes().Any(node => node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax
                    or YieldStatementSyntax or AwaitExpressionSyntax))
                return Incomplete("Async, iterator and nested-function semantics are unsupported");

            ExpressionSyntax? failingExpression = failureStatement switch
            {
                ReturnStatementSyntax returned => returned.Expression,
                LocalDeclarationStatementSyntax local when local.Declaration.Variables.Count == 1 => local.Declaration.Variables[0].Initializer?.Value,
                ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
                    when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) => assignment.Left,
                _ => null
            };
            if (failingExpression == null) return Incomplete("Failure must be a direct return, single local initializer, or array write");
            var operation = model.GetOperation(failingExpression);
            var initial = Initialize(operation, violation.Predicate);
            if (failureStatement is ExpressionStatementSyntax || method.DescendantNodes().Any(node => node is
                WhileStatementSyntax or ForStatementSyntax or DoStatementSyntax or InvocationExpressionSyntax or ArrayCreationExpressionSyntax))
                return PaperSourceModel.Analyze(model, method, failureStatement, initial);
            var nodes = new List<PaperNode> { new("exit", 0, []), new("failure", violation.Line, []) };
            var sequence = 0;

            string Add(StatementSyntax statement, IReadOnlyList<PaperEdge> successors)
            {
                var identity = $"statement-{++sequence}";
                nodes.Add(new(identity, statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1, successors));
                return identity;
            }

            string Lower(StatementSyntax statement, string next)
            {
                if (statement == failureStatement) return "failure";
                if (statement is BlockSyntax block)
                {
                    var entry = next;
                    foreach (var child in block.Statements.Reverse()) entry = Lower(child, entry);
                    return entry;
                }
                if (statement is IfStatementSyntax branch)
                {
                    var condition = Expression(model.GetOperation(branch.Condition));
                    return Add(statement, [
                        new(Lower(branch.Statement, next), new PaperGuard(condition, true)),
                        new(branch.Else == null ? next : Lower(branch.Else.Statement, next), new PaperGuard(condition, false))
                    ]);
                }
                if (statement is ReturnStatementSyntax or ThrowStatementSyntax) return Add(statement, []);
                PaperStatement transfer = statement switch
                {
                    EmptyStatementSyntax => new PaperFrame([]),
                    LocalDeclarationStatementSyntax local when local.Declaration.Variables.Count == 1
                        && local.UsingKeyword.IsKind(SyntaxKind.None)
                        && local.Declaration.Variables[0].Initializer != null =>
                        new PaperAssignment(Identity(model.GetDeclaredSymbol(local.Declaration.Variables[0])),
                            Expression(model.GetOperation(local.Declaration.Variables[0].Initializer!.Value))),
                    ExpressionStatementSyntax expression when model.GetOperation(expression.Expression) is ISimpleAssignmentOperation assignment =>
                        new PaperAssignment(AssignedIdentity(assignment.Target), Expression(assignment.Value)),
                    _ => new PaperUnsupported($"Unsupported statement: {statement.Kind()} at line {statement.GetLocation().GetLineSpan().StartLinePosition.Line + 1}")
                };
                if (statement.DescendantNodesAndSelf().Contains(failureStatement))
                    throw new NotSupportedException("Failure is nested in unsupported control flow (including loops)");
                return Add(statement, [new(next, transfer)]);
            }

            var entry = Lower(method.Body, "exit");
            var result = PaperRuleEngine.Propagate(nodes, entry, "failure", initial);
            if (result.EntryExpression == null) return result;
            var variables = new Dictionary<string, PaperSort>(StringComparer.Ordinal);
            foreach (var declaration in method.DescendantNodes().Where(node => node is ParameterSyntax or VariableDeclaratorSyntax))
            {
                var symbol = model.GetDeclaredSymbol(declaration);
                var type = symbol switch { ILocalSymbol local => local.Type, IParameterSymbol parameter => parameter.Type, _ => null };
                if (type?.SpecialType == SpecialType.System_Int32) variables[Identity(symbol)] = PaperSort.Int32;
                else if (type?.SpecialType == SpecialType.System_Boolean) variables[Identity(symbol)] = PaperSort.Boolean;
                else if (type?.IsReferenceType == true) variables[Identity(symbol)] = PaperSort.Reference;
            }
            var condition = PaperExpression.Apply("!", result.EntryExpression);
            return result with
            {
                ProposedEntryCondition = condition.ToString(),
                ForwardModelCheck = PaperForwardVerifier.Verify(nodes, entry,
                    new Dictionary<string, PaperExpression> { ["failure"] = initial }, condition, variables)
            };
        }
        catch (NotSupportedException exception)
        {
            return Incomplete(exception.Message);
        }
    }

    private static PaperPropagationResult Incomplete(string reason) => new("Incomplete", null, [], [], reason);

    private static PaperExpression Initialize(IOperation? operation, string predicate)
    {
        if (predicate.StartsWith("BufferInBounds", StringComparison.Ordinal)
            && operation is IArrayElementReferenceOperation { Indices.Length: 1 } array
            && array.ArrayReference.Type is IArrayTypeSymbol { Rank: 1 })
            return PaperExpression.Bounds(Expression(array.ArrayReference), Expression(array.Indices[0]));

        if (predicate.StartsWith("NotNull", StringComparison.Ordinal))
        {
            var receiver = operation switch
            {
                IPropertyReferenceOperation property => property.Instance,
                IFieldReferenceOperation field => field.Instance,
                IArrayElementReferenceOperation nullArray => nullArray.ArrayReference,
                IInvocationOperation invocation => invocation.Instance,
                _ => null
            };
            if (receiver?.Type?.IsReferenceType == true)
                return PaperExpression.Null(Expression(receiver));
        }

        if (predicate.StartsWith("ValidCast", StringComparison.Ordinal)
            && operation is IConversionOperation { IsImplicit: false, IsTryCast: false } cast
            && cast.Conversion.IsReference && cast.Type?.IsReferenceType == true)
            return PaperExpression.Cast(Expression(cast.Operand), PaperExpression.Constant(cast.Type.ToDisplayString()));

        throw new NotSupportedException("No supported Table 1 template at this exact expression");
    }

    internal static string Identity(ISymbol? symbol)
    {
        if (symbol is not ILocalSymbol and not IParameterSymbol)
            throw new NotSupportedException("Only local and parameter identities are supported");
        if (symbol is ILocalSymbol { RefKind: not RefKind.None } or IParameterSymbol { RefKind: not RefKind.None })
            throw new NotSupportedException("Ref aliases and ref/out parameters are unsupported");
        return $"{symbol.Name}@{symbol.Locations.FirstOrDefault()?.SourceSpan.Start ?? -1}";
    }

    private static string AssignedIdentity(IOperation operation) => operation switch
    {
        ILocalReferenceOperation local => Identity(local.Local),
        IParameterReferenceOperation parameter => Identity(parameter.Parameter),
        _ => throw new NotSupportedException("Heap writes and nonlocal assignments are unsupported")
    };

    internal static PaperExpression Expression(IOperation? operation)
    {
        if (operation == null) throw new NotSupportedException("Missing expression operation");
        if (operation is ILiteralOperation literal && literal.ConstantValue.HasValue)
        {
            var value = literal.ConstantValue.Value;
            return PaperExpression.Constant(value switch
            {
                null => "null",
                bool boolean => boolean ? "true" : "false",
                int integer => integer.ToString(CultureInfo.InvariantCulture),
                string text => SymbolDisplay.FormatLiteral(text, quote: true),
                _ => throw new NotSupportedException("Unsupported literal type")
            });
        }
        return operation switch
        {
            ILocalReferenceOperation local => PaperExpression.Variable(Identity(local.Local)),
            IParameterReferenceOperation parameter => PaperExpression.Variable(Identity(parameter.Parameter)),
            IPropertyReferenceOperation { Property.Name: "Length", Instance.Type: IArrayTypeSymbol { Rank: 1 } } length =>
                PaperExpression.Apply("len", Expression(length.Instance)),
            IParenthesizedOperation parenthesized => Expression(parenthesized.Operand),
            IConversionOperation conversion when conversion.Conversion.IsIdentity => Expression(conversion.Operand),
            IConversionOperation conversion when conversion.IsImplicit && conversion.Conversion.IsReference
                && conversion.OperatorMethod == null && conversion.Operand.Type?.IsReferenceType == true
                && conversion.Type?.IsReferenceType == true => Expression(conversion.Operand),
            IConversionOperation conversion when conversion.IsImplicit && conversion.Operand.ConstantValue is { HasValue: true, Value: null }
                && conversion.Type?.IsReferenceType == true => PaperExpression.Constant("null"),
            IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, OperatorMethod: null } unary
                when unary.Type?.SpecialType == SpecialType.System_Boolean => PaperExpression.Apply("!", Expression(unary.Operand)),
            IBinaryOperation binary when binary.OperatorMethod == null && !binary.IsLifted => Binary(binary),
            _ => throw new NotSupportedException($"Unsupported expression: {operation.Kind}; calls and heap reads require additional semantics")
        };
    }

    private static PaperExpression Binary(IBinaryOperation binary)
    {
        if (binary.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals
            && binary.LeftOperand.Type?.IsReferenceType == true && binary.RightOperand.Type?.IsReferenceType == true)
            return PaperExpression.Apply(binary.OperatorKind == BinaryOperatorKind.Equals ? "==" : "!=",
                Expression(binary.LeftOperand), Expression(binary.RightOperand));
        if (binary.LeftOperand.Type?.SpecialType is not SpecialType.System_Int32 and not SpecialType.System_Boolean
            || binary.RightOperand.Type?.SpecialType != binary.LeftOperand.Type?.SpecialType)
            throw new NotSupportedException("Only int/bool comparisons and boolean connectives are supported");
        var operation = binary.OperatorKind switch
        {
            BinaryOperatorKind.Add when binary.Type?.SpecialType == SpecialType.System_Int32 => binary.IsChecked ? "int32.checked+" : "int32.unchecked+",
            BinaryOperatorKind.Subtract when binary.Type?.SpecialType == SpecialType.System_Int32 => binary.IsChecked ? "int32.checked-" : "int32.unchecked-",
            BinaryOperatorKind.Multiply when binary.Type?.SpecialType == SpecialType.System_Int32 => binary.IsChecked ? "int32.checked*" : "int32.unchecked*",
            BinaryOperatorKind.Equals => "==",
            BinaryOperatorKind.NotEquals => "!=",
            BinaryOperatorKind.LessThan => "<",
            BinaryOperatorKind.LessThanOrEqual => "<=",
            BinaryOperatorKind.GreaterThan => ">",
            BinaryOperatorKind.GreaterThanOrEqual => ">=",
            BinaryOperatorKind.ConditionalAnd => "&&",
            BinaryOperatorKind.ConditionalOr => "||",
            _ => throw new NotSupportedException("Arithmetic/bitwise semantics are not approximated as mathematical integers")
        };
        return PaperExpression.Apply(operation, Expression(binary.LeftOperand), Expression(binary.RightOperand));
    }
}
namespace Sharper.CFGAnalyzer.Analysis;

public sealed record PaperProcedure(string Name, string Entry, string Exit, IReadOnlyList<string> Parameters,
    IReadOnlyDictionary<string, PaperSort> Variables, IReadOnlyList<PaperNode> Nodes, PaperExpression ReturnValue,
    IReadOnlyDictionary<string, PaperExpression> Failures, IReadOnlyDictionary<string, PaperExpression> Invariants);

public sealed record PaperVerificationRequest(IReadOnlyList<PaperNode> Nodes, string Entry,
    IReadOnlyDictionary<string, PaperSort> Variables, PaperExpression Condition,
    IReadOnlyDictionary<string, PaperExpression> Failures, IReadOnlyDictionary<string, PaperExpression> Invariants,
    IReadOnlyList<PaperProcedure> Procedures);

public static class PaperProgramVerifier
{
    public static PaperForwardResult Verify(PaperVerificationRequest request, int maxStates = 1000, bool exploreCycles = false)
    {
        try
        {
            var procedures = request.Procedures.ToDictionary(procedure => procedure.Name, StringComparer.Ordinal);
            var variables = new Dictionary<string, PaperSort>(request.Variables, StringComparer.Ordinal);
            var nodes = new Dictionary<string, PaperNode>(StringComparer.Ordinal);
            var failures = new Dictionary<string, PaperExpression>(StringComparer.Ordinal);
            var invariants = new Dictionary<string, PaperExpression>(StringComparer.Ordinal);
            var sequence = 0;

            PaperExpression Rename(PaperExpression expression, IReadOnlyDictionary<string, string> names) =>
                expression.Operator == "variable"
                    ? PaperExpression.Variable(names[expression.Atom!])
                    : expression with { Operands = [.. expression.Operands.Select(operand => Rename(operand, names))] };

            void Expand(IReadOnlyList<PaperNode> body, Dictionary<string, string> names, Dictionary<string, string> labels,
                IReadOnlyDictionary<string, PaperExpression> obligations, IReadOnlyDictionary<string, PaperExpression> cutpoints,
                HashSet<string> stack)
            {
                foreach (var obligation in obligations) failures.Add(labels[obligation.Key], Rename(obligation.Value, names));
                foreach (var invariant in cutpoints) invariants.Add(labels[invariant.Key], Rename(invariant.Value, names));
                foreach (var node in body)
                {
                    if (nodes.Count >= maxStates) throw new NotSupportedException("Interprocedural expansion budget exceeded");
                    var edges = new List<PaperEdge>();
                    nodes.Add(labels[node.Id], new(labels[node.Id], node.Line, edges));
                    foreach (var edge in node.Successors)
                    {
                        if (edge.Statement is not PaperCall call)
                        {
                            var statement = edge.Statement switch
                            {
                                PaperAssignment assignment => (PaperStatement)new PaperAssignment(names[assignment.Variable], Rename(assignment.Value, names)),
                                PaperGuard guard => new PaperGuard(Rename(guard.Condition, names), guard.Taken),
                                PaperFrame frame => new PaperFrame(frame.Definitions.Select(name => names[name]).ToArray()),
                                PaperUnsupported unsupported => unsupported,
                                _ => throw new NotSupportedException("Unknown statement")
                            };
                            edges.Add(new(labels[edge.Target], statement));
                            continue;
                        }
                        if (!procedures.TryGetValue(call.Procedure, out var callee))
                            throw new NotSupportedException($"Missing body for {call.Procedure}");
                        if (stack.Contains(call.Procedure)) throw new NotSupportedException("Recursive calls require verified recursive contracts");
                        if (call.Arguments.Count != callee.Parameters.Count || callee.Parameters.Distinct().Count() != callee.Parameters.Count)
                            throw new NotSupportedException("Invalid call arity or duplicate parameters");
                        if (!callee.Nodes.Any(item => item.Id == callee.Exit && item.Successors.Count == 0)
                            || callee.Nodes.Any(item => item.Id != callee.Exit && item.Successors.Count == 0))
                            throw new NotSupportedException("Callee must have one explicit normal-return exit");
                        var prefix = $"$call{++sequence}/{callee.Name}/";
                        var calleeNames = callee.Variables.Keys.ToDictionary(name => name, name => prefix + name, StringComparer.Ordinal);
                        foreach (var variable in callee.Variables) variables.Add(calleeNames[variable.Key], variable.Value);
                        var calleeLabels = callee.Nodes.ToDictionary(item => item.Id, item => prefix + item.Id, StringComparer.Ordinal);
                        var first = callee.Parameters.Count == 0 ? calleeLabels[callee.Entry] : prefix + "$argument0";
                        edges.Add(new(first, new PaperFrame([])));
                        for (var argument = 0; argument < callee.Parameters.Count; argument++)
                        {
                            var label = prefix + "$argument" + argument;
                            var next = argument + 1 == callee.Parameters.Count ? calleeLabels[callee.Entry] : prefix + "$argument" + (argument + 1);
                            nodes.Add(label, new(label, node.Line,
                                [new(next, new PaperAssignment(calleeNames[callee.Parameters[argument]], Rename(call.Arguments[argument], names)))]));
                        }
                        Expand(callee.Nodes, calleeNames, calleeLabels, callee.Failures, callee.Invariants,
                            new HashSet<string>(stack, StringComparer.Ordinal) { call.Procedure });
                        var exit = nodes[calleeLabels[callee.Exit]];
                        nodes[exit.Id] = exit with { Successors = [new(labels[edge.Target],
                            new PaperAssignment(names[call.Result], Rename(callee.ReturnValue, calleeNames)))] };
                    }
                }
            }

            Expand(request.Nodes, request.Variables.Keys.ToDictionary(name => name, name => name, StringComparer.Ordinal),
                request.Nodes.ToDictionary(node => node.Id, node => node.Id, StringComparer.Ordinal),
                request.Failures, request.Invariants, new(StringComparer.Ordinal));
            return PaperForwardVerifier.Verify(nodes.Values.ToArray(), request.Entry, failures, request.Condition, variables, maxStates, invariants, exploreCycles);
        }
        catch (Exception exception) when (exception is NotSupportedException or KeyNotFoundException or ArgumentException)
        {
            return new("Incomplete", 0, null, null, exception.Message);
        }
    }
}
using Microsoft.Z3;

namespace Sharper.CFGAnalyzer.Analysis;

public enum PaperSort { Int32, Boolean, Reference }
public sealed record PaperForwardResult(string Status, int CheckedLocations, string? CounterexampleNode, string? Counterexample, string? Reason)
{
    public int? CounterexampleLine { get; init; }
}

public static class PaperForwardVerifier
{
    public static PaperForwardResult Verify(
        IReadOnlyList<PaperNode> nodes, string entry,
        IReadOnlyDictionary<string, PaperExpression> failurePredicates,
        PaperExpression repairCondition, IReadOnlyDictionary<string, PaperSort> variables,
        int maxStates = 1000,
        IReadOnlyDictionary<string, PaperExpression>? invariants = null,
        bool exploreCycles = false)
    {
        var checkedLocations = 0;
        try
        {
            var graph = nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            invariants ??= new Dictionary<string, PaperExpression>();
            if (!graph.ContainsKey(entry) || failurePredicates.Keys.Any(key => !graph.ContainsKey(key))
                || invariants.Keys.Any(key => !graph.ContainsKey(key))
                || graph.Values.Any(node => node.Successors.Any(edge => !graph.ContainsKey(edge.Target))))
                return new("Incomplete", 0, null, null, "Invalid graph or failure locations");
            if (failurePredicates.Count == 0) return new("Incomplete", 0, null, null, "No safety obligations supplied");
            using var context = new Context();
            using var solver = context.MkSolver();
            using var parameters = context.MkParams();
            parameters.Add("timeout", 5000u);
            solver.Parameters = parameters;
            var references = context.MkUninterpretedSort("Reference");
            var length = context.MkFuncDecl("array_length", references, context.MkBitVecSort(32));
            const string heapKey = "$sharper.array-heap";
            if (variables.ContainsKey(heapKey)) throw new NotSupportedException("Reserved model variable: " + heapKey);
            var integerSort = context.MkBitVecSort(32);
            var contentsSort = context.MkArraySort(integerSort, integerSort);
            var axioms = new List<BoolExpr>();
            var initial = variables.ToDictionary(pair => pair.Key, pair => pair.Value switch
            {
                PaperSort.Int32 => context.MkFreshConst(pair.Key, context.MkBitVecSort(32)),
                PaperSort.Boolean => context.MkFreshConst(pair.Key, context.MkBoolSort()),
                PaperSort.Reference => context.MkFreshConst(pair.Key, references),
                _ => throw new NotSupportedException("Unsupported variable sort")
            }, StringComparer.Ordinal);
            initial.Add(heapKey, context.MkFreshConst("array_heap", context.MkArraySort(references, contentsSort)));

            Expr Translate(PaperExpression expression, IReadOnlyDictionary<string, Expr> environment)
            {
                if (expression.Operator == "variable")
                    return environment.TryGetValue(expression.Atom!, out var value) ? value
                        : throw new NotSupportedException($"Missing variable sort: {expression.Atom}");
                if (expression.Operator == "constant")
                {
                    if (expression.Atom == "null") return context.MkConst("null", references);
                    if (bool.TryParse(expression.Atom, out var boolean)) return context.MkBool(boolean);
                    if (int.TryParse(expression.Atom, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out var integer)) return context.MkBV(integer, 32);
                    throw new NotSupportedException("Unsupported constant");
                }
                var left = Translate(expression.Operands[0], environment);
                if (expression.Operator == "!") return context.MkNot((BoolExpr)left);
                if (expression.Operator == "len")
                {
                    var arrayLength = (BitVecExpr)context.MkApp(length, left);
                    axioms.Add(context.MkBVSGE(arrayLength, context.MkBV(0, 32)));
                    return arrayLength;
                }
                if (expression.Operator == "compatible")
                    throw new NotSupportedException("Runtime type compatibility requires a closed type model");
                var right = Translate(expression.Operands[1], environment);
                return expression.Operator switch
                {
                    "array.int32" => context.MkSelect((ArrayExpr)context.MkSelect((ArrayExpr)environment[heapKey], left), right),
                    "&&" => context.MkAnd((BoolExpr)left, (BoolExpr)right),
                    "||" => context.MkOr((BoolExpr)left, (BoolExpr)right),
                    "==" => context.MkEq(left, right),
                    "!=" => context.MkNot(context.MkEq(left, right)),
                    "<" => context.MkBVSLT((BitVecExpr)left, (BitVecExpr)right),
                    "<=" => context.MkBVSLE((BitVecExpr)left, (BitVecExpr)right),
                    ">" => context.MkBVSGT((BitVecExpr)left, (BitVecExpr)right),
                    ">=" => context.MkBVSGE((BitVecExpr)left, (BitVecExpr)right),
                    "int32.unchecked+" => context.MkBVAdd((BitVecExpr)left, (BitVecExpr)right),
                    "int32.unchecked-" => context.MkBVSub((BitVecExpr)left, (BitVecExpr)right),
                    "int32.unchecked*" => context.MkBVMul((BitVecExpr)left, (BitVecExpr)right),
                    _ => throw new NotSupportedException("Unsupported operator or checked-overflow exceptional path")
                };
            }

            Status Check(BoolExpr predicate)
            {
                solver.Reset();
                solver.Assert(axioms.ToArray());
                solver.Assert(predicate);
                return solver.Check();
            }

            var assumption = (BoolExpr)Translate(repairCondition, initial);
            var feasibility = Check(assumption);
            if (feasibility == Status.UNSATISFIABLE) return new("InfeasibleAssumption", 0, null, null, "Repair condition permits no modeled input");
            if (feasibility == Status.UNKNOWN) return new("Incomplete", 0, null, null, solver.ReasonUnknown);
            var pending = new Stack<(string Node, Dictionary<string, Expr> Values, BoolExpr Path, HashSet<string> Visited)>();
            pending.Push((entry, initial, assumption, new(StringComparer.Ordinal)));
            var explored = 0;
            var encounteredCycle = false;
            var expandedCutpoints = new HashSet<string>(StringComparer.Ordinal);
            while (pending.TryPop(out var current))
            {
                if (++explored > maxStates) return new("Incomplete", checkedLocations, null, null, "Forward exploration budget exceeded");
                var feasible = Check(current.Path);
                if (feasible == Status.UNSATISFIABLE) continue;
                if (feasible == Status.UNKNOWN) return new("Incomplete", checkedLocations, null, null, solver.ReasonUnknown);
                if (invariants.TryGetValue(current.Node, out var invariant))
                {
                    var holds = (BoolExpr)Translate(invariant, current.Values);
                    var initiationOrPreservation = Check(context.MkAnd(current.Path, context.MkNot(holds)));
                    if (initiationOrPreservation == Status.SATISFIABLE)
                        return new("InvalidInvariant", checkedLocations, current.Node, solver.Model.ToString(),
                            "Invariant initiation or preservation failed");
                    if (initiationOrPreservation == Status.UNKNOWN)
                        return new("Incomplete", checkedLocations, null, null, solver.ReasonUnknown);
                    if (!expandedCutpoints.Add(current.Node)) continue;
                    var arbitrary = initial.ToDictionary(pair => pair.Key,
                        pair => context.MkFreshConst("cutpoint", pair.Value.Sort), StringComparer.Ordinal);
                    current = (current.Node, arbitrary, (BoolExpr)Translate(invariant, arbitrary), new(StringComparer.Ordinal));
                }
                if (!current.Visited.Add(current.Node))
                {
                    if (!exploreCycles) return new("Incomplete", checkedLocations, null, null, "Feasible cycle requires an invariant or explicit bound");
                    encounteredCycle = true;
                }
                if (failurePredicates.TryGetValue(current.Node, out var failure))
                {
                    var unsafeCondition = (BoolExpr)Translate(failure, current.Values);
                    var status = Check(context.MkAnd(current.Path, unsafeCondition));
                    checkedLocations++;
                    if (status == Status.SATISFIABLE)
                        return new("Counterexample", checkedLocations, current.Node, solver.Model.ToString(), "Repair condition permits a failure")
                        { CounterexampleLine = graph[current.Node].Line };
                    if (status == Status.UNKNOWN) return new("Incomplete", checkedLocations, null, null, solver.ReasonUnknown);
                }
                foreach (var edge in graph[current.Node].Successors)
                {
                    var environment = new Dictionary<string, Expr>(current.Values, StringComparer.Ordinal);
                    var path = current.Path;
                    if (edge.Statement is PaperArrayRead or PaperArrayWrite)
                    {
                        var (array, index) = edge.Statement switch
                        {
                            PaperArrayRead read => (read.Array, read.Index),
                            PaperArrayWrite write => (write.Array, write.Index),
                            _ => throw new NotSupportedException()
                        };
                        var unsafeAccess = PaperExpression.Apply("||", PaperExpression.Null(array), PaperExpression.Bounds(array, index));
                        var status = Check(context.MkAnd(path, (BoolExpr)Translate(unsafeAccess, current.Values)));
                        checkedLocations++;
                        if (status == Status.SATISFIABLE)
                            return new("Counterexample", checkedLocations, current.Node, solver.Model.ToString(), "Array element access can be null or out of bounds")
                            { CounterexampleLine = graph[current.Node].Line };
                        if (status == Status.UNKNOWN) return new("Incomplete", checkedLocations, null, null, solver.ReasonUnknown);
                    }
                    switch (edge.Statement)
                    {
                        case PaperArrayAllocation allocation:
                            if (allocation.Length < 0 || !environment.TryGetValue(allocation.Variable, out var destination)
                                || !destination.Sort.Equals(references))
                                throw new NotSupportedException("Invalid array allocation target or length");
                            var allocated = context.MkFreshConst("allocated_array", references);
                            var distinct = initial.Values.Concat(current.Values.Values).Where(value => value.Sort.Equals(references))
                                .Select(value => context.MkNot(context.MkEq(allocated, value))).ToList();
                            distinct.Add(context.MkNot(context.MkEq(allocated, context.MkConst("null", references))));
                            distinct.Add(context.MkEq(context.MkApp(length, allocated), context.MkBV(allocation.Length, 32)));
                            path = context.MkAnd(path, context.MkAnd(distinct.ToArray()));
                            environment[allocation.Variable] = allocated;
                            environment[heapKey] = context.MkStore((ArrayExpr)current.Values[heapKey], allocated,
                                context.MkConstArray(integerSort, context.MkBV(0, 32)));
                            break;
                        case PaperArrayRead read:
                            if (!environment.TryGetValue(read.Variable, out var readDestination) || !readDestination.Sort.Equals(integerSort))
                                throw new NotSupportedException("Array read target must be Int32");
                            environment[read.Variable] = Translate(PaperExpression.Apply("array.int32", read.Array, read.Index), current.Values);
                            break;
                        case PaperArrayWrite write:
                            var receiver = Translate(write.Array, current.Values);
                            var offset = Translate(write.Index, current.Values);
                            var contents = (ArrayExpr)context.MkSelect((ArrayExpr)current.Values[heapKey], receiver);
                            environment[heapKey] = context.MkStore((ArrayExpr)current.Values[heapKey], receiver,
                                context.MkStore(contents, offset, Translate(write.Value, current.Values)));
                            break;
                        case PaperAssignment assignment:
                            var value = Translate(assignment.Value, current.Values);
                            if (!environment.TryGetValue(assignment.Variable, out var original) || !original.Sort.Equals(value.Sort))
                                throw new NotSupportedException("Assignment variable is missing or has an incompatible sort");
                            environment[assignment.Variable] = value;
                            break;
                        case PaperGuard guard:
                            var condition = (BoolExpr)Translate(guard.Condition, current.Values);
                            path = context.MkAnd(path, guard.Taken ? condition : context.MkNot(condition));
                            break;
                        case PaperFrame { Definitions.Count: 0 }:
                            break;
                        default:
                            throw new NotSupportedException("Unsupported effect in forward graph");
                    }
                    pending.Push((edge.Target, environment, path, new(current.Visited, StringComparer.Ordinal)));
                }
            }
            return new(expandedCutpoints.Count != 0 ? "VerifiedInductiveModel" : encounteredCycle ? "VerifiedExploredModel" : "VerifiedAcyclicModel", checkedLocations, null, null,
                "All supplied safety obligations hold in the model; checked invariants establish partial correctness, not termination or full C# correctness");
        }
        catch (Exception exception) when (exception is NotSupportedException or Z3Exception or InvalidCastException or IndexOutOfRangeException
            or DllNotFoundException or TypeInitializationException)
        {
            return new("Incomplete", checkedLocations, null, null, exception.Message);
        }
    }
}
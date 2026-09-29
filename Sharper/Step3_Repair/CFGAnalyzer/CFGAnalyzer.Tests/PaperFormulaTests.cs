using System.Globalization;
using System.Text.Json;
using Sharper.CFGAnalyzer.Repair;
using Sharper.CFGAnalyzer.Analysis;
using Sharper.Step2Constraints.Models;
using Sharper.Step2Constraints.Rules;
using Sharper.Step2Constraints.Runtime;
using Xunit;

namespace Sharper.CFGAnalyzer.Tests;

public sealed class PaperFormulaTests
{
    [Theory]
    [InlineData(1, "Counterexample")]
    [InlineData(2, "VerifiedExploredModel")]
    public async Task SourceInitializerDoesNotHideLaterLoopFailures(int length, string expected)
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("RepeatedAccess", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var source = "public static class Fixture { public static int Read(int[] array) {\n"
            + "if (array == null || array.Length != " + length + ") return 0;\nint index = 0;\nwhile (index < 2) {\n"
            + "int value = array[index];\nindex++;\n}\nreturn 0;\n} }";
        var result = await PaperSourceAnalysis.AnalyzeAsync(project.AddDocument("Fixture.cs", source),
            new Violation("BufferInBounds(array,index)", "safe", new(), false, "Fixture.cs", 5, "trace"));
        Assert.Equal(expected, result.ForwardModelCheck?.Status);
    }

    [Theory]
    [InlineData("while (index < 3) { index++; }", "VerifiedExploredModel")]
    [InlineData("for (index = 0; index < 3; index++) { }", "VerifiedExploredModel")]
    [InlineData("do { index++; } while (index < 3);", "VerifiedExploredModel")]
    [InlineData("index = Count(3);", "VerifiedExploredModel")]
    [InlineData("index = Count(4);", "Counterexample")]
    public async Task SourceLoopsAndCallsAreLoweredAutomatically(string computation, string expected)
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("SourceLoops", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var source = "public static class Fixture { public static int Read(int[] array) {\n"
            + "if (array == null || array.Length < 4) return 0;\nint index = 0;\n" + computation
            + "\nreturn array[index];\n}\nstatic int Count(int limit) { int count = 0; while (count < limit) { count++; } return count; } }";
        var document = project.AddDocument("Fixture.cs", source);
        var result = await PaperSourceAnalysis.AnalyzeAsync(document,
            new Violation("BufferInBounds(array,index)", "safe", new(), false, "Fixture.cs", 5, "trace"));
        Assert.True(result.Status == "ForwardOnlySupportedSource", result.Reason);
        Assert.Equal(expected, result.ForwardModelCheck?.Status);
    }

    [Theory]
    [InlineData("4", "VerifiedExploredModel")]
    [InlineData("100", "Incomplete")]
    public void ExhaustiveLoopExplorationNeverTreatsBudgetExhaustionAsProof(string limit, string expected)
    {
        var index = PaperExpression.Variable("index");
        var condition = PaperExpression.Apply("<", index, PaperExpression.Constant(limit));
        PaperNode[] nodes = [
            new("entry", 1, [new("head", new PaperAssignment("index", PaperExpression.Constant("0")))]),
            new("head", 2, [new("body", new PaperGuard(condition, true)), new("exit", new PaperGuard(condition, false))]),
            new("body", 3, [new("head", new PaperAssignment("index", PaperExpression.Apply("int32.unchecked+", index, PaperExpression.Constant("1"))))]),
            new("exit", 4, [])];
        var result = PaperForwardVerifier.Verify(nodes, "entry",
            new Dictionary<string, PaperExpression> { ["exit"] = PaperExpression.Apply("!=", index, PaperExpression.Constant(limit)) },
            PaperExpression.Constant("true"), new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 },
            maxStates: 30, exploreCycles: true);
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void SolverInputNamesCannotAliasTheNullLiteral()
    {
        var result = PaperForwardVerifier.Verify([new("entry", 1, [])], "entry",
            new Dictionary<string, PaperExpression> { ["entry"] = PaperExpression.Apply("!=", PaperExpression.Variable("null"), PaperExpression.Constant("null")) },
            PaperExpression.Constant("true"), new Dictionary<string, PaperSort> { ["null"] = PaperSort.Reference });
        Assert.Equal("Counterexample", result.Status);
    }

    [Theory]
    [InlineData("pass", "AcceptedByTests")]
    [InlineData("regression", "Rejected")]
    [InlineData("no-tests", "Rejected")]
    [InlineData("changed-tests", "Rejected")]
    [InlineData("mutated-tests", "Rejected")]
    public async Task AutomaticAcceptanceRequiresStableReproducerAndRegressionResults(string scenario, string expected)
    {
        var directory = Path.Combine(Path.GetTempPath(), "SharperGate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, "Fixture.cs");
            var patch = Path.Combine(directory, "candidate.txt");
            var project = Path.Combine(directory, "Tests.csproj");
            var testSource = Path.Combine(directory, "Tests.cs");
            await File.WriteAllTextAsync(source, "original");
            await File.WriteAllTextAsync(patch, "candidate");
            await File.WriteAllTextAsync(project, "test fixture");
            await File.WriteAllTextAsync(testSource, "original assertions");
            var item = new RepairItem(new("NotNull(value)", "safe", new(), false, "Fixture.cs", 1, "trace"),
                "Read", source, 1, 1, RepairAction.AutoPatchable, "candidate", [], patch, null)
                { SourceSha256 = CandidateValidator.HashFile(source) };
            var calls = 0;
            Task<TestRunEvidence> Run(string sandboxProject, string? filter, string output)
            {
                calls++;
                Assert.NotEqual(project, sandboxProject);
                Assert.Equal(calls <= 2 ? "original" : "candidate", File.ReadAllText(Path.Combine(Path.GetDirectoryName(sandboxProject)!, "Fixture.cs")));
                if (calls <= 2) return Task.FromResult(new TestRunEvidence(1, new() { ["reproducer"] = "Failed" }));
                if (scenario == "mutated-tests") File.WriteAllText(Path.Combine(Path.GetDirectoryName(sandboxProject)!, "Tests.cs"), "weakened assertions");
                if (scenario == "regression") return Task.FromResult(new TestRunEvidence(1, new() { ["reproducer"] = "Failed" }));
                if (scenario == "no-tests") return Task.FromResult(new TestRunEvidence(0, new()));
                return Task.FromResult(new TestRunEvidence(0, new() { [scenario == "changed-tests" ? "different" : "reproducer"] = "Passed" }));
            }
            var result = await CandidateValidator.ValidateAsync(item, directory, project, "reproducer", Path.Combine(directory, "results"), Run);
            Assert.Equal(expected, result.Status);
            Assert.Equal("original", await File.ReadAllTextAsync(source));
            Assert.Equal(4, result.Runs.Count);
            var unvalidated = new RepairPlan("now", "violations.json", 1, new(), [item]);
            Assert.Throws<InvalidOperationException>(() => PatchSynthesizer.ApplyInPlace(unvalidated));
            if (result.Status == "AcceptedByTests")
            {
                var accepted = unvalidated with { Items = [item with { Validation = result }] };
                await File.WriteAllTextAsync(patch, "tampered");
                Assert.Throws<InvalidOperationException>(() => PatchSynthesizer.ApplyInPlace(accepted));
                await File.WriteAllTextAsync(patch, "candidate");
                Assert.Equal(1, PatchSynthesizer.ApplyInPlace(accepted));
                Assert.Equal("candidate", await File.ReadAllTextAsync(source));
                Assert.Equal(1, PatchSynthesizer.RestoreFromBackups(accepted));
                Assert.Equal("original", await File.ReadAllTextAsync(source));
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void CorrectedObservedActivationIsExplicitlyDifferentFromLiteralRoot()
    {
        var predicate = PaperExpression.Apply(">=", PaperExpression.Variable("index"), PaperExpression.Constant("5"));
        var statement = new PaperAssignment("index", PaperExpression.Apply("int32.unchecked+", PaperExpression.Variable("index"), PaperExpression.Constant("1")));
        var before = new Dictionary<string, object?> { ["index"] = 4 };
        var after = new Dictionary<string, object?> { ["index"] = 5 };
        var observed = PaperEvaluator.EvaluateObservedActivation(true, statement, predicate, before, after);
        Assert.True(observed.IsCandidate);
        Assert.Contains("not literal Root", observed.Reason);
        Assert.False(PaperEvaluator.EvaluateRoot(true, statement, PaperRuleEngine.Transfer(statement, predicate).Predicate, predicate, before, after).IsCandidate);
        Assert.False(PaperEvaluator.EvaluateObservedActivation(false, statement, predicate, before, after).IsCandidate);
        Assert.Equal("Unknown", PaperEvaluator.EvaluateObservedActivation(true, statement, predicate, new Dictionary<string, object?>(), after).Status);
        after["index"] = 6;
        Assert.Equal("InvalidTransition", PaperEvaluator.EvaluateObservedActivation(true, statement, predicate, before, after).Status);
    }

    [Theory]
    [InlineData(false, "VerifiedAcyclicModel")]
    [InlineData(true, "Counterexample")]
    public void InterproceduralVerificationChecksCalleeAndIsolatesCallerLocals(bool failingCallee, string expected)
    {
        var index = PaperExpression.Variable("index");
        var procedure = new PaperProcedure("Increment", "start", "return", ["index"],
            new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 },
            [new("start", 1, [new("return", new PaperAssignment("index", PaperExpression.Apply("int32.unchecked+", index, PaperExpression.Constant("1"))))]),
             new("return", 2, [])], index,
            new Dictionary<string, PaperExpression> { ["return"] = PaperExpression.Constant(failingCallee ? "true" : "false") },
            new Dictionary<string, PaperExpression>());
        var request = new PaperVerificationRequest(
            [new("entry", 1, [new("exit", new PaperCall("Increment", "result", [index]))]), new("exit", 2, [])], "entry",
            new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32, ["result"] = PaperSort.Int32 },
            PaperExpression.Apply("==", index, PaperExpression.Constant("4")),
            new Dictionary<string, PaperExpression> { ["exit"] = PaperExpression.Apply("||",
                PaperExpression.Apply("!=", index, PaperExpression.Constant("4")),
                PaperExpression.Apply("!=", PaperExpression.Variable("result"), PaperExpression.Constant("5"))) },
            new Dictionary<string, PaperExpression>(), [procedure]);
        Assert.Equal(expected, PaperProgramVerifier.Verify(request).Status);
        Assert.Equal("Incomplete", PaperProgramVerifier.Verify(request with { Procedures = [] }).Status);
        var json = JsonSerializer.Serialize(request);
        Assert.Equal(expected, PaperProgramVerifier.Verify(JsonSerializer.Deserialize<PaperVerificationRequest>(json)!).Status);
        var recursive = procedure with { Nodes = [new("start", 1, [new("return", new PaperCall("Increment", "index", [index]))]), new("return", 2, [])] };
        Assert.Equal("Incomplete", PaperProgramVerifier.Verify(request with { Procedures = [recursive] }).Status);
    }

    [Theory]
    [InlineData("10", "VerifiedInductiveModel")]
    [InlineData("9", "InvalidInvariant")]
    public void LoopInvariantsRequireInitiationAndPreservation(string upperBound, string expected)
    {
        var index = PaperExpression.Variable("index");
        var condition = PaperExpression.Apply("<", index, PaperExpression.Constant("10"));
        var graph = new PaperNode[]
        {
            new("entry", 1, [new("head", new PaperAssignment("index", PaperExpression.Constant("0")))]),
            new("head", 2, [new("body", new PaperGuard(condition, true)), new("exit", new PaperGuard(condition, false))]),
            new("body", 3, [new("head", new PaperAssignment("index", PaperExpression.Apply("int32.unchecked+", index, PaperExpression.Constant("1"))))]),
            new("exit", 4, [])
        };
        var invariant = PaperExpression.Apply("&&", PaperExpression.Apply(">=", index, PaperExpression.Constant("0")),
            PaperExpression.Apply("<=", index, PaperExpression.Constant(upperBound)));
        var obligations = new Dictionary<string, PaperExpression>
        {
            ["body"] = PaperExpression.Apply("||", PaperExpression.Apply("<", index, PaperExpression.Constant("0")),
                PaperExpression.Apply(">=", index, PaperExpression.Constant("10"))),
            ["exit"] = PaperExpression.Apply("!=", index, PaperExpression.Constant("10"))
        };
        var result = PaperForwardVerifier.Verify(graph, "entry", obligations, PaperExpression.Constant("true"),
            new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 },
            invariants: new Dictionary<string, PaperExpression> { ["head"] = invariant });
        Assert.Equal(expected, result.Status);
        if (expected == "VerifiedInductiveModel") Assert.True(result.CheckedLocations >= 2);
        else Assert.NotNull(result.Counterexample);
        var falseInvariant = PaperForwardVerifier.Verify(graph, "entry", obligations, PaperExpression.Constant("true"),
            new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 },
            invariants: new Dictionary<string, PaperExpression> { ["head"] = PaperExpression.Constant("false") });
        Assert.Equal("InvalidInvariant", falseInvariant.Status);
    }

    [Theory]
    [InlineData(".sharper.bak")]
    [InlineData(".autorepair.bak")]
    public void RestorePreservesCompatibilityWithExistingRepairBackups(string suffix)
    {
        var directory = Path.Combine(Path.GetTempPath(), "SharperRestore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var source = Path.Combine(directory, "Fixture.cs");
            File.WriteAllText(source, "candidate");
            File.WriteAllText(source + suffix, "original");
            var violation = new Violation("NotNull(value)", "safe", new(), false, "Fixture.cs", 1, "trace");
            var item = new RepairItem(violation, "Read", source, 1, 1, RepairAction.AutoPatchable, "candidate", [], null, null);
            var plan = new RepairPlan("now", "violations.json", 1, new(), [item]);
            Assert.Equal(1, PatchSynthesizer.RestoreFromBackups(plan));
            Assert.Equal("original", File.ReadAllText(source));
            Assert.False(File.Exists(source + suffix));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("int32.unchecked+", "Counterexample")]
    [InlineData("int32.checked+", "Incomplete")]
    public void ForwardSolverPreservesInt32AssignmentBoundaries(string operation, string expected)
    {
        var index = PaperExpression.Variable("index");
        var graph = new PaperNode[]
        {
            new("entry", 1, [new("access", new PaperAssignment("index", PaperExpression.Apply(operation, index, PaperExpression.Constant("1"))))]),
            new("access", 2, [])
        };
        var result = PaperForwardVerifier.Verify(graph, "entry", new Dictionary<string, PaperExpression>
        { ["access"] = PaperExpression.Apply("<", index, PaperExpression.Constant("0")) },
            PaperExpression.Apply("==", index, PaperExpression.Constant("2147483647")),
            new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 });
        Assert.Equal(expected, result.Status);
    }

    [Theory]
    [InlineData("for (; count < 10005; count++) { }")]
    [InlineData("while (count < 10005) { count++; }")]
    [InlineData("do { count++; } while (count < 10005);")]
    public void InstrumentationDoesNotTerminateFiniteLoopsAtDiagnosticThreshold(string loop)
    {
        var source = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            "public class Fixture { public static int Run() { int count = 0; checked { " + loop + " } return count; } }");
        var rewritten = new Instrumenter.Rewriters.SafetyRewriter { TargetFileName = "Fixture.cs" }.Visit(source.GetRoot());
        var helper = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            "namespace SharperTracing { public static class TraceLogger { public static void Log(params object[] values) { } } }");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("LoopFixture" + Guid.NewGuid().ToString("N"),
            new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(rewritten!.ToFullString()), helper },
            new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var assembly = System.Reflection.Assembly.Load(output.ToArray());
        Assert.Equal(10005, assembly.GetType("Fixture")!.GetMethod("Run")!.Invoke(null, null));
    }

    [Fact]
    public void Figure3_ForwardSolverFindsLaterBufferFailureAndChecksStrongerCondition()
    {
        var index = PaperExpression.Variable("index");
        var lower = PaperExpression.Apply(">=", index, PaperExpression.Constant("0"));
        var firstSafety = PaperExpression.Apply("&&", lower, PaperExpression.Apply("<", index, PaperExpression.Constant("5")));
        var secondSafety = PaperExpression.Apply("&&", lower, PaperExpression.Apply("<", index, PaperExpression.Constant("2")));
        var graph = new PaperNode[]
        {
            new("first", 1, [new("second", new PaperFrame([]))]),
            new("second", 2, [])
        };
        var obligations = new Dictionary<string, PaperExpression>
        {
            ["first"] = PaperExpression.Apply("!", firstSafety),
            ["second"] = PaperExpression.Apply("!", secondSafety)
        };
        var variables = new Dictionary<string, PaperSort> { ["index"] = PaperSort.Int32 };
        var counterexample = PaperForwardVerifier.Verify(graph, "first", obligations, firstSafety, variables);
        Assert.Equal("Counterexample", counterexample.Status);
        Assert.Equal("second", counterexample.CounterexampleNode);
        Assert.NotEmpty(counterexample.Counterexample!);
        Assert.Equal("VerifiedAcyclicModel", PaperForwardVerifier.Verify(graph, "first", obligations, secondSafety, variables).Status);
        Assert.Equal("InfeasibleAssumption", PaperForwardVerifier.Verify(graph, "first", obligations, PaperExpression.Constant("false"), variables).Status);
    }

    [Fact]
    public void ForwardSolverDoesNotCertifyFeasibleCyclesOrUnknownEffects()
    {
        var graph = new PaperNode[] { new("loop", 1, [new("loop", new PaperFrame([]))]) };
        var result = PaperForwardVerifier.Verify(graph, "loop", new Dictionary<string, PaperExpression>
        { ["loop"] = PaperExpression.Constant("false") }, PaperExpression.Constant("true"), new Dictionary<string, PaperSort>());
        Assert.Equal("Incomplete", result.Status);
        Assert.Contains("cycle", result.Reason);
    }

    [Fact]
    public void Figure2_LiteralRootEvaluatesBothPredicatesAndChecksTheTransition()
    {
        var post = PaperExpression.Apply(">=", PaperExpression.Variable("index"), PaperExpression.Constant("5"));
        var statement = new PaperAssignment("index", PaperExpression.Apply("int32.unchecked+", PaperExpression.Variable("index"), PaperExpression.Constant("1")));
        var pre = PaperRuleEngine.Transfer(statement, post).Predicate;
        var before = new Dictionary<string, object?> { ["index"] = 4 };
        var after = new Dictionary<string, object?> { ["index"] = 5 };
        var result = PaperEvaluator.EvaluateRoot(true, statement, pre, post, before, after);
        Assert.Equal("Evaluated", result.Status);
        Assert.False(result.IsCandidate);
        after["index"] = 6;
        Assert.Equal("InvalidTransition", PaperEvaluator.EvaluateRoot(true, statement, pre, post, before, after).Status);
    }

    [Fact]
    public void Figure2_MissingObservationsDoNotBecomeRootCandidates()
    {
        var predicate = PaperExpression.Null(PaperExpression.Variable("value"));
        var result = PaperEvaluator.EvaluateRoot(true, new PaperFrame([]), predicate, predicate, new Dictionary<string, object?>(), new Dictionary<string, object?>());
        Assert.Equal("Unknown", result.Status);
        Assert.Null(result.IsCandidate);
    }

    [Fact]
    public void ArithmeticEvaluationPreservesCheckedAndUncheckedInt32Semantics()
    {
        var maximum = PaperExpression.Constant(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var one = PaperExpression.Constant("1");
        var state = new Dictionary<string, object?>();
        Assert.Equal(int.MinValue, PaperEvaluator.Evaluate(PaperExpression.Apply("int32.unchecked+", maximum, one), state).Value);
        Assert.False(PaperEvaluator.Evaluate(PaperExpression.Apply("int32.checked+", maximum, one), state).Known);
    }

    [Theory]
    [InlineData("index = next;", "PropagatedSupportedSubset")]
    [InlineData("index = index + 1;", "PropagatedSupportedSubset")]
    [InlineData("while (index < next) { index++; }", "Incomplete")]
    [InlineData("System.Console.WriteLine(index);", "Incomplete")]
    [InlineData("array[0] = next;", "Incomplete")]
    public async Task Figure2_SourceAdapterRejectsUnsupportedEffects(string preceding, string expected)
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("SubsetFixture", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Subset.cs", $"class Fixture {{ static int Read(int[] array, int index, int next) {{\n{preceding}\nreturn array[index];\n}} }}");
        var violation = new Violation("BufferInBounds(index:array, array)", "in bounds", new(), false, "Subset.cs", 3, "trace");
        var result = await PaperSourceAnalysis.AnalyzeAsync(document, violation);
        Assert.Equal(expected, result.Status);
        if (expected == "Incomplete") Assert.Null(result.EntryPredicate);
        Assert.Equal(expected == "Incomplete" ? "NotPerformed" : "VerifiedAcyclicModel", result.ForwardVerificationStatus);
    }

    [Theory]
    [InlineData("string value", "value.Length", "NotNull(value)", "== null")]
    [InlineData("object value", "(string)value", "ValidCast(value, string)", "compatible")]
    public async Task Table1_SourceAdapterInitializesNullAndReferenceCast(string parameter, string expression, string predicate, string expected)
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("TemplateFixture", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var returnType = predicate.StartsWith("NotNull", StringComparison.Ordinal) ? "int" : "string";
        var document = project.AddDocument("Template.cs", $"class Fixture {{ static {returnType} Read({parameter}) {{\nreturn {expression};\n}} }}");
        var result = await PaperSourceAnalysis.AnalyzeAsync(document,
            new Violation(predicate, "safe", new(), false, "Template.cs", 2, "trace"));
        Assert.Equal("PropagatedSupportedSubset", result.Status);
        Assert.Contains(expected, result.EntryPredicate);
        Assert.Contains(result.Steps, step => step.Rule == "Initialize");
    }

    [Fact]
    public async Task Figure2_RepairDriverPropagatesBothCSharpBranches()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("FormulaIntegration", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Fixture.cs", """
            class Fixture
            {
                static int Read(int[] array, int index, int next, bool condition)
                {
                    if (condition)
                        index = next;
                    else
                        index = 0;
                    return array[index];
                }
            }
            """, filePath: Path.Combine(Path.GetTempPath(), "Fixture.cs"));
        var output = Path.Combine(Path.GetTempPath(), "SharperFormulaIntegration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            var violation = new Violation("BufferInBounds(index:array, array)", "in bounds", new(), false, "Fixture.cs", 9, "trace-1");
            var violationsPath = Path.Combine(output, "violations.json");
            await File.WriteAllTextAsync(violationsPath, JsonSerializer.Serialize(new[] { violation }));
            var plan = await ViolationDrivenRepair.RunAsync(document.Project.Solution, violationsPath, output);
            var item = Assert.Single(plan.Items);
            Assert.NotNull(item.FormulaAnalysis);
            Assert.Equal("PropagatedSupportedSubset", item.FormulaAnalysis.Status);
            Assert.Contains("next@", item.FormulaAnalysis.EntryPredicate);
            Assert.DoesNotContain("index@", item.FormulaAnalysis.EntryPredicate);
            Assert.Contains(item.FormulaAnalysis.Steps, step => step.Rule == "Join");
            Assert.Equal("VerifiedAcyclicModel", item.FormulaAnalysis.ForwardVerificationStatus);
            Assert.NotNull(item.FormulaAnalysis.ProposedEntryCondition);
            Assert.Contains("not the generated patch", item.FormulaAnalysis.ForwardVerificationScope);
            Assert.Equal("NotPerformed", plan.VerificationStatus);
            Assert.NotNull(item.PatchedFilePath);
            Assert.Contains("Unverified predicate-guard", item.ActionReason);
            var candidate = await File.ReadAllTextAsync(item.PatchedFilePath);
            Assert.Contains("array is null || index < 0 || index >= array.Length", candidate);
            Assert.DoesNotContain("catch", candidate);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("GuardFixture" + Guid.NewGuid().ToString("N"),
                [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(candidate)],
                [Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
            using var binary = new MemoryStream();
            var emitted = compilation.Emit(binary);
            Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
            var method = System.Reflection.Assembly.Load(binary.ToArray()).GetType("Fixture")!.GetMethod("Read",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            Assert.Equal(20, method.Invoke(null, [new[] { 10, 20 }, 0, 1, true]));
            Assert.Equal(10, method.Invoke(null, [new[] { 10, 20 }, 0, 99, false]));
            Assert.Equal(0, method.Invoke(null, [new[] { 10, 20 }, 0, 2, true]));
            Assert.Equal(0, method.Invoke(null, [Array.Empty<int>(), 0, -1, true]));
            Assert.Equal(0, method.Invoke(null, [null, 0, 0, true]));
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "repair_plan.json")));
            Assert.Equal("PropagatedSupportedSubset", json.RootElement.GetProperty("Items")[0]
                .GetProperty("FormulaAnalysis").GetProperty("Status").GetString());
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Figure2_AssignSubstitutesSymbolIdentityWithoutCapturingOtherVariables()
    {
        var predicate = PaperExpression.Apply("==", PaperExpression.Variable("index@10"), PaperExpression.Variable("index@20"));
        var (actual, rule) = PaperRuleEngine.Transfer(
            new PaperAssignment("index@10", PaperExpression.Variable("next@30")), predicate);
        Assert.Equal("Assign", rule);
        Assert.Equal("(next@30 == index@20)", actual.ToString());
        Assert.DoesNotContain("index@10", actual.FreeVariables());
    }

    [Fact]
    public void Figure2_AssignIrrelAndFrameLeavePredicateUnchanged()
    {
        var predicate = PaperExpression.Null(PaperExpression.Variable("value"));
        Assert.Equal((predicate, "Assign-Irrel"), PaperRuleEngine.Transfer(new PaperAssignment("other", PaperExpression.Constant("0")), predicate));
        Assert.Equal((predicate, "Frame"), PaperRuleEngine.Transfer(new PaperFrame(["other"]), predicate));
        Assert.Throws<NotSupportedException>(() => PaperRuleEngine.Transfer(new PaperFrame(["value"]), predicate));
    }

    [Fact]
    public void Figure2_GuardsJoinAndInitializeAreAppliedAlongBothPaths()
    {
        var condition = PaperExpression.Variable("condition");
        var nodes = new PaperNode[]
        {
            new("entry", 1, [new("yes", new PaperGuard(condition, true)), new("no", new PaperGuard(condition, false))]),
            new("yes", 2, [new("failure", new PaperAssignment("value", PaperExpression.Constant("null")))]),
            new("no", 3, [new("failure", new PaperFrame([]))]),
            new("failure", 4, [])
        };
        var result = PaperRuleEngine.Propagate(nodes, "entry", "failure", PaperExpression.Null(PaperExpression.Variable("value")));
        Assert.Equal("PropagatedSupportedSubset", result.Status);
        Assert.Equal("((condition && (null == null)) || (!(condition) && (value == null)))", result.EntryPredicate);
        foreach (var rule in new[] { "Initialize", "Assign", "Frame", "Guard-T", "Guard-F", "Join" })
            Assert.Contains(result.Steps, step => step.Rule == rule);
        Assert.Equal("BlockedByPrintedRootDefinition", result.RootStatus);
        Assert.Equal("NotPerformed", result.ForwardVerificationStatus);
    }

    [Fact]
    public void Figure2_CyclesAndUnknownEffectsAreIncompleteNotVerified()
    {
        var nodes = new PaperNode[]
        {
            new("entry", 1, [new("entry", new PaperFrame([])), new("failure", new PaperFrame([]))]),
            new("failure", 2, [])
        };
        var result = PaperRuleEngine.Propagate(nodes, "entry", "failure", PaperExpression.Constant("true"));
        Assert.Equal("Incomplete", result.Status);
        Assert.Null(result.EntryPredicate);
        Assert.Throws<NotSupportedException>(() => PaperRuleEngine.Transfer(new PaperUnsupported("call"), PaperExpression.Constant("true")));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 4)]
    [InlineData(5, 5)]
    [InlineData(5, int.MinValue)]
    [InlineData(5, int.MaxValue)]
    public void Table1_BoundsMatchesFailurePredicate(int length, int index)
    {
        var state = new StateTracker();
        state.Update(Event($"Heap State: buffer (int[] size={length})"));
        var access = Event("Variable Update", "index:buffer", index.ToString(CultureInfo.InvariantCulture));
        state.Update(access);

        var detected = new BufferBoundsRule().TryEvaluate(access, state, out var violation);

        Assert.Equal(index < 0 || index >= length, detected);
        if (detected)
        {
            Assert.False(violation.Result);
            Assert.Equal(length, violation.Bindings["heap.size(buffer)"]);
        }
    }

    [Fact]
    public void Table1_UnknownLengthDoesNotMeanEmptyArray()
    {
        var state = new StateTracker();
        state.Update(Event("Heap State: buffer (int[])"));
        Assert.False(new BufferBoundsRule().TryEvaluate(
            Event("Variable Update", "index:buffer", "0"), state, out _));
    }

    [Fact]
    public void Table1_EmptySnapshotHasKnownZeroLength()
    {
        var state = new StateTracker();
        state.Update(Event("Array Snapshot", "buffer", "[]"));
        Assert.True(new BufferBoundsRule().TryEvaluate(
            Event("Variable Update", "index:buffer", "0"), state, out _));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Figure2_ExactAssignmentPreimageCannotSatisfyRootPremises(int before)
    {
        var after = before + 1;
        var predicateAfter = after >= 5;
        var substitutedPredicateBefore = before + 1 >= 5;

        Assert.Equal(predicateAfter, substitutedPredicateBefore);
        Assert.False(!substitutedPredicateBefore && predicateAfter);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("value", false)]
    public void Table1_JsonNullReceiverMatchesFailurePredicate(string? value, bool expectedFailure)
    {
        var state = new StateTracker();
        state.Update(Event("Variable Update", "receiver", value));
        var access = Event("Variable Update", "receiver.Length", "0");

        Assert.Equal(expectedFailure, new NullDereferenceRule().TryEvaluate(access, state, out _));
    }

    [Fact]
    public void Table1_NonNullAssignmentClearsNullState()
    {
        var state = new StateTracker();
        state.Update(Event("Variable Update", "receiver", null));
        Assert.True(state.IsNullTracked("receiver"));
        state.Update(Event("Variable Update", "receiver", ""));
        Assert.False(state.IsNullTracked("receiver"));
    }

    [Theory]
    [InlineData("null", "System.String", false)]
    [InlineData("System.String", "System.String", false)]
    [InlineData("System.Int32", "System.String", true)]
    public void Table1_CastSupportedCases(string sourceType, string targetType, bool expectedFailure)
    {
        var cast = Event("Cast Attempt", $"cast:{targetType}", sourceType);
        Assert.Equal(expectedFailure, new InvalidCastingRule().TryEvaluate(cast, new StateTracker(), out _));
    }

    private static TraceEvent Event(string message, string? name = null, string? value = null) =>
        new("event-1", "2026-09-29T00:00:00Z", 1, "FormulaFixture.cs", 10, message,
            name == null ? null : new TraceVariablePayload(name, value));

    [Fact]
    public void Section23_RepairPlanDoesNotClaimVerification()
    {
        var plan = new RepairPlan("2026-09-29", "violations.json", 0, new(), new());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(plan));
        Assert.Equal("NotPerformed", json.RootElement.GetProperty("VerificationStatus").GetString());
    }

    [Fact]
    public async Task Section23_GeneratedCandidateDoesNotClaimForwardVerification()
    {
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var project = workspace.AddProject("FormulaFixture", Microsoft.CodeAnalysis.LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "Fixture.cs", Microsoft.CodeAnalysis.Text.SourceText.From(
            "class Fixture { public static int Read(string value) { return value.Length; } }"));
        var root = await document.GetSyntaxRootAsync();
        var statement = Assert.Single(root!.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax>());
        var output = Path.Combine(Path.GetTempPath(), "SharperFormula-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try
        {
            var (patchedFile, _) = await PatchSynthesizer.WritePatchAsync(
                document, statement, "System.NullReferenceException", output);
            var candidate = await File.ReadAllTextAsync(patchedFile);
            Assert.Contains("Unverified catch-based candidate", candidate);
            Assert.DoesNotContain("Forward-propagated", candidate);
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Section21_AsCastOfNullableSourceDoesNotIntroduceCompilerWarnings()
    {
        var source = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("""
            #nullable enable
            public class Fixture
            {
                public static string? Convert(object? value) => value as string;
            }
            """);
        var rewritten = new Instrumenter.Rewriters.CastingRewriter { TargetFileName = "Fixture.cs" }
            .Visit(source.GetRoot());
        var helper = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("""
            #nullable disable
            namespace SharperTracing
            {
                public static class TraceLogger
                {
                    public static T LogAsCast<T>(string id, string file, int line, object value, string type)
                        where T : class => value as T;
                }
            }
            """);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("NullableCastFixture",
            new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(rewritten!.ToFullString()), helper },
            new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity is
            Microsoft.CodeAnalysis.DiagnosticSeverity.Warning or Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }
}
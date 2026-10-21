using System.Runtime.Loader;
using Instrumenter;
using Instrumenter.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Sharper.CFGAnalyzer.Tests;

public class VariableTracingTests
{
    [Theory]
    [InlineData("var values = new int[2]; int i = 0; values[i++] = 10; values[i++] = 20; return $\"{i}:{values[0]}:{values[1]}\";", "2:10:20")]
    [InlineData("var values = new int[2]; int i = -1; values[++i] = 10; values[++i] = 20; return $\"{i}:{values[0]}:{values[1]}\";", "1:10:20")]
    [InlineData("var values = new int[2]; int i = 1; values[i--] = 10; values[i--] = 20; return $\"{i}:{values[0]}:{values[1]}\";", "-1:20:10")]
    [InlineData("var values = new int[2]; int i = 0; values[i++] += 10; return $\"{i}:{values[0]}\";", "1:10")]
    [InlineData("var values = new int[2]; int calls = 0; int Next() => calls++; values[Next()] = 10; return $\"{calls}:{values[0]}\";", "1:10")]
    [InlineData("var values = new int[2]; int calls = 0; int[] Get() { calls++; return values; } Get()[0] = 10; return $\"{calls}:{values[0]}\";", "1:10")]
    [InlineData("var values = new int[2]; int i = 2; try { values[i++] = 10; } catch (System.IndexOutOfRangeException) { } return i.ToString();", "3")]
    [InlineData("var values = new int[2]; int i = 0; bool skip = true; if (!skip && values[i++] == 0) { } return i.ToString();", "0")]
    [InlineData("var values = new int[2]; int i = 0; int calls = 0; int Value() { calls++; return i; } values[i++] = Value(); return $\"{i}:{calls}:{values[0]}\";", "1:1:1")]
    [InlineData("var values = new int[2,2]; int i = 0; int j = 0; values[i++,j++] = 10; return $\"{i}:{j}:{values[0,0]}\";", "1:1:10")]
    [InlineData("int i = 2; int value = 12 / i++; return $\"{i}:{value}\";", "3:6")]
    public void LoggingPreservesEvaluationCountAndOrder(string body, string expected)
    {
        var source = $"public static class Fixture {{ public static string Run() {{ {body} }} }}";
        Assert.Equal(expected, Execute(source, instrument: false).Result);
        Assert.Equal(expected, Execute(source, instrument: true).Result);
    }

    [Fact]
    public void ConstantContextsStillCompileAfterInstrumentation()
    {
        const string source = """
            public static class Fixture
            {
                private const int Half = 10 / 2;
                private static int Scale(int value, int divisor = 8 / 4) => value / divisor;
                public static string Run()
                {
                    const int Quarter = 20 / 4;
                    var values = new int[] { 1, 2, 3 };
                    int i = 4;
                    switch (i)
                    {
                        case 8 / 2: return $"{Half}:{Quarter}:{Scale(values[1] * 6)}:{i / 2}";
                        default: return "none";
                    }
                }
            }
            """;
        Assert.Equal("5:5:6:2", Execute(source, instrument: false).Result);
        var (result, events) = Execute(source, instrument: true);
        Assert.Equal("5:5:6:2", result);
        Assert.Contains("index:values=1", events);
        Assert.Contains("denominator=2", events);
    }

    [Fact]
    public void IndexEventsUseTheActualAccessedIndex()
    {
        const string source = """
            public static class Fixture
            {
                public static string Run()
                {
                    var values = new int[2];
                    int i = 0;
                    values[i++] = 10;
                    values[i++] = 20;
                    return i.ToString();
                }
            }
            """;
        var (result, events) = Execute(source, instrument: true);
        Assert.Equal("2", result);
        Assert.Equal(new[] { "index:values=0", "index:values=1" },
            events.Where(entry => entry.StartsWith("index:")));
    }

    [Fact]
    public void IndexEventsKeepOriginalLinesAfterEarlierRewrites()
    {
        const string source = "class Fixture { void Run(int[] values, int i) {\nvalues[i++] = 10;\n} }";
        var root = LineNumberAnnotator.Annotate(CSharpSyntaxTree.ParseText(source).GetRoot());
        var shifted = root.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia("\n\n\n\n"));
        var rewritten = new VariableRewriter { TargetFileName = "Fixture.cs" }.Visit(shifted)!;
        var log = Assert.Single(rewritten.DescendantNodes().OfType<InvocationExpressionSyntax>(),
            call => call.ArgumentList.Arguments.LastOrDefault()?.Expression is LiteralExpressionSyntax literal
                && literal.Token.ValueText == "index:values");
        Assert.Equal("2", log.ArgumentList.Arguments[2].Expression.ToString());
    }

    private static (string Result, string[] Events) Execute(string source, bool instrument)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        if (instrument)
            root = new VariableRewriter { TargetFileName = "Fixture.cs" }.Visit(LineNumberAnnotator.Annotate(root))!;
        const string logger = """
            namespace SharperTracing
            {
                public static class TraceLogger
                {
                    public static readonly System.Collections.Generic.List<string> Events = new();
                    public static T LogValue<T>(string id, string file, int line, string message, T value, string name)
                    {
                        Events.Add(name + "=" + value);
                        return value;
                    }
                    public static void Log(string id, string file, int line, string message, object value, string name) { }
                    public static void LogArray(string id, string file, int line, string message, object value, string name) { }
                    public static string[] ReadEvents() => Events.ToArray();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("TracingFixture" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(root.ToFullString()), CSharpSyntaxTree.ParseText(logger) },
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext(compilation.AssemblyName!, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var result = (string)assembly.GetType("Fixture")!.GetMethod("Run")!.Invoke(null, null)!;
            var events = (string[])assembly.GetType("SharperTracing.TraceLogger")!.GetMethod("ReadEvents")!.Invoke(null, null)!;
            return (result, events);
        }
        finally { context.Unload(); }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Instrumenter.Rewriters;

namespace Instrumenter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Instrumenter <ProjectRootPath>");
                Console.WriteLine("  Instrumenter --test-project <TestProject.csproj>");
                Console.WriteLine("  Instrumenter --diagnose <file.cs>");
                return;
            }

            if (args[0] == "--diagnose")
            {
                if (args.Length < 2 || !File.Exists(args[1]))
                {
                    Console.WriteLine("Error: --diagnose requires a valid .cs file path.");
                    return;
                }
                DiagnoseFile(Path.GetFullPath(args[1]));
                return;
            }

            List<string> sourceDirectories;
            string rootPath;

            if (args[0] == "--test-project")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: --test-project requires a .csproj path.");
                    return;
                }

                string testProject = Path.GetFullPath(args[1]);
                if (!File.Exists(testProject))
                {
                    Console.WriteLine($"Error: Test project not found at {testProject}!");
                    return;
                }

                Console.WriteLine($"Resolving project graph from: {testProject}");
                sourceDirectories = ProjectGraphResolver.ResolveSourceDirectories(testProject);

                if (sourceDirectories.Count == 0)
                {
                    Console.WriteLine("Error: No source project references found in the test project.");
                    return;
                }

                // Use the first source directory as rootPath for TraceLogger placement
                // and relative-path computation.
                rootPath = sourceDirectories[0];
            }
            else
            {
                rootPath = Path.GetFullPath(args[0]);
                if (!Directory.Exists(rootPath))
                {
                    Console.WriteLine($"Error: Directory not found at {rootPath}!");
                    return;
                }
                sourceDirectories = new List<string> { rootPath };
            }

            Console.WriteLine($"Instrumenting project at: {rootPath}");

            // 1. Generate TraceLogger in each source directory
            foreach (var dir in sourceDirectories)
            {
                GenerateTraceLogger(dir);
            }

            // 2. Find files across all source directories
            var files = new List<string>();
            foreach (var dir in sourceDirectories)
            {
                var dirFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                             && !f.EndsWith("TraceLogger.cs")
                             && !f.EndsWith(".g.cs")
                             && !f.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase));
                files.AddRange(dirFiles);
            }

            Console.WriteLine($"Found {files.Count} C# files to instrument.");

            // 3. Instrument files in parallel for large repos
            int succeeded = 0;
            int failed = 0;
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                file =>
                {
                    bool ok = InstrumentFile(file, rootPath);
                    if (ok)
                        Interlocked.Increment(ref succeeded);
                    else
                        Interlocked.Increment(ref failed);
                });

            Console.WriteLine($"Instrumentation Complete. Succeeded: {succeeded}, Failed: {failed}");
        }

        static void DiagnoseFile(string filePath)
        {
            string code = File.ReadAllText(filePath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            int originalErrors = tree.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
            Console.WriteLine($"Original parse errors: {originalErrors}");

            string relPath = Path.GetFileName(filePath);

            // Step 0: Annotate
            var annotated = LineNumberAnnotator.Annotate(root);
            ReportStep("LineNumberAnnotator", annotated, originalErrors);

            // Test each rewriter individually (applied on top of annotated root only)
            var rewriters = new (string Name, Func<SyntaxNode, SyntaxNode> Apply)[]
            {
                ("SafetyRewriter", node => new SafetyRewriter { TargetFileName = relPath }.Visit(node)),
                ("ControlFlowRewriter", node => new ControlFlowRewriter { TargetFileName = relPath }.Visit(node)),
                ("VariableRewriter", node => new VariableRewriter { TargetFileName = relPath }.Visit(node)),
                ("HeapRewriter", node => new HeapRewriter { TargetFileName = relPath }.Visit(node)),
                ("CastingRewriter", node => new CastingRewriter { TargetFileName = relPath }.Visit(node)),
                ("OverflowRewriter", node => new OverflowRewriter { TargetFileName = relPath }.Visit(node)),
            };

            foreach (var (name, apply) in rewriters)
            {
                try
                {
                    var result = apply(annotated);
                    int errs = ReportStep(name + " (solo)", result, originalErrors);
                    if (errs > originalErrors)
                    {
                        // Dump the broken output to a temp file for inspection
                        string dumpPath = Path.Combine(Path.GetTempPath(), $"diagnose_{name}.cs");
                        File.WriteAllText(dumpPath, result.ToFullString());
                        Console.WriteLine($"    Dumped to: {dumpPath}");
                        var newTree2 = CSharpSyntaxTree.ParseText(result.ToFullString());
                        foreach (var diag in newTree2.GetDiagnostics()
                            .Where(d => d.Severity == DiagnosticSeverity.Error))
                        {
                            var span = diag.Location.GetLineSpan();
                            Console.WriteLine($"    Error at line {span.StartLinePosition.Line + 1}: {diag.GetMessage()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {name} (solo): EXCEPTION — {ex.Message}");
                }
            }

            // Now test cumulative pipeline
            Console.WriteLine("\n--- Cumulative pipeline ---");
            SyntaxNode current = annotated;
            foreach (var (name, apply) in rewriters)
            {
                try
                {
                    current = apply(current);
                    int errs = ReportStep(name, current, originalErrors);
                    if (errs > originalErrors)
                    {
                        // Dump first few errors
                        var newTree2 = CSharpSyntaxTree.ParseText(current.ToFullString());
                        foreach (var diag in newTree2.GetDiagnostics()
                            .Where(d => d.Severity == DiagnosticSeverity.Error)
                            .Take(5))
                        {
                            var span = diag.Location.GetLineSpan();
                            Console.WriteLine($"    Error at line {span.StartLinePosition.Line + 1}: {diag.GetMessage()}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  {name}: EXCEPTION — {ex.Message}");
                    break;
                }
            }
        }

        static int ReportStep(string stepName, SyntaxNode node, int originalErrors)
        {
            var newTree = CSharpSyntaxTree.ParseText(node.ToFullString());
            int newErrors = newTree.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
            int delta = newErrors - originalErrors;
            string status = delta == 0 ? "OK" : $"+{delta} error(s)";
            Console.WriteLine($"  {stepName}: {status} (total: {newErrors})");
            return newErrors;
        }

        static bool InstrumentFile(string filePath, string rootPath)
        {
            Console.WriteLine($"Processing: {filePath}");
            try
            {
                // Create Backup if not exists (atomic check-and-copy)
                string backupPath = filePath + ".bak";
                if (!File.Exists(backupPath))
                {
                    try { File.Copy(filePath, backupPath, overwrite: false); }
                    catch (IOException) { /* another thread created it first */ }
                }

                string code = File.ReadAllText(filePath);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                // Count pre-existing parse errors so we only flag new ones.
                int originalErrors = tree.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);

                // 0. Annotate
                var root0 = LineNumberAnnotator.Annotate(root);

                // 1. Safety (FIRST - wrap methods/loops before other rewriters instrument them)
                var rewriter1 = new SafetyRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root1 = rewriter1.Visit(root0);

                // 2. Control Flow
                var rewriter2 = new ControlFlowRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root2 = rewriter2.Visit(root1);

                // 3. Variable Values
                var rewriter3 = new VariableRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root3 = rewriter3.Visit(root2);

                // 4. Heap State
                var rewriter4 = new HeapRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root4 = rewriter4.Visit(root3);

                // 5. Casting
                var rewriter5 = new CastingRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root5 = rewriter5.Visit(root4);

                // 6. Overflow
                var rewriter6 = new OverflowRewriter { TargetFileName = GetRelativePath(filePath, rootPath) };
                var root6 = rewriter6.Visit(root5);

                string newCode = root6.ToFullString();

                // Validate: re-parse and check for NEW syntax errors introduced by rewriting.
                var newTree = CSharpSyntaxTree.ParseText(newCode);
                int newErrors = newTree.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
                if (newErrors > originalErrors)
                {
                    // Instrumentation introduced syntax errors — restore original and skip.
                    Console.WriteLine($"  [skip] Instrumentation introduced {newErrors - originalErrors} syntax error(s) — keeping original: {filePath}");
                    // Restore from backup (backup is always the clean original).
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, filePath, overwrite: true);
                    }
                    return false;
                }

                File.WriteAllText(filePath, newCode);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to instrument {filePath}: {ex.Message}");
                return false;
            }
        }

        static string GetRelativePath(string fullPath, string rootPath)
        {
            return Path.GetRelativePath(rootPath, fullPath);
        }

        static void GenerateTraceLogger(string rootPath)
        {
            string traceLoggerPath = Path.Combine(rootPath, "TraceLogger.cs");
            // Use char codes to avoid any backslash/quote escaping in generated strings
            // (char)34 = double-quote, (char)92 = backslash
            string Q = "\"";  // double-quote for use in outer C# strings (safe here)

            var lines = new List<string>
            {
                "using System.Linq;",
                "using System.Collections;",
                "",
                "namespace AutoRepairTracing",
                "{",
                "    public static class TraceLogger",
                "    {",
                "        private static readonly string LogJsonPath;",
                "        private static readonly char DQ = (char)34;",
                "",
                "        static TraceLogger()",
                "        {",
                "            var envPath = System.Environment.GetEnvironmentVariable(" + Q + "TRACE_LOG_JSON_PATH" + Q + ");",
                "            LogJsonPath = string.IsNullOrEmpty(envPath) ? " + Q + "trace_logs.jsonl" + Q + " : envPath;",
                "        }",
                "",
                "        private static string EJ(string v)",
                "        {",
                "            if (v == null) return " + Q + Q + ";",
                "            var r = new System.Text.StringBuilder();",
                "            foreach (char c in v)",
                "            {",
                "                if (c == (char)34) { r.Append((char)92); r.Append((char)34); }",
                "                else if (c == (char)92) { r.Append((char)92); r.Append((char)92); }",
                "                else if (c == (char)10) { r.Append((char)92); r.Append('n'); }",
                "                else if (c == (char)13) { r.Append((char)92); r.Append('r'); }",
                "                else if (c == (char)9)  { r.Append((char)92); r.Append('t'); }",
                "                else r.Append(c);",
                "            }",
                "            return r.ToString();",
                "        }",
                "",
                "        private static string JStr(string s) { return DQ + s + DQ; }",
                "",
                "        public static void Log(string uniqueId, string file, int line, string message, object variableValue = null, string variableName = null)",
                "        {",
                "            var ts = System.DateTime.Now.ToString(" + Q + "yyyy-MM-dd HH:mm:ss.fff" + Q + ");",
                "            var tid = System.Threading.Thread.CurrentThread.ManagedThreadId;",
                "            try",
                "            {",
                "                string varPart = variableName != null",
                "                    ? " + Q + "{" + Q + " + JStr(" + Q + "name" + Q + ") + " + Q + ":" + Q + " + JStr(EJ(variableName)) + " + Q + "," + Q + " + JStr(" + Q + "val" + Q + ") + " + Q + ":" + Q + " + (variableValue != null ? JStr(EJ(variableValue.ToString())) : " + Q + "null" + Q + ") + " + Q + "}" + Q,
                "                    : " + Q + "null" + Q + ";",
                "                string j = " + Q + "{" + Q,
                "                    + JStr(" + Q + "id"   + Q + ") + " + Q + ":" + Q + " + JStr(EJ(uniqueId))",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "ts"   + Q + ") + " + Q + ":" + Q + " + JStr(EJ(ts))",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "tid"  + Q + ") + " + Q + ":" + Q + " + tid",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "file" + Q + ") + " + Q + ":" + Q + " + JStr(EJ(file))",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "line" + Q + ") + " + Q + ":" + Q + " + line",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "msg"  + Q + ") + " + Q + ":" + Q + " + JStr(EJ(message))",
                "                    + " + Q + "," + Q + " + JStr(" + Q + "var"  + Q + ") + " + Q + ":" + Q + " + varPart + " + Q + "}" + Q + ";",
                "                lock (LogJsonPath) { System.IO.File.AppendAllText(LogJsonPath, j + System.Environment.NewLine); }",
                "            }",
                "            catch {}",
                "        }",
                "",
                "        public static T LogValue<T>(string uniqueId, string file, int line, string message, T value, string variableName = null)",
                "        {",
                "            Log(uniqueId, file, line, message, value, variableName);",
                "            return value;",
                "        }",
                "",
                "        public static void LogArray(string uniqueId, string file, int line, string message, IEnumerable array, string variableName = null)",
                "        {",
                "            object snapshot = null;",
                "            if (array != null)",
                "            {",
                "                var values = array.Cast<object>().Select(v => v != null ? v.ToString() : " + Q + "null" + Q + ");",
                "                snapshot = " + Q + "[" + Q + " + string.Join(" + Q + ", " + Q + ", values) + " + Q + "]" + Q + ";",
                "            }",
                "            Log(uniqueId, file, line, message, snapshot, variableName);",
                "        }",
                "",
                "        public static void LogArrayAliasParameter(string uniqueId, string file, int line, string methodName, int position, IEnumerable array, string parameterName)",
                "        { Log(uniqueId, file, line, " + Q + "Array Parameter Alias" + Q + ", parameterName, methodName + " + Q + "#arg" + Q + " + position); }",
                "",
                "        public static void LogArrayAliasArgument(string uniqueId, string file, int line, string methodName, int position, object argumentValue, string argumentExpression)",
                "        { Log(uniqueId, file, line, " + Q + "Array Argument Alias" + Q + ", argumentExpression, methodName + " + Q + "#arg" + Q + " + position); }",
                "",
                "        public static object LogCastSource(string uniqueId, string file, int line, object source, string targetType)",
                "        {",
                "            var sourceType = source != null ? source.GetType().FullName : " + Q + "null" + Q + ";",
                "            Log(uniqueId, file, line, " + Q + "Cast Attempt" + Q + ", sourceType, " + Q + "cast:" + Q + " + targetType);",
                "            return source;",
                "        }",
                "",
                "        public static T LogAsCast<T>(string uniqueId, string file, int line, object source, string targetType) where T : class",
                "        {",
                "            var result = source as T;",
                "            var sourceType = source != null ? source.GetType().FullName : " + Q + "null" + Q + ";",
                "            if (result == null && source != null)",
                "                Log(uniqueId, file, line, " + Q + "Invalid Cast" + Q + ", sourceType, " + Q + "cast:" + Q + " + targetType);",
                "            else",
                "                Log(uniqueId, file, line, " + Q + "Cast Attempt" + Q + ", sourceType, " + Q + "cast:" + Q + " + targetType);",
                "            return result;",
                "        }",
                "",
                "        public static long LogCheckedBinary(string uniqueId, string file, int line, long left, long right, string op)",
                "        {",
                "            try { long r = op == " + Q + "+" + Q + " ? checked(left+right) : op == " + Q + "-" + Q + " ? checked(left-right) : op == " + Q + "*" + Q + " ? checked(left*right) : left; return r; }",
                "            catch (System.OverflowException) { Log(uniqueId, file, line, " + Q + "Overflow" + Q + ", left + " + Q + "|" + Q + " + right + " + Q + "|Int64" + Q + ", " + Q + "overflow:" + Q + " + op); return op == " + Q + "+" + Q + " ? unchecked(left+right) : op == " + Q + "-" + Q + " ? unchecked(left-right) : op == " + Q + "*" + Q + " ? unchecked(left*right) : left; }",
                "        }",
                "",
                "        public static int LogCheckedBinary(string uniqueId, string file, int line, int left, int right, string op)",
                "        {",
                "            try { int r = op == " + Q + "+" + Q + " ? checked(left+right) : op == " + Q + "-" + Q + " ? checked(left-right) : op == " + Q + "*" + Q + " ? checked(left*right) : left; return r; }",
                "            catch (System.OverflowException) { Log(uniqueId, file, line, " + Q + "Overflow" + Q + ", left + " + Q + "|" + Q + " + right + " + Q + "|Int32" + Q + ", " + Q + "overflow:" + Q + " + op); return op == " + Q + "+" + Q + " ? unchecked(left+right) : op == " + Q + "-" + Q + " ? unchecked(left-right) : op == " + Q + "*" + Q + " ? unchecked(left*right) : left; }",
                "        }",
                "",
                "        public static uint? LogCheckedBinary(string uniqueId, string file, int line, uint? left, uint? right, string op)",
                "        {",
                "            try { uint? r = op == " + Q + "+" + Q + " ? left+right : op == " + Q + "-" + Q + " ? left-right : op == " + Q + "*" + Q + " ? left*right : left; return r; }",
                "            catch (System.OverflowException) { Log(uniqueId, file, line, " + Q + "Overflow" + Q + ", left + " + Q + "|" + Q + " + right + " + Q + "|uint?" + Q + ", " + Q + "overflow:" + Q + " + op); return left; }",
                "        }",
                "",
                "        public static decimal LogCheckedBinary(string uniqueId, string file, int line, decimal left, decimal right, string op)",
                "        {",
                "            try { decimal r = op == " + Q + "+" + Q + " ? checked(left+right) : op == " + Q + "-" + Q + " ? checked(left-right) : op == " + Q + "*" + Q + " ? checked(left*right) : left; return r; }",
                "            catch (System.OverflowException) { Log(uniqueId, file, line, " + Q + "Overflow" + Q + ", left + " + Q + "|" + Q + " + right + " + Q + "|Decimal" + Q + ", " + Q + "overflow:" + Q + " + op); return left; }",
                "        }",
                "",
                "        // Catch-all overloads for non-numeric operands the rewriter could not",
                "        // type-check syntactically (e.g. string concat, char arithmetic). They",
                "        // bypass overflow checking and just compute the operation.",
                "        public static string LogCheckedBinary(string uniqueId, string file, int line, string left, string right, string op)",
                "        { return op == " + Q + "+" + Q + " ? (left ?? " + Q + Q + ") + (right ?? " + Q + Q + ") : left; }",
                "",
                "        public static string LogCheckedBinary(string uniqueId, string file, int line, string left, object right, string op)",
                "        { return op == " + Q + "+" + Q + " ? (left ?? " + Q + Q + ") + (right != null ? right.ToString() : " + Q + Q + ") : left; }",
                "",
                "        public static string LogCheckedBinary(string uniqueId, string file, int line, object left, string right, string op)",
                "        { return op == " + Q + "+" + Q + " ? (left != null ? left.ToString() : " + Q + Q + ") + (right ?? " + Q + Q + ") : right; }",
                "    }",
                "}"
            };

            File.WriteAllText(traceLoggerPath, string.Join(System.Environment.NewLine, lines));
            Console.WriteLine($"Generated TraceLogger.cs at {traceLoggerPath}");
        }
    }
}

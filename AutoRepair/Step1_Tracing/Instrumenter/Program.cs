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
                Console.WriteLine("  Instrumenter --scan-root <LibrariesRootPath>");
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

            if (args[0] == "--scan-root")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: --scan-root requires a directory path.");
                    return;
                }
                string scanRoot = Path.GetFullPath(args[1]);
                if (!Directory.Exists(scanRoot))
                {
                    Console.WriteLine($"Error: Directory not found at {scanRoot}!");
                    return;
                }
                RunScanRoot(scanRoot);
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

                // Inject pragma to suppress nullable warnings introduced by instrumentation.
                // Must come before any namespace/class declarations but after existing #-directives.
                newCode = "#pragma warning disable CS8600,CS8602,CS8604,CS8619,CS8625,CS0161,CS0165,CS0177,CA5360,CS8603,CS0269,CS0675,CS0103\n" + newCode;

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

        /// <summary>
        /// --scan-root mode: discovers all library source directories under the given root
        /// (e.g. runtime/src/libraries) and instruments each one independently.
        /// Expects the pattern: {scanRoot}/*/src/*.csproj with .cs files.
        /// </summary>
        static void RunScanRoot(string scanRoot)
        {
            Console.WriteLine($"Scanning for library source projects under: {scanRoot}");

            // Exclusions: projects that can't or shouldn't be instrumented
            var excludedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Private.CoreLib" // Special build — compiled as part of the runtime itself
            };

            var libraryDirs = Directory.GetDirectories(scanRoot);
            var projectDirs = new List<(string SourceDir, string CsprojPath, string LibName)>();

            foreach (var libDir in libraryDirs)
            {
                string libName = Path.GetFileName(libDir);
                if (excludedProjects.Contains(libName))
                {
                    Console.WriteLine($"  [skip] {libName} (excluded)");
                    continue;
                }

                string srcDir = Path.Combine(libDir, "src");
                if (!Directory.Exists(srcDir))
                    continue;

                // Find .csproj in the src/ directory
                var csprojFiles = Directory.GetFiles(srcDir, "*.csproj");
                if (csprojFiles.Length == 0)
                    continue;

                // Check for .cs files (excluding obj/bin/generated)
                var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                             && !f.EndsWith(".g.cs")
                             && !f.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase));

                if (!csFiles.Any())
                    continue;

                projectDirs.Add((srcDir, csprojFiles[0], libName));
            }

            Console.WriteLine($"Found {projectDirs.Count} library source projects to instrument.");

            int totalSucceeded = 0;
            int totalFailed = 0;

            foreach (var (sourceDir, csprojPath, libName) in projectDirs)
            {
                Console.WriteLine($"\n=== Instrumenting: {libName} ===");

                // Generate TraceLogger.cs in this project's src/ directory
                GenerateTraceLogger(sourceDir);

                // Patch the .csproj to include TraceLogger.cs (runtime uses EnableDefaultItems=false)
                PatchCsprojForTraceLogger(csprojPath);

                // Find .cs files to instrument
                var files = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                             && !f.EndsWith("TraceLogger.cs")
                             && !f.EndsWith(".g.cs")
                             && !f.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                Console.WriteLine($"  {files.Length} C# files to instrument.");

                int succeeded = 0;
                int failed = 0;
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    file =>
                    {
                        bool ok = InstrumentFile(file, sourceDir);
                        if (ok)
                            Interlocked.Increment(ref succeeded);
                        else
                            Interlocked.Increment(ref failed);
                    });

                Console.WriteLine($"  Succeeded: {succeeded}, Failed/Skipped: {failed}");
                totalSucceeded += succeeded;
                totalFailed += failed;
            }

            Console.WriteLine($"\n=== Scan-Root Instrumentation Complete ===");
            Console.WriteLine($"Projects: {projectDirs.Count}");
            Console.WriteLine($"Files Succeeded: {totalSucceeded}");
            Console.WriteLine($"Files Failed/Skipped: {totalFailed}");

            // Handle generator/analyzer projects that share source files from src/
            // They need a TraceLogger stub so the instrumented shared files compile.
            // Note: Gen projects typically have EnableDefaultItems=true (SDK default),
            //       so we only generate the stub file — no csproj patching needed.
            Console.WriteLine("\nPatching generator projects with TraceLogger stubs...");
            int genPatched = 0;
            foreach (var libDir in libraryDirs)
            {
                string genDir = Path.Combine(libDir, "gen");
                if (!Directory.Exists(genDir))
                    continue;

                var genCsprojs = Directory.GetFiles(genDir, "*.csproj", SearchOption.AllDirectories);
                foreach (var genCsproj in genCsprojs)
                {
                    string genCsprojDir = Path.GetDirectoryName(genCsproj)!;
                    GenerateTraceLoggerStub(genCsprojDir);
                    genPatched++;
                }
            }
            Console.WriteLine($"Generator projects patched: {genPatched}");
        }

        /// <summary>
        /// Adds a &lt;Compile Include="TraceLogger.cs"/&gt; item to the .csproj so that
        /// the runtime build (which uses EnableDefaultItems=false) picks up the file.
        /// We modify each project individually instead of Directory.Build.targets to
        /// avoid invalidating the global NuGet restore cache.
        /// </summary>
        static void PatchCsprojForTraceLogger(string csprojPath)
        {
            // Backup the original csproj
            string backupPath = csprojPath + ".bak";
            if (!File.Exists(backupPath))
            {
                try { File.Copy(csprojPath, backupPath, overwrite: false); }
                catch (IOException) { /* another thread created it first */ }
            }

            string content = File.ReadAllText(csprojPath);

            // If the csproj explicitly sets EnableDefaultItems=true, the SDK auto-includes
            // all .cs files, so adding an explicit Compile item would cause NETSDK1022.
            // Note: most runtime library csprojs inherit EnableDefaultItems=false from
            // Directory.Build.props, so we only skip when it's explicitly overridden to true.
            if (content.Contains("<EnableDefaultItems>true</EnableDefaultItems>"))
            {
                Console.WriteLine($"  Skipping csproj patch (EnableDefaultItems=true, SDK auto-includes .cs files)");
                return;
            }

            // Check if already patched
            if (content.Contains("TraceLogger.cs"))
            {
                Console.WriteLine($"  .csproj already includes TraceLogger.cs");
                return;
            }

            // Insert <Compile Include="TraceLogger.cs" /> before the closing </Project> tag
            const string marker = "</Project>";
            int idx = content.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                Console.WriteLine($"  WARNING: Could not find </Project> in {csprojPath}");
                return;
            }

            string patch = "  <ItemGroup>\n    <Compile Include=\"TraceLogger.cs\" />\n  </ItemGroup>\n";
            content = content.Substring(0, idx) + patch + content.Substring(idx);
            File.WriteAllText(csprojPath, content);
            Console.WriteLine($"  Patched {Path.GetFileName(csprojPath)} with TraceLogger.cs include");
        }

        static string GetRelativePath(string fullPath, string rootPath)
        {
            return Path.GetRelativePath(rootPath, fullPath);
        }

        /// <summary>
        /// Generates a minimal no-op TraceLogger stub for generator/analyzer projects
        /// that share source files from src/. These projects typically target netstandard2.0
        /// and don't need actual tracing — they just need the type to exist for compilation.
        /// </summary>
        static void GenerateTraceLoggerStub(string directory)
        {
            string path = Path.Combine(directory, "TraceLogger.cs");
            if (File.Exists(path)) return;

            string code = @"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Auto-generated stub for generator project compilation compatibility.
#pragma warning disable
namespace AutoRepairTracing
{
    internal static class TraceLogger
    {
        public static void Log(string uniqueId, string file, int line, string message, object variableValue = null, string variableName = null) { }
        public static T LogValue<T>(string uniqueId, string file, int line, string message, T value, string variableName = null) => value;
        public static string Str(object value) { try { return value != null ? value.ToString() : null; } catch { return null; } }
        public static string Str(string value) => value;
        public static void LogArray(string uniqueId, string file, int line, string message, System.Array array, string variableName = null) { }
        public static void LogArrayAliasParameter(string uniqueId, string file, int line, string methodName, int position, System.Array array, string parameterName) { }
        public static void LogArrayAliasArgument(string uniqueId, string file, int line, string methodName, int position, object argumentValue, string argumentExpression) { }
        public static object LogCastSource(string uniqueId, string file, int line, object source, string targetType) => source;
        public static T LogAsCast<T>(string uniqueId, string file, int line, object source, string targetType) where T : class => source as T;
        public static int LogCheckedBinary(string uniqueId, string file, int line, int left, int right, string op) => left;
        public static long LogCheckedBinary(string uniqueId, string file, int line, long left, long right, string op) => left;
        public static decimal LogCheckedBinary(string uniqueId, string file, int line, decimal left, decimal right, string op) => left;
        public static uint LogCheckedBinary(string uniqueId, string file, int line, uint left, uint right, string op) => left;
        public static ulong LogCheckedBinary(string uniqueId, string file, int line, ulong left, ulong right, string op) => left;
        public static double LogCheckedBinary(string uniqueId, string file, int line, double left, double right, string op) => left;
        public static float LogCheckedBinary(string uniqueId, string file, int line, float left, float right, string op) => left;
        public static int LogCheckedBinary(string uniqueId, string file, int line, byte left, byte right, string op) => left;
        public static int LogCheckedBinary(string uniqueId, string file, int line, ushort left, ushort right, string op) => left;
        public static int LogCheckedBinary(string uniqueId, string file, int line, short left, short right, string op) => left;
        public static int LogCheckedBinary(string uniqueId, string file, int line, sbyte left, sbyte right, string op) => left;
        public static int LogCheckedBinary(string uniqueId, string file, int line, char left, char right, string op) => left;
        public static TLeft LogCheckedBinary<TLeft, TRight>(string uniqueId, string file, int line, TLeft left, TRight right, string op) => left;
    }
}
";
            File.WriteAllText(path, code);
            Console.WriteLine($"  Generated TraceLogger stub at {path}");
        }

        static void GenerateTraceLogger(string rootPath)
        {
            string traceLoggerPath = Path.Combine(rootPath, "TraceLogger.cs");
            string code = @"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
#pragma warning disable
#pragma warning disable CA5360
namespace AutoRepairTracing
{
    internal static class TraceLogger
    {
        private static readonly string LogJsonPath =
            System.Environment.GetEnvironmentVariable(""TRACE_LOG_JSON_PATH"") ?? ""trace_logs.jsonl"";
        private static readonly object FileLock = new object();

        private static string EscapeJson(string s)
        {
            if (s == null) return ""null"";
            var sb = new System.Text.StringBuilder(s.Length + 10);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append(""\\\\""); break;
                    case '""': sb.Append(""\\\""""); break;
                    case '\n': sb.Append(""\\n""); break;
                    case '\r': sb.Append(""\\r""); break;
                    case '\t': sb.Append(""\\t""); break;
                    default:   sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static void Log(string uniqueId, string file, int line, string message, object variableValue = null, string variableName = null)
        {
            var timestamp = System.DateTime.Now.ToString(""yyyy-MM-dd HH:mm:ss.fff"");
            var threadId = System.Environment.CurrentManagedThreadId;

            try
            {
                var sb = new System.Text.StringBuilder(256);
                sb.Append(""{\""id\"":\"""").Append(EscapeJson(uniqueId)).Append(""\"""");
                sb.Append("",\""ts\"":\"""").Append(EscapeJson(timestamp)).Append(""\"""");
                sb.Append("",\""tid\"":"").Append(threadId);
                sb.Append("",\""file\"":\"""").Append(EscapeJson(file)).Append(""\"""");
                sb.Append("",\""line\"":"").Append(line);
                sb.Append("",\""msg\"":\"""").Append(EscapeJson(message)).Append(""\"""");

                if (variableName != null)
                {
                    sb.Append("",\""var\"":{\""name\"":\"""").Append(EscapeJson(variableName)).Append(""\"""");
                    sb.Append("",\""val\"":\"""").Append(EscapeJson(variableValue?.ToString())).Append(""\""}"");
                }
                else
                {
                    sb.Append("",\""var\"":null"");
                }
                sb.Append(""}"");

                System.IO.File.AppendAllText(LogJsonPath, sb.ToString() + System.Environment.NewLine);
            }
            catch {}
        }

        public static T LogValue<T>(string uniqueId, string file, int line, string message, T value, string variableName = null)
        {
            try
            {
                string s;
                try { s = value?.ToString() ?? ""null""; } catch { s = ""null""; }
                Log(uniqueId, file, line, message, s, variableName);
            }
            catch {}
            return value;
        }

        public static string Str(object value)
        {
            try { return value != null ? value.ToString() : ""null""; }
            catch { return ""null""; }
        }

        public static string Str(string value) => value ?? ""null"";

        public static void LogArray(string uniqueId, string file, int line, string message, System.Array array, string variableName = null)
        {
            object snapshot = null;
            if (array != null)
            {
                var sb = new System.Text.StringBuilder(""["");
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0) sb.Append("", "");
                    var item = array.GetValue(i);
                    sb.Append(item?.ToString() ?? ""null"");
                }
                sb.Append(""]"");
                snapshot = sb.ToString();
            }
            Log(uniqueId, file, line, message, snapshot, variableName);
        }

        public static void LogArrayAliasParameter(string uniqueId, string file, int line, string methodName, int position, System.Array array, string parameterName)
        {
            var aliasLabel = $""{methodName}#arg{position}"";
            Log(uniqueId, file, line, ""Array Parameter Alias"", parameterName, aliasLabel);
        }

        public static void LogArrayAliasArgument(string uniqueId, string file, int line, string methodName, int position, object argumentValue, string argumentExpression)
        {
            var aliasLabel = $""{methodName}#arg{position}"";
            Log(uniqueId, file, line, ""Array Argument Alias"", argumentExpression, aliasLabel);
        }

        public static object LogCastSource(string uniqueId, string file, int line, object source, string targetType)
        {
            var sourceType = source?.GetType()?.FullName ?? ""null"";
            Log(uniqueId, file, line, ""Cast Attempt"", sourceType, ""cast:"" + targetType);
            return source;
        }

        public static T LogAsCast<T>(string uniqueId, string file, int line, object source, string targetType) where T : class
        {
            var result = source as T;
            var sourceType = source?.GetType()?.FullName ?? ""null"";
            if (result == null && source != null)
            {
                Log(uniqueId, file, line, ""Invalid Cast"", sourceType, ""cast:"" + targetType);
            }
            else
            {
                Log(uniqueId, file, line, ""Cast Attempt"", sourceType, ""cast:"" + targetType);
            }
            return result;
        }

        // ── LogCheckedBinary overloads ──────────────────────────────────────
        // int
        public static int LogCheckedBinary(string uniqueId, string file, int line, int left, int right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|Int32"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // long
        public static long LogCheckedBinary(string uniqueId, string file, int line, long left, long right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|Int64"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // decimal
        public static decimal LogCheckedBinary(string uniqueId, string file, int line, decimal left, decimal right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|Decimal"", ""overflow:"" + op);
                return left;
            }
        }

        // uint
        public static uint LogCheckedBinary(string uniqueId, string file, int line, uint left, uint right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|UInt32"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // ulong
        public static ulong LogCheckedBinary(string uniqueId, string file, int line, ulong left, ulong right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|UInt64"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // nint
        public static nint LogCheckedBinary(string uniqueId, string file, int line, nint left, nint right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|IntPtr"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // nuint
        public static nuint LogCheckedBinary(string uniqueId, string file, int line, nuint left, nuint right, string op)
        {
            try
            {
                return op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
            }
            catch (System.OverflowException)
            {
                Log(uniqueId, file, line, ""Overflow"", $""{left}|{right}|UIntPtr"", ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        // byte (promotes to int, matching C# semantics)
        public static int LogCheckedBinary(string uniqueId, string file, int line, byte left, byte right, string op)
            => LogCheckedBinary(uniqueId, file, line, (int)left, (int)right, op);

        // ushort (promotes to int, matching C# semantics)
        public static int LogCheckedBinary(string uniqueId, string file, int line, ushort left, ushort right, string op)
            => LogCheckedBinary(uniqueId, file, line, (int)left, (int)right, op);

        // double (no overflow — saturates to Infinity)
        public static double LogCheckedBinary(string uniqueId, string file, int line, double left, double right, string op)
        {
            return op switch
            {
                ""+"" => left + right,
                ""-"" => left - right,
                ""*"" => left * right,
                _ => left
            };
        }

        // float (no overflow — saturates to Infinity)
        public static float LogCheckedBinary(string uniqueId, string file, int line, float left, float right, string op)
        {
            return op switch
            {
                ""+"" => left + right,
                ""-"" => left - right,
                ""*"" => left * right,
                _ => left
            };
        }

        // Generic catch-all for non-numeric types (e.g. string + string, enum, etc.)
        // Lower priority than specific overloads due to being generic.
        public static TLeft LogCheckedBinary<TLeft, TRight>(string uniqueId, string file, int line, TLeft left, TRight right, string op)
        {
            try
            {
                string sl; try { sl = left?.ToString() ?? ""null""; } catch { sl = ""null""; }
                string sr; try { sr = right?.ToString() ?? ""null""; } catch { sr = ""null""; }
                Log(uniqueId, file, line, ""Overflow Check"", sl + ""|"" + sr, ""overflow:"" + op);
            }
            catch {}
            return left;
        }

        // short (promotes to int, matching C# semantics)
        public static int LogCheckedBinary(string uniqueId, string file, int line, short left, short right, string op)
            => LogCheckedBinary(uniqueId, file, line, (int)left, (int)right, op);

        // sbyte (promotes to int, matching C# semantics)
        public static int LogCheckedBinary(string uniqueId, string file, int line, sbyte left, sbyte right, string op)
            => LogCheckedBinary(uniqueId, file, line, (int)left, (int)right, op);

        // char (promotes to int, matching C# semantics)
        public static int LogCheckedBinary(string uniqueId, string file, int line, char left, char right, string op)
            => LogCheckedBinary(uniqueId, file, line, (int)left, (int)right, op);
    }
}
";
            File.WriteAllText(traceLoggerPath, code);
            Console.WriteLine($"Generated TraceLogger.cs at {traceLoggerPath}");
        }
    }
}

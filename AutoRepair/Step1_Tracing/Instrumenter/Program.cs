using System;
using System.IO;
using System.Linq;
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
                Console.WriteLine("Usage: Instrumenter <ProjectRootPath>");
                return;
            }

            string rootPath = Path.GetFullPath(args[0]);
            Console.WriteLine($"Instrumenting project at: {rootPath}");

            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Error: Directory not found at {rootPath}!");
                return;
            }

            // 1. Generate TraceLogger
            GenerateTraceLogger(rootPath);

            // 2. Find and Instrument Files
            var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\") && !f.EndsWith("TraceLogger.cs"))
                .ToList();

            Console.WriteLine($"Found {files.Count} C# files to instrument.");

            foreach (var file in files)
            {
                InstrumentFile(file, rootPath);
            }
            
            Console.WriteLine("Instrumentation Complete.");
        }

        static void InstrumentFile(string filePath, string rootPath)
        {
            Console.WriteLine($"Processing: {filePath}");
            try
            {
                // Create Backup if not exists
                string backupPath = filePath + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(filePath, backupPath);
                }

                string code = File.ReadAllText(filePath);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

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
                File.WriteAllText(filePath, newCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to instrument {filePath}: {ex.Message}");
            }
        }

        static string GetRelativePath(string fullPath, string rootPath)
        {
            return Path.GetRelativePath(rootPath, fullPath);
        }

        static void GenerateTraceLogger(string rootPath)
        {
            string traceLoggerPath = Path.Combine(rootPath, "TraceLogger.cs");
            string code = @"
using System.Linq;

namespace AutoRepairTracing
{
    public static class TraceLogger
    {
        private static readonly string LogJsonPath;

        static TraceLogger()
        {
            var envPath = System.Environment.GetEnvironmentVariable(""TRACE_LOG_JSON_PATH"");
            LogJsonPath = string.IsNullOrEmpty(envPath) ? ""trace_logs.jsonl"" : envPath;
        }

        public static void Log(string uniqueId, string file, int line, string message, object? variableValue = null, string? variableName = null)
        {
            var timestamp = System.DateTime.Now.ToString(""yyyy-MM-dd HH:mm:ss.fff"");
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            try
            {
                // JSON Output
                var jsonEntryObj = new 
                {
                    id = uniqueId,
                    ts = timestamp,
                    tid = threadId,
                    file = file,
                    line = line,
                    msg = message,
                    var = variableName != null ? new { name = variableName, val = variableValue?.ToString() } : null
                };
                
                string jsonLine = System.Text.Json.JsonSerializer.Serialize(jsonEntryObj);
                
                lock (LogJsonPath) // Simple file lock
                {
                   System.IO.File.AppendAllText(LogJsonPath, jsonLine + System.Environment.NewLine);
                }
            }
            catch {}
        }

        public static T LogValue<T>(string uniqueId, string file, int line, string message, T value, string? variableName = null)
        {
            Log(uniqueId, file, line, message, value, variableName);
            return value;
        }

        public static void LogArray(string uniqueId, string file, int line, string message, System.Array array, string? variableName = null)
        {
            object? snapshot = null;
            if (array != null)
            {
                var values = array.Cast<object?>()
                    .Select(v => v?.ToString() ?? ""null"");
                snapshot = ""["" + string.Join("", "", values) + ""]"";
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

        /// <summary>
        /// Logs the source runtime type before an explicit cast and returns the source unchanged.
        /// The outer (T) cast in the instrumented code performs the real conversion.
        /// </summary>
        public static object LogCastSource(string uniqueId, string file, int line, object source, string targetType)
        {
            var sourceType = source?.GetType()?.FullName ?? ""null"";
            Log(uniqueId, file, line, ""Cast Attempt"", sourceType, ""cast:"" + targetType);
            return source;
        }

        /// <summary>
        /// Performs an 'as' cast, logging ""Invalid Cast"" when the result is null but the source was non-null.
        /// </summary>
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

        /// <summary>
        /// Evaluates a binary arithmetic operation inside a checked context.
        /// If overflow occurs, logs an ""Overflow"" event with operand values and returns
        /// the unchecked result so execution can continue for further tracing.
        /// </summary>
        public static long LogCheckedBinary(string uniqueId, string file, int line, long left, long right, string op)
        {
            try
            {
                long result = op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
                return result;
            }
            catch (System.OverflowException)
            {
                var payload = $""{left}|{right}|Int64"";
                Log(uniqueId, file, line, ""Overflow"", payload, ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        /// <summary>int overload - promotes to long for range checking then casts back.</summary>
        public static int LogCheckedBinary(string uniqueId, string file, int line, int left, int right, string op)
        {
            try
            {
                int result = op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
                return result;
            }
            catch (System.OverflowException)
            {
                var payload = $""{left}|{right}|Int32"";
                Log(uniqueId, file, line, ""Overflow"", payload, ""overflow:"" + op);
                return op switch
                {
                    ""+"" => unchecked(left + right),
                    ""-"" => unchecked(left - right),
                    ""*"" => unchecked(left * right),
                    _ => left
                };
            }
        }

        /// <summary>decimal overload for financial arithmetic.</summary>
        public static decimal LogCheckedBinary(string uniqueId, string file, int line, decimal left, decimal right, string op)
        {
            try
            {
                decimal result = op switch
                {
                    ""+"" => checked(left + right),
                    ""-"" => checked(left - right),
                    ""*"" => checked(left * right),
                    _ => left
                };
                return result;
            }
            catch (System.OverflowException)
            {
                var payload = $""{left}|{right}|Decimal"";
                Log(uniqueId, file, line, ""Overflow"", payload, ""overflow:"" + op);

                // Decimal arithmetic cannot provide a wrapped unchecked result.
                return left;
            }
        }
    }
}
";
            File.WriteAllText(traceLoggerPath, code);
            Console.WriteLine($"Generated TraceLogger.cs at {traceLoggerPath}");
        }
    }
}

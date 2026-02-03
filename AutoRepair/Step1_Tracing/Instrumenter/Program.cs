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

                string newCode = root4.ToFullString();
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
    }
}
";
            File.WriteAllText(traceLoggerPath, code);
            Console.WriteLine($"Generated TraceLogger.cs at {traceLoggerPath}");
        }
    }
}
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
            try 
            {
                string targetPath;
                string outputPath;

                if (args.Length >= 2)
                {
                    targetPath = args[0];
                    outputPath = args[1];
                }
                else
                {
                     // Fallback for debugging or manual run
                     // Assuming running from AutoRepair/Step1_Tracing
                     targetPath = Path.GetFullPath(Path.Combine("..", "..", "TargetApp", "Program.cs"));
                     outputPath = Path.GetFullPath(Path.Combine("..", "..", "TargetApp", "Program_Instrumented.cs"));
                }
                
                Console.WriteLine($"Searching for file at: {targetPath}");

                if (!File.Exists(targetPath))
                {
                    Console.WriteLine($"Error: File not found at {targetPath}!");
                    Console.WriteLine("Usage: Instrumenter <InputFilePath> <OutputFilePath>");
                    return;
                }

                string code = File.ReadAllText(targetPath);
                
                Console.WriteLine("File read successfully. Parsing...");
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();


                // 0. Annotate Original Lines
                Console.WriteLine("=== 0. Annotating Lines ===");
                var root0 = LineNumberAnnotator.Annotate(root);

                // 1. Control Flow Decision
                Console.WriteLine("=== 1. Control Flow Decision ===");
                var rewriter1 = new ControlFlowRewriter();
                var root1 = rewriter1.Visit(root0);
                
                // 2. Variable Values
                Console.WriteLine("=== 2. Variable Values ===");
                var rewriter2 = new VariableRewriter();
                var root2 = rewriter2.Visit(root1);

                // 3. Heap State
                Console.WriteLine("=== 3. Heap State ===");
                var rewriter3 = new HeapRewriter();
                var root3 = rewriter3.Visit(root2);

                // 4. Safety & Error Handling
                Console.WriteLine("=== 4. Safety & Error Handling ===");
                var rewriter4 = new SafetyRewriter();
                var root4 = rewriter4.Visit(root3);

                string newCode = root4.ToFullString();

                // Inject TraceLogger
                string traceLoggerCode = @"
namespace TargetApp
{
    public static class TraceLogger
    {
        private static readonly string LogJsonPath;

        static TraceLogger()
        {
            // Use environment variable for path if available, otherwise default
            var envPath = System.Environment.GetEnvironmentVariable(""TRACE_LOG_JSON_PATH"");
            LogJsonPath = string.IsNullOrEmpty(envPath) ? ""trace_logs.jsonl"" : envPath;

            try { 
                if(System.IO.File.Exists(LogJsonPath)) System.IO.File.Delete(LogJsonPath);
            } catch {}
        }

        public static void Log(string uniqueId, string file, int line, string message, object? variableValue = null, string? variableName = null)
        {
            var timestamp = System.DateTime.Now.ToString(""yyyy-MM-dd HH:mm:ss.fff"");
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            try
            {
               // 1. Write to TXT (Console Output, captured by script)
               var txtLog = $""[{timestamp}] [Thread {threadId}] {message} at {file}:{line} (ID: {uniqueId})"";
               if (variableName != null)
               {
                   txtLog += $"" [Var {variableName}: {variableValue}]"";
               }
               System.Console.WriteLine(txtLog);

               // 2. Write to JSON
               var jsonEntryObj = new 
               {
                    id = uniqueId,
                    timestamp = timestamp,
                    threadId = threadId,
                    file = file,
                    line = line,
                    message = message,
                    variable = variableName != null ? new { name = variableName, value = variableValue?.ToString() } : null
               };
               string jsonLine = System.Text.Json.JsonSerializer.Serialize(jsonEntryObj);

               lock (LogJsonPath)
               {
                   System.IO.File.AppendAllText(LogJsonPath, jsonLine + System.Environment.NewLine);
               }
            }
            catch (System.Exception) 
            {
            }
        }
    }
}
";
                newCode += Environment.NewLine + traceLoggerCode;
                
                File.WriteAllText(outputPath, newCode);
                Console.WriteLine($"Intrumented File Saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
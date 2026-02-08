using System;

namespace TargetApp
{
    class Program
    {
        static void Main(string[] args)
{global::AutoRepairTracing.TraceLogger.Log("dead146d-1217-48a4-80d2-1bee91f1c776", "Program.cs", 7, "Enter Method 'Main'");
try        {
            // Simulate input
            int check = 11; 
global::AutoRepairTracing.TraceLogger.Log("621f42f2-3b12-4546-b1b4-ddd98c922f16", "Program.cs", 10, "Variable Update", check, "check");
            int[] buf1 = new int[10];
global::AutoRepairTracing.TraceLogger.Log("c3483bed-4041-4746-a040-f60b31690385", "Program.cs", 11, "Heap State: buf1 (int[] size=10)");global::AutoRepairTracing.TraceLogger.Log("a250bc8f-5a8a-43a3-ac01-66f638e3a82e", "Program.cs", 11, "Variable Update", buf1, "buf1");
            int[] buf2 = new int[8];
global::AutoRepairTracing.TraceLogger.Log("749a3ab9-0905-45a8-a222-696bd0c642c7", "Program.cs", 12, "Heap State: buf2 (int[] size=8)");global::AutoRepairTracing.TraceLogger.Log("587be50e-eb24-4708-92cb-77748bd0800a", "Program.cs", 12, "Variable Update", buf2, "buf2");

            // ---------------------------------------------------------
            // BUGGY LOOP from Abhishek's PDF
            // ---------------------------------------------------------
            Worker.RunLoop1(check, buf1);
            Worker.RunLoop2(buf2, check);
{global::AutoRepairTracing.TraceLogger.Log("c99e88ed-279e-4387-b885-30bf27ba91e6", "Program.cs", 21, "Variable Update", buf1.Length, "index:buf1");
global::AutoRepairTracing.TraceLogger.LogValue("fec5757a-88a0-4095-bb14-5bfc39a4e7dc", "Program.cs", 22, "Variable Update", buf1[buf1.Length] = 42, "buf1[buf1.Length]");
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("82712ed8-f3ed-4a7a-a76b-c01d0c100800", "Program.cs", 7, "Exception Caught", ex.Message, "ex.Message");global::AutoRepairTracing.TraceLogger.Log("df02bdcc-88e2-4b01-92c5-aab8bfe9e49b", "Program.cs", 7, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("2c5b4b6c-ad04-478a-bd6a-09abed6fea9c", "Program.cs", 7, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");}}    }
}

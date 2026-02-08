using System;

namespace TargetApp
{
    internal static class Worker
    {
        public static void RunLoop1(int check, int[] buffer)
{global::AutoRepairTracing.TraceLogger.Log("31b011ae-092c-4d14-86cd-52c80aea5186", "Worker.cs", 7, "Enter Method 'RunLoop1'");
try        {
            int i = 0;
global::AutoRepairTracing.TraceLogger.Log("d8c6e738-b141-4cc9-93bf-75405cccb94c", "Worker.cs", 9, "Variable Update", i, "i");
{int __loopCounter_73ab0e3f = 0;global::AutoRepairTracing.TraceLogger.Log("2dd37b00-ff01-4c84-b290-8caaf44d016e", "Worker.cs", 11, "Variable Update", __loopCounter_73ab0e3f, "__loopCounter_73ab0e3f");
            while (i < check)
            {
global::AutoRepairTracing.TraceLogger.Log("927d333e-997e-4ed0-8203-4b97b307413d", "Worker.cs", 10, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("9e541ee6-743f-4f11-bc91-8cc8f5361403", "Worker.cs", 14, "Variable Update", __loopCounter_73ab0e3f++, "__loopCounter_73ab0e3f");if(__loopCounter_73ab0e3f > 10000){global::AutoRepairTracing.TraceLogger.Log("7b646af6-df92-492c-91e3-25b60bf680f1", "Worker.cs", 12, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("73ab0e3f-0286-44d4-8397-487d9e610dc8", "Worker.cs", 10, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_73ab0e3f, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("9e349528-3079-43f0-815d-6291a04c4406", "Worker.cs", 12, "Break");
break;}}{global::AutoRepairTracing.TraceLogger.Log("2950847d-8584-4d09-bec3-f4f927066bc2", "Worker.cs", 13, "Variable Update", i, "index:buffer");
global::AutoRepairTracing.TraceLogger.LogValue("439bed79-7bd8-4238-889e-226d7a7538ac", "Worker.cs", 17, "Variable Update", buffer[i] = i + 1, "buffer[i]");
}global::AutoRepairTracing.TraceLogger.LogValue("8cdfa1ca-bd44-49a3-a1ad-816ca03cbec4", "Worker.cs", 19, "Variable Update", i++, "i");
            }
}
            Console.WriteLine("Loop 1 Finished");
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("6f174db6-0d56-498c-85e0-87bc36100f7f", "Worker.cs", 7, "Exception Caught", ex.Message, "ex.Message");global::AutoRepairTracing.TraceLogger.Log("008395bb-df41-460a-9a56-b7cd81bda73c", "Worker.cs", 7, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("0d80e91c-55bd-4c86-ac9b-083174098403", "Worker.cs", 7, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");}}
        public static void RunLoop2(int[] buffer, int startIndex)
{global::AutoRepairTracing.TraceLogger.Log("7a030728-bbb3-4028-99a2-c517b761fbf9", "Worker.cs", 21, "Enter Method 'RunLoop2'");
try        {
            int i = startIndex;
global::AutoRepairTracing.TraceLogger.Log("bfa201d0-3aca-4045-b4fa-3342e1d6d8fc", "Worker.cs", 23, "Variable Update", i, "i");
{int __loopCounter_700a4194 = 0;global::AutoRepairTracing.TraceLogger.Log("9d85b3e4-911c-446a-b5a6-40c875cbabd4", "Worker.cs", 29, "Variable Update", __loopCounter_700a4194, "__loopCounter_700a4194");
            while (i >= 0)
            {
global::AutoRepairTracing.TraceLogger.Log("aa2f00af-6a0c-44c1-8d21-7d9e65565199", "Worker.cs", 24, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("7eaee6c5-ddd6-45f6-a3d6-58d588e3e9c2", "Worker.cs", 32, "Variable Update", __loopCounter_700a4194++, "__loopCounter_700a4194");if(__loopCounter_700a4194 > 10000){global::AutoRepairTracing.TraceLogger.Log("b3b321ea-35ae-46b2-b52d-44f9854f10b8", "Worker.cs", 26, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("700a4194-d614-44aa-9889-f08916ef2c8e", "Worker.cs", 24, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_700a4194, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("ae4a8483-ee1e-480f-81f8-3eb0a08ad2a4", "Worker.cs", 26, "Break");
break;}}{global::AutoRepairTracing.TraceLogger.Log("33b93f13-35c0-44ef-94e4-cfbdd0792b47", "Worker.cs", 27, "Variable Update", i, "index:buffer");
global::AutoRepairTracing.TraceLogger.LogValue("592770ff-511f-4cf0-a48d-a7450cb9544f", "Worker.cs", 35, "Variable Update", buffer[i] = i + 1, "buffer[i]");
}global::AutoRepairTracing.TraceLogger.LogValue("b3eb959a-975f-4755-b98c-f27e82d7debb", "Worker.cs", 37, "Variable Update", i--, "i");
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("95f7b125-c1a1-4592-ad6a-c2abe51df27d", "Worker.cs", 21, "Exception Caught", ex.Message, "ex.Message");global::AutoRepairTracing.TraceLogger.Log("3ffdaed2-a125-42f7-aee0-d66dfdc7d145", "Worker.cs", 21, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("9e193dd9-871d-4f1a-b5e7-0f9f2a4c0213", "Worker.cs", 21, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");}}    }
}

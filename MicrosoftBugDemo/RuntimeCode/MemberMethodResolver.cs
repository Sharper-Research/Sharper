#pragma warning disable CS8600,CS8602,CS8604,CS8619,CS8625,CS0161,CS0165,CS0177,CA5360,CS8603,CS0269,CS0675,CS0103
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace RuntimeCode
{
    internal static class MemberMethodResolver
    {
        public static bool CheckShouldPersist(object o, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("9b89a558-36eb-4a0a-b06f-e6f7256adac5", "MemberMethodResolver.cs", 10, "Enter Method 'CheckShouldPersist'");
try        {
            string methodInvoke = $"ShouldSerialize{memberName}";
global::AutoRepairTracing.TraceLogger.Log("32d01025-6957-413a-8b32-1b9740e6ccfb", "MemberMethodResolver.cs", 12, "Variable Update", methodInvoke, "methodInvoke");
            MethodInfo method = o.GetType().GetMethod(
                methodInvoke,
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("2b1543cd-82d4-4bed-ab0d-dc1c2505a0fc", "MemberMethodResolver.cs", 13, "Variable Update", method, "method");
            bool shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
global::AutoRepairTracing.TraceLogger.Log("dc5ad42b-4128-4f4b-89d1-6bda750793b3", "MemberMethodResolver.cs", 18, "Variable Update", shouldPersist, "shouldPersist");
{global::AutoRepairTracing.TraceLogger.Log("bf63ab73-649e-431b-8c35-665c437f1f57", "MemberMethodResolver.cs", 19, "Return");
            return shouldPersist;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("210d29fc-08dc-4eb7-a2d5-cee855ae9f21", "MemberMethodResolver.cs", 10, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("b3c96a4d-0b8f-4931-83f0-27efb1fb972a", "MemberMethodResolver.cs", 21, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c820fdad-6dc5-418b-955f-13379a3ebbad", "MemberMethodResolver.cs", 10, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("6c4dfca3-6a2f-4296-a6fc-1c9b32fa168d", "MemberMethodResolver.cs", 10, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("bc1c4c5d-78ae-4559-976d-8f1d73d482f0", "MemberMethodResolver.cs", 10, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}    }
}

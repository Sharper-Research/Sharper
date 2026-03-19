// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace RuntimeCode
{
    internal static class MemberMethodResolver
    {
        public static bool CheckShouldPersist(object o, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("a97ddea5-c6b5-4602-aafe-3861a121d8e0", "MemberMethodResolver.cs", 10, "Enter Method 'CheckShouldPersist'");
try        {
            string methodInvoke = $"ShouldSerialize{memberName}";
global::AutoRepairTracing.TraceLogger.Log("3fc4651f-51d1-4ba8-8e83-06767c551b9c", "MemberMethodResolver.cs", 12, "Variable Update", methodInvoke, "methodInvoke");
            MethodInfo method = o.GetType().GetMethod(
                methodInvoke,
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("eb6522a9-a8b8-4ec9-8135-e8b666a05e3f", "MemberMethodResolver.cs", 13, "Variable Update", method, "method");
            bool shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
global::AutoRepairTracing.TraceLogger.Log("43922df4-7875-48ab-9c4a-2b7a22c8b124", "MemberMethodResolver.cs", 18, "Variable Update", shouldPersist, "shouldPersist");
{global::AutoRepairTracing.TraceLogger.Log("19e676cd-90de-49ad-9967-d419d2fd8e3e", "MemberMethodResolver.cs", 19, "Return");
            return shouldPersist;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("6bffbbc1-588c-41aa-b942-ae4a23164a4f", "MemberMethodResolver.cs", 10, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("281eeb42-3d47-4ac3-ab18-aff6b39323e6", "MemberMethodResolver.cs", 21, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("b891c163-a357-4771-bc2e-ce3e03f0810c", "MemberMethodResolver.cs", 10, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("7b2c80e4-d530-4ae6-809d-e1d00d696f0c", "MemberMethodResolver.cs", 10, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("4697254a-9504-4bd4-ab50-81903af1fc9f", "MemberMethodResolver.cs", 10, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}    }
}

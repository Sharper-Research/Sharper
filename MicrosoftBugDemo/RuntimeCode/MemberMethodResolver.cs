// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace RuntimeCode
{
    internal static class MemberMethodResolver
    {
        public static bool CheckShouldPersist(object o, string memberName)
{global::SharperTracing.TraceLogger.Log("c5695355-078e-4241-9dec-a974a0519eb1", "MemberMethodResolver.cs", 10, "Enter Method 'CheckShouldPersist'");
try        {
            string methodInvoke = $"ShouldSerialize{memberName}";
global::SharperTracing.TraceLogger.Log("cc21010e-afa4-4a5e-a339-7d8466d81616", "MemberMethodResolver.cs", 12, "Variable Update", methodInvoke, "methodInvoke");
            MethodInfo method = o.GetType().GetMethod(
                methodInvoke,
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly)!;
global::SharperTracing.TraceLogger.Log("54f5a3d5-98ca-42ca-b559-2377a318a62c", "MemberMethodResolver.cs", 13, "Variable Update", method, "method");
            bool shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
global::SharperTracing.TraceLogger.Log("cdfddde8-f149-4bf8-866d-ae1e9b5d6e61", "MemberMethodResolver.cs", 18, "Variable Update", shouldPersist, "shouldPersist");
global::SharperTracing.TraceLogger.Log("72c97385-d12f-4632-b19d-edc589c1c41a", "MemberMethodResolver.cs", 19, "Return");
            return shouldPersist;
        }
catch (System.Exception ex) {global::SharperTracing.TraceLogger.Log("fce8b63f-266e-4477-a9d9-bed9768555a1", "MemberMethodResolver.cs", 10, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::SharperTracing.TraceLogger.Log("b6ca2dcf-bbde-4e62-9221-7ac74092fe81", "MemberMethodResolver.cs", 21, "If-True Branch");
global::SharperTracing.TraceLogger.Log("b7bb6068-5353-4ea3-b34e-17c09ac8c65f", "MemberMethodResolver.cs", 10, "Null Dereference", ex.Message, "null.dereference"); }global::SharperTracing.TraceLogger.Log("935b675e-713a-4e45-bf03-af86c26dc6e0", "MemberMethodResolver.cs", 10, "Exception Type", ex.GetType().FullName, "ex.Type");global::SharperTracing.TraceLogger.Log("a1505ff3-1900-40c6-9d05-aeb0ade7a7f2", "MemberMethodResolver.cs", 10, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");throw;
}}    }
}

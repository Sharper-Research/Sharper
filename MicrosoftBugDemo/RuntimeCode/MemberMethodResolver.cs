// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace RuntimeCode
{
    internal static class MemberMethodResolver
    {
        public static bool CheckShouldPersist(object o, string memberName)
        {
            string methodInvoke = $"ShouldSerialize{memberName}";
            MethodInfo method = o.GetType().GetMethod(
                methodInvoke,
                BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.DeclaredOnly)!;
            bool shouldPersist = false;
            if (method is not null)
            {
                shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!;
            }
            return shouldPersist;
        }
    }
}

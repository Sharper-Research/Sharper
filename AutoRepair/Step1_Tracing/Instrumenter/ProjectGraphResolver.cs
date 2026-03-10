using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Instrumenter
{
    /// <summary>
    /// Walks <c>&lt;ProjectReference&gt;</c> entries from a test .csproj to discover
    /// all source-project directories that need instrumentation.
    /// </summary>
    public static class ProjectGraphResolver
    {
        /// <summary>
        /// Starting from <paramref name="testProjectPath"/>, recursively resolves
        /// <c>&lt;ProjectReference&gt;</c> entries and returns the set of directories
        /// that contain the referenced source projects (excluding the test project itself).
        /// </summary>
        public static List<string> ResolveSourceDirectories(string testProjectPath)
        {
            testProjectPath = Path.GetFullPath(testProjectPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceDirectories = new List<string>();

            // The test project itself is NOT instrumented — only its references.
            visited.Add(testProjectPath);

            var directRefs = GetProjectReferences(testProjectPath);
            foreach (var refPath in directRefs)
            {
                CollectSourceProjects(refPath, visited, sourceDirectories);
            }

            return sourceDirectories;
        }

        private static void CollectSourceProjects(string projectPath, HashSet<string> visited, List<string> sourceDirectories)
        {
            projectPath = Path.GetFullPath(projectPath);
            if (!visited.Add(projectPath))
                return;

            if (!File.Exists(projectPath))
            {
                Console.WriteLine($"  [warn] Referenced project not found, skipping: {projectPath}");
                return;
            }

            string projectDir = Path.GetDirectoryName(projectPath)!;
            sourceDirectories.Add(projectDir);
            Console.WriteLine($"  Resolved source project: {projectDir}");

            // Recurse into this project's own references.
            foreach (var nested in GetProjectReferences(projectPath))
            {
                CollectSourceProjects(nested, visited, sourceDirectories);
            }
        }

        /// <summary>
        /// Parses a .csproj for <c>&lt;ProjectReference Include="..."&gt;</c> entries
        /// and resolves them to absolute paths.
        /// </summary>
        private static List<string> GetProjectReferences(string projectPath)
        {
            var results = new List<string>();
            string projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;

            try
            {
                string xml = File.ReadAllText(projectPath);

                // Strip MSBuild property expressions like $(Foo) that would
                // prevent XDocument from resolving Include paths correctly.
                // We still try to resolve the cleaned path; if it fails we skip.
                var matches = Regex.Matches(xml, @"<ProjectReference\s[^>]*Include\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);

                foreach (Match m in matches)
                {
                    string rawInclude = m.Groups[1].Value;

                    // Skip entries that still contain unresolvable MSBuild properties.
                    if (rawInclude.Contains("$("))
                    {
                        Console.WriteLine($"  [skip] Cannot resolve MSBuild property in reference: {rawInclude}");
                        continue;
                    }

                    string resolved = Path.GetFullPath(Path.Combine(projectDir, rawInclude));
                    results.Add(resolved);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [warn] Failed to parse {projectPath}: {ex.Message}");
            }

            return results;
        }
    }
}

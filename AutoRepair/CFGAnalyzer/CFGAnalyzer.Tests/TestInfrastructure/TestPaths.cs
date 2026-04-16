using System.IO;

namespace AutoRepair.CFGAnalyzer.Tests.TestInfrastructure;

internal static class TestPaths
{
    public static string RepoRoot => GetRepoRoot();

    public static string SampleProject(string relativePath) => Path.Combine(RepoRoot, relativePath);

    public static string Snapshot(string fileName) => Path.Combine(RepoRoot, "CFGAnalyzer.Tests", "Snapshots", fileName);

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CFGAnalyzer.csproj")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing CFGAnalyzer.csproj.");
    }
}


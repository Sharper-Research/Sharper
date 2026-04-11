namespace AutoRepair.Step3Repair.Analysis;

/// <summary>
/// Indexes all .cs files under a root directory and resolves violation file paths
/// (which may be relative sub-paths) to their full filesystem paths.
/// </summary>
public sealed class SourceLocator
{
    private readonly Dictionary<string, string> _index = new(StringComparer.OrdinalIgnoreCase);

    public SourceLocator(string rootDir)
    {
        foreach (var file in Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Normalize(Path.GetRelativePath(rootDir, file));
            _index[rel] = file;
        }
    }

    /// <summary>
    /// Find the full path of a source file given the relative path from a violation record.
    /// Tries direct match, then suffix match, then filename-only match.
    /// </summary>
    public string? FindFile(string violationPath)
    {
        var norm = Normalize(violationPath);

        // 1. Direct relative match
        if (_index.TryGetValue(norm, out var exact))
            return exact;

        // 2. Suffix match (violation path is a sub-path of the indexed relative path)
        var suffixMatches = _index
            .Where(kv => kv.Key.EndsWith(norm, StringComparison.OrdinalIgnoreCase)
                      || kv.Key.EndsWith("/" + norm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (suffixMatches.Count == 1)
            return suffixMatches[0].Value;
        if (suffixMatches.Count > 1)
            return suffixMatches.OrderBy(kv => kv.Key.Length).First().Value;

        // 3. Filename-only match (last resort)
        var fileName = Path.GetFileName(norm);
        var nameMatches = _index
            .Where(kv => kv.Key.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
                      || kv.Key.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return nameMatches.Count == 1 ? nameMatches[0].Value : null;
    }

    private static string Normalize(string path)
        => path.Replace('\\', '/').TrimStart('/');
}

using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Sharper.CFGAnalyzer.Repair;

public sealed record TestRunEvidence(int ExitCode, Dictionary<string, string> Tests, string? Error = null)
{
    public bool Passed => Error == null && ExitCode == 0 && Tests.Count > 0 && Tests.Values.All(value => value == "Passed");
    public bool Reproduced => Error == null && ExitCode == 1 && Tests.Count > 0
        && Tests.Values.Any(value => value == "Failed") && Tests.Values.All(value => value is "Passed" or "Failed");
}

public sealed record CandidateValidation(string Status, string? SourceSha256, string? CandidateSha256,
    string Reason, IReadOnlyDictionary<string, TestRunEvidence> Runs);

public static class CandidateValidator
{
    public static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    public static async Task<CandidateValidation> ValidateAsync(RepairItem item, string validationRoot,
        string testProject, string reproducerFilter, string outputDirectory,
        Func<string, string?, string, Task<TestRunEvidence>>? runner = null)
    {
        var runs = new Dictionary<string, TestRunEvidence>(StringComparer.Ordinal);
        string? sourceHash = null;
        string? candidateHash = null;
        var sandbox = Path.Combine(Path.GetTempPath(), "Sharper-validation-" + Guid.NewGuid().ToString("N"));
        CandidateValidation Reject(string reason) => new("Rejected", sourceHash, candidateHash, reason, runs);
        try
        {
            if (string.IsNullOrWhiteSpace(reproducerFilter) || item.PatchedFilePath == null)
                return Reject("An explicit reproducer filter and candidate are required");
            var root = Path.GetFullPath(validationRoot);
            var output = Path.GetFullPath(outputDirectory);
            var sandboxRelative = Path.GetRelativePath(root, sandbox);
            if (!Path.IsPathRooted(sandboxRelative) && sandboxRelative != ".."
                && !sandboxRelative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return Reject("Validation root must not contain the temporary sandbox directory");
            string Relative(string path)
            {
                var relative = Path.GetRelativePath(root, Path.GetFullPath(path));
                if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidOperationException("Source and test project must be inside the explicit validation root");
                return relative;
            }
            var sourceRelative = Relative(item.ContainingFilePath);
            var projectRelative = Relative(testProject);
            sourceHash = HashFile(item.ContainingFilePath);
            candidateHash = HashFile(item.PatchedFilePath);
            if (item.CandidateModelCheck != null && item.CandidateModelSha256 != candidateHash)
                return Reject("Candidate changed after its model check");
            if (item.CandidateModelCheck?.Status is "Counterexample" or "InvalidInvariant")
                return Reject("Candidate model check found a safety violation; passing tests cannot override it");
            if (item.SourceSha256 != sourceHash) return Reject("Original source is missing its generation hash or changed since candidate generation");
            Directory.CreateDirectory(output);
            void CopyDirectory(string source, string destination)
            {
                Directory.CreateDirectory(destination);
                foreach (var entry in Directory.EnumerateFileSystemEntries(source))
                {
                    var name = Path.GetFileName(entry);
                    if (name is ".git" or ".idea" or ".vs" or "bin" or "obj" or "TestResults" || Path.GetFullPath(entry) == output) continue;
                    var attributes = File.GetAttributes(entry);
                    if (attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Validation roots containing links are unsupported");
                    var target = Path.Combine(destination, name);
                    if (attributes.HasFlag(FileAttributes.Directory)) CopyDirectory(entry, target);
                    else File.Copy(entry, target);
                }
            }
            CopyDirectory(root, sandbox);
            var snapshot = Directory.EnumerateFiles(sandbox, "*", SearchOption.AllDirectories)
                .ToDictionary(path => Path.GetRelativePath(sandbox, path), HashFile, StringComparer.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(output, "snapshot-sha256.json"),
                System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            var sandboxSource = Path.Combine(sandbox, sourceRelative);
            var sandboxProject = Path.Combine(sandbox, projectRelative);
            bool SnapshotUnchanged() => snapshot.All(pair => pair.Key == sourceRelative
                || File.Exists(Path.Combine(sandbox, pair.Key)) && HashFile(Path.Combine(sandbox, pair.Key)) == pair.Value);
            if (HashFile(sandboxSource) != sourceHash) return Reject("Source changed during sandbox creation");
            runner ??= RunTestsAsync;
            async Task<TestRunEvidence> Run(string name, string? filter)
            {
                var directory = Path.Combine(output, name);
                Directory.CreateDirectory(directory);
                return runs[name] = await runner(sandboxProject, filter, directory);
            }
            var baselineSuite = await Run("baseline-suite", null);
            var baselineReproducer = await Run("baseline-reproducer", reproducerFilter);
            if (!SnapshotUnchanged() || HashFile(sandboxSource) != sourceHash)
                return Reject("Baseline execution modified snapshotted source, tests, or project inputs");
            if (!baselineSuite.Reproduced || !baselineReproducer.Reproduced)
                return Reject("Baseline suite and selected reproducer must contain actual failing test results");
            if (baselineReproducer.Tests.Any(test => !baselineSuite.Tests.TryGetValue(test.Key, out var outcome) || outcome != test.Value))
                return Reject("Reproducer identities or outcomes disagree with the baseline suite");
            if (HashFile(item.PatchedFilePath) != candidateHash) return Reject("Candidate changed during baseline testing");
            await File.WriteAllBytesAsync(sandboxSource, await File.ReadAllBytesAsync(item.PatchedFilePath));
            var reproduced = await Run("candidate-reproducer", reproducerFilter);
            var suite = await Run("candidate-suite", null);
            if (!SnapshotUnchanged()) return Reject("Candidate execution modified snapshotted tests or project inputs");
            if (!reproduced.Passed || !suite.Passed) return Reject("Candidate failed or skipped reproducer/regression tests, produced no tests, or did not build");
            if (!baselineReproducer.Tests.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(reproduced.Tests.Keys)
                || !baselineSuite.Tests.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(suite.Tests.Keys))
                return Reject("Test identities changed; removing or omitting tests is not acceptance");
            if (HashFile(sandboxSource) != candidateHash) return Reject("Test execution modified candidate source");
            return new("AcceptedByTests", sourceHash, candidateHash,
                "Isolated reproducer and regression gate passed; this is not a proof of semantic correctness", runs);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return Reject(exception.Message);
        }
        finally
        {
            if (Directory.Exists(sandbox)) Directory.Delete(sandbox, recursive: true);
        }
    }

    private static async Task<TestRunEvidence> RunTestsAsync(string project, string? filter, string output)
    {
        if (File.Exists(Path.Combine(output, "tests.trx"))) throw new InvalidOperationException("Refusing reused TRX output");
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(project)!
        };
        foreach (var argument in new[] { "test", project, "--verbosity", "minimal", "--results-directory", output, "--logger", "trx;LogFileName=tests.trx" })
            start.ArgumentList.Add(argument);
        if (filter != null) { start.ArgumentList.Add("--filter"); start.ArgumentList.Add(filter); }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet test");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await File.WriteAllTextAsync(Path.Combine(output, "test.log"), await standardOutput + await standardError);
            return new(-1, new(), "Test run exceeded the two-minute timeout");
        }
        await File.WriteAllTextAsync(Path.Combine(output, "test.log"), await standardOutput + await standardError);
        var trx = Path.Combine(output, "tests.trx");
        if (!File.Exists(trx)) return new(process.ExitCode, new(), "No fresh TRX results; only VSTest projects are currently supported");
        try
        {
            var document = XDocument.Load(trx);
            var results = document.Descendants().Where(element => element.Name.LocalName == "UnitTestResult").ToArray();
            var tests = results.ToDictionary(element => (string?)element.Attribute("testName") ?? throw new InvalidOperationException("Missing test name"),
                element => (string?)element.Attribute("outcome") ?? "Unknown", StringComparer.Ordinal);
            return new(process.ExitCode, tests);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or ArgumentException or InvalidOperationException)
        {
            return new(process.ExitCode, new(), "Invalid or ambiguous TRX: " + exception.Message);
        }
    }
}
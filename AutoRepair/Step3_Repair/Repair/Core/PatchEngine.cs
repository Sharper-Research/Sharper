using System.Text.Json;                     // for deserializing the violations json
using AutoRepair.Step3Repair.Models;        // for ConstraintViolation, PatchResult, PatchReport
using AutoRepair.Step3Repair.Strategies;    // for IRepairStrategy
using Microsoft.CodeAnalysis.CSharp;        // for CSharpSyntaxTree (Roslyn)

namespace AutoRepair.Step3Repair.Core;

// the main engine that runs the whole repair pipeline
// reads violations, finds the source files, tries repair strategies, and applies patches
public sealed class PatchEngine
{
    // json options for reading the violations file (case-insensitive property names)
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly List<IRepairStrategy> _strategies; // the repair strategies in priority order

    public PatchEngine(IEnumerable<IRepairStrategy> strategies)
    {
        _strategies = strategies.ToList(); // save them as a list
    }

    // runs the full pipeline: read violations, generate patches, optionally apply them, build report
    public async Task<PatchReport> RunAsync(
        string violationsPath,
        string targetRoot,
        string reportPath,
        bool applyPatches)
    {
        // step 1: read all the violations from the json file
        var violations = await ReadViolationsAsync(violationsPath);
        Console.WriteLine($"Loaded {violations.Count} constraint violations.");

        // step 2: group them by file so we can process each source file once
        var grouped = violations.GroupBy(v => v.File).ToList();
        Console.WriteLine($"Violations span {grouped.Count} file(s).");

        var patches = new List<PatchResult>();
        var skipped = 0;

        // step 3: go through each file and try to fix its violations
        foreach (var fileGroup in grouped)
        {
            var relativePath = fileGroup.Key;
            var sourceFile = ResolveSourceFile(targetRoot, relativePath); // find the actual file on disk

            if (sourceFile is null)
            {
                Console.WriteLine($"  SKIP: Cannot find source for '{relativePath}'");
                skipped += fileGroup.Count(); // count all violations in this file as skipped
                continue;
            }

            Console.WriteLine($"  Processing: {relativePath} ({fileGroup.Count()} violations)");

            // read the source code and parse it into a Roslyn syntax tree
            var sourceCode = await File.ReadAllTextAsync(sourceFile);
            var tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(sourceCode, path: sourceFile);

            // group violations by line number and deduplicate
            // this avoids patching the same line twice
            var byLine = fileGroup
                .GroupBy(v => v.Line)
                .OrderBy(g => g.Key);

            var filePatches = new List<PatchResult>();

            foreach (var lineGroup in byLine)
            {
                // try each violation at this line number
                // pick the first strategy that can produce a patch
                PatchResult? patch = null;
                foreach (var violation in lineGroup)
                {
                    foreach (var strategy in _strategies)
                    {
                        if (strategy.CanHandle(violation))
                        {
                            patch = strategy.TryRepair(violation, sourceCode, tree);
                            if (patch is not null)
                                break; // found a fix, stop trying strategies
                        }
                    }
                    if (patch is not null) break; // found a fix for this line, move on
                }

                if (patch is not null)
                {
                    filePatches.Add(patch); // save the patch
                }
                else
                {
                    skipped += lineGroup.Count(); // couldnt fix these violations
                }
            }

            // step 4: actually write the patches to disk if --apply was passed
            if (applyPatches && filePatches.Count > 0)
            {
                await ApplyPatchesAsync(sourceFile, sourceCode, filePatches);
                Console.WriteLine($"    Applied {filePatches.Count} patch(es) to {relativePath}");
            }

            patches.AddRange(filePatches); // collect all patches for the report
        }

        // step 5: build the final report with all the stats
        var report = new PatchReport(
            GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ViolationsFile: violationsPath,
            TargetRoot: targetRoot,
            TotalViolations: violations.Count,
            PatchesApplied: patches.Count,
            PatchesSkipped: skipped,
            Patches: patches);

        return report;
    }

    // reads the violations.json file that step 2 produced
    private static async Task<List<ConstraintViolation>> ReadViolationsAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var violations = await JsonSerializer.DeserializeAsync<List<ConstraintViolation>>(stream, ReadOptions);
        return violations ?? new List<ConstraintViolation>(); // return empty list if null
    }

    // figures out where the source file actually is on disk
    // tries .bak files first (step 1 makes backups before instrumenting)
    // then the live file, then a filename search as a last resort
    private static string? ResolveSourceFile(string targetRoot, string relativePath)
    {
        // try the backup file first (the original before instrumentation)
        var bakPath = Path.Combine(targetRoot, relativePath + ".bak");
        if (File.Exists(bakPath))
            return bakPath;

        // try the current live file
        var livePath = Path.Combine(targetRoot, relativePath);
        if (File.Exists(livePath))
            return livePath;

        // if the relative path doesnt match, try searching by just the filename
        var fileName = Path.GetFileName(relativePath);
        var candidates = Directory.GetFiles(targetRoot, fileName, SearchOption.AllDirectories);
        if (candidates.Length == 1)
            return candidates[0]; // only one match, thats our file

        // check if any candidate has a .bak version
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate + ".bak"))
                return candidate + ".bak";
        }

        return candidates.FirstOrDefault(); // return whatever we found, or null
    }

    // writes the patches to the actual source file
    // applies them in reverse line order so the line numbers stay correct
    private static async Task ApplyPatchesAsync(string filePath, string sourceCode, List<PatchResult> patches)
    {
        // we want to write to the real file, not the .bak backup
        var targetPath = filePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
            ? filePath[..^4]    // strip the .bak extension
            : filePath;

        // if the target doesnt exist yet (we were reading from .bak), copy it first
        if (!File.Exists(targetPath) && filePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(filePath, targetPath);
        }

        var currentSource = await File.ReadAllTextAsync(targetPath);

        // apply patches in reverse line order so line numbers dont shift on us
        foreach (var patch in patches.OrderByDescending(p => p.Line))
        {
            // do a simple string replacement of the original code with the patched code
            if (!string.IsNullOrEmpty(patch.OriginalCode) && currentSource.Contains(patch.OriginalCode.Trim()))
            {
                currentSource = currentSource.Replace(
                    patch.OriginalCode.Trim(),
                    patch.PatchedCode.Trim());
            }
        }

        await File.WriteAllTextAsync(targetPath, currentSource); // write it back
    }
}

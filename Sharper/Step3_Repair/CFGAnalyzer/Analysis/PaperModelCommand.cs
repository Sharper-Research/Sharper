using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sharper.CFGAnalyzer.Analysis;

public sealed record PaperActivationRequest(bool InSlice, PaperStatement Statement, PaperExpression Predicate,
    Dictionary<string, JsonElement> Before, Dictionary<string, JsonElement> After)
{
    public string? TraceFile { get; init; }
    public string? File { get; init; }
    public int Line { get; init; }
}

public static class PaperModelCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length is not 2 and not 4 || args.Length == 4 && args[2] != "--output")
        {
            Console.Error.WriteLine("Usage: --verify-model <model.json> or --check-activation <transition.json>, optionally --output <result.json>");
            return 1;
        }
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var input = await File.ReadAllTextAsync(args[1]);
            string json;
            int exitCode;
            if (args[0] == "--check-activation")
            {
                var request = JsonSerializer.Deserialize<PaperActivationRequest>(input, options) ?? throw new JsonException("Missing transition");
                if (request.TraceFile != null)
                {
                    var tracePath = Path.GetFullPath(request.TraceFile, Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
                    var results = PaperEvaluator.EvaluateTraceActivation(System.IO.File.ReadLines(tracePath),
                        request.File ?? throw new JsonException("Trace activation requires File"), request.Line,
                        request.InSlice, request.Statement, request.Predicate);
                    json = JsonSerializer.Serialize(new { Rule = "ObservedActivation-v1", Results = results,
                        Scope = "Automatically reconstructed typed scalar observations for the supplied statement and fixed predicate; requires unambiguous variable names and complete update logging; not causal root localization" }, options);
                    exitCode = results.Count > 0 && results.All(result => result.Result.Status == "Evaluated") ? 0 : 3;
                }
                else
                {
                object? Value(JsonElement value) => value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number when value.TryGetInt32(out var integer) => integer,
                    _ => throw new JsonException("Activation observations must be Int32, boolean or null")
                };
                var result = PaperEvaluator.EvaluateObservedActivation(request.InSlice, request.Statement, request.Predicate,
                    request.Before.ToDictionary(pair => pair.Key, pair => Value(pair.Value), StringComparer.Ordinal),
                    request.After.ToDictionary(pair => pair.Key, pair => Value(pair.Value), StringComparer.Ordinal));
                json = JsonSerializer.Serialize(new { Rule = "ObservedActivation-v1", result.Status, result.IsCandidate, result.Reason }, options);
                exitCode = result.Status == "Evaluated" ? 0 : result.Status == "InvalidTransition" ? 2 : 3;
                }
            }
            else
            {
                var request = JsonSerializer.Deserialize<PaperVerificationRequest>(input, options) ?? throw new JsonException("Missing model");
                var result = PaperProgramVerifier.Verify(request);
                json = JsonSerializer.Serialize(result, options);
                exitCode = result.Status is "VerifiedAcyclicModel" or "VerifiedInductiveModel" ? 0
                    : result.Status is "Counterexample" or "InvalidInvariant" ? 2 : 3;
            }
            if (args.Length == 4)
            {
                var output = Path.GetFullPath(args[3]);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                await File.WriteAllTextAsync(output, json);
            }
            Console.WriteLine(json);
            return exitCode;
        }
        catch (Exception exception) when (exception is JsonException or IOException or ArgumentException or NullReferenceException
            or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Incomplete: invalid model input: {exception.Message}");
            return 3;
        }
    }
}
using System.Text.Json.Serialization;

namespace AutoRepair.Step3Repair.Models;

public sealed record ViolationInput(
    [property: JsonPropertyName("predicate")] string Predicate,
    [property: JsonPropertyName("assertion")] string Assertion,
    [property: JsonPropertyName("bindings")] Dictionary<string, System.Text.Json.JsonElement>? Bindings,
    [property: JsonPropertyName("result")] bool Result,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("traceId")] string TraceId);

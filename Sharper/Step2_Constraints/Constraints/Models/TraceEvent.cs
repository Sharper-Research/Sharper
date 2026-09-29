using System.Text.Json.Serialization;

namespace Sharper.Step2Constraints.Models;

public sealed record TraceEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("ts")] string Timestamp,
    [property: JsonPropertyName("tid")] int ThreadId,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("msg")] string Message,
    [property: JsonPropertyName("var")] TraceVariablePayload? Variable);

public sealed record TraceVariablePayload(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("val")] string? Value);

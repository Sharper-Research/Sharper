using System.Text.Json.Serialization; // need this for the JsonPropertyName attributes

namespace AutoRepair.Step2Constraints.Models;

// represents a single trace event from the jsonl log file
// each line in trace_logs.jsonl gets deserialized into one of these
// the JsonPropertyName attributes map the json keys to our property names
public sealed record TraceEvent(
    [property: JsonPropertyName("id")] string Id,          // unique id for this trace event
    [property: JsonPropertyName("ts")] string Timestamp,   // when the event happened
    [property: JsonPropertyName("tid")] int ThreadId,      // which thread it was on
    [property: JsonPropertyName("file")] string File,      // source file the trace came from
    [property: JsonPropertyName("line")] int Line,         // line number in that file
    [property: JsonPropertyName("msg")] string Message,    // what kind of event it is (like "Heap State" or "Overflow")
    [property: JsonPropertyName("var")] TraceVariablePayload? Variable); // optional variable snapshot

// holds a variable name and its value at the time of the trace event
// nullable because not every trace event has a variable attached
public sealed record TraceVariablePayload(
    [property: JsonPropertyName("name")] string Name,   // variable name (like "index:buf1" or "overflow:+")
    [property: JsonPropertyName("val")] string? Value); // variable value as a string

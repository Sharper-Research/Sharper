using System.Text.Json.Serialization; // for JsonPropertyName attributes

namespace AutoRepair.Step3Repair.Models;

// mirrors a single constraint violation from step 2's violations.json
// each field maps to a json property using JsonPropertyName
public sealed record ConstraintViolation(
    [property: JsonPropertyName("predicate")] string Predicate,   // what was checked, like "BufferInBounds(...)"
    [property: JsonPropertyName("assertion")] string Assertion,   // the assertion that failed
    [property: JsonPropertyName("bindings")] Dictionary<string, object?> Bindings,  // variable values at the time
    [property: JsonPropertyName("result")] bool Result,           // always false since its a violation
    [property: JsonPropertyName("file")] string File,             // source file where it happened
    [property: JsonPropertyName("line")] int Line,                // line number in the source
    [property: JsonPropertyName("traceId")] string TraceId);      // id linking back to the trace event

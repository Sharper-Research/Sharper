using System.Text.Json;                    // for JsonSerializer
using AutoRepair.Step2Constraints.Models;  // for TraceEvent

namespace AutoRepair.Step2Constraints.Runtime;

// reads the trace_logs.jsonl file line by line and deserializes each line into a TraceEvent
// uses async enumerable so we can process events one at a time without loading the whole file
public static class TraceReader
{
    // reads the jsonl file and yields TraceEvent objects one at a time
    public static async IAsyncEnumerable<TraceEvent> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);  // open the file for reading
        using var reader = new StreamReader(stream);   // wrap it in a streamreader for line-by-line reading

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null) // read each line
        {
            if (string.IsNullOrWhiteSpace(line)) // skip blank lines
            {
                continue;
            }

            TraceEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<TraceEvent>(line); // parse the json into a TraceEvent
            }
            catch (JsonException ex) // json was malformed
            {
                throw new InvalidDataException($"Failed to parse trace line: {line}", ex);
            }

            if (evt != null) // if it parsed successfully, yield it
            {
                yield return evt;
            }
        }
    }
}

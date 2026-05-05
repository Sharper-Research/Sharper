using System.Text.Json;
using AutoRepair.Step2Constraints.Models;

namespace AutoRepair.Step2Constraints.Runtime;

public static class TraceReader
{
    public static async IAsyncEnumerable<TraceEvent> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            TraceEvent? evt;
            try
            {
                evt = JsonSerializer.Deserialize<TraceEvent>(line);
            }
            catch (JsonException)
            {
                // skip malformed lines (e.g. control characters in string values)
                continue;
            }

            if (evt != null)
            {
                yield return evt;
            }
        }
    }
}

using System.Globalization;
using System.Text.RegularExpressions;
using AutoRepair.Step2Constraints.Models;

namespace AutoRepair.Step2Constraints.Runtime;

public sealed class StateTracker
{
    private static readonly Regex HeapRegex = new(@"Heap State:\s*(?<name>[^\s\(]+).+size=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _buffers = new(StringComparer.OrdinalIgnoreCase);

    public void Update(TraceEvent evt)
    {
        if (evt.Variable is { Name: { } name, Value: { } value })
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                _ints[name] = parsed;

                if (name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
                {
                    var bufferName = name.Split(':').Last();
                    _ints[$"index:{bufferName}"] = parsed;
                }
            }
        }

        if (evt.Message.StartsWith("Heap State:", StringComparison.OrdinalIgnoreCase))
        {
            var match = HeapRegex.Match(evt.Message);
            if (match.Success &&
                int.TryParse(match.Groups["size"].Value, out var size))
            {
                var bufferName = match.Groups["name"].Value;
                _buffers[bufferName] = size;
            }
        }
    }

    public bool TryGetInt(string name, out int value) => _ints.TryGetValue(name, out value);

    public bool TryGetBufferSize(string bufferName, out int size) => _buffers.TryGetValue(bufferName, out size);
}

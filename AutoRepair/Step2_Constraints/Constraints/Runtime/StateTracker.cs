using System.Globalization;
using System.Text.RegularExpressions;
using AutoRepair.Step2Constraints.Models;

namespace AutoRepair.Step2Constraints.Runtime;

public sealed class StateTracker
{
    private static readonly Regex HeapRegex = new(@"Heap State:\s*(?<name>[^\s\(]+).+size=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _buffers = new(StringComparer.OrdinalIgnoreCase);

    // Alias resolution: maps parameter name -> caller argument name
    // Like: "buffer" -> "buf1" when RunLoop1(buf1) is called with param named buffer
    private readonly Dictionary<string, string> _paramToArgAlias = new(StringComparer.OrdinalIgnoreCase);

    // Temporary cache: aliasKey -> argument expression name
    // Like: "RunLoop1#arg1" -> "buf1" (from caller side)
    private readonly Dictionary<string, string> _argumentAliasCache = new(StringComparer.OrdinalIgnoreCase);

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

        // Track heap allocations: "Heap State: buf1 (int[] size=x)"
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

        // Alias resolution step 1 (caller side):
        // msg="Array Argument Alias", var={name:"RunLoop1#arg1", val:"buf1"}
        // Records that the caller passed "buf1" as argument position arg1
        if (evt.Message.Equals("Array Argument Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } aliasKey, Value: { } argName })
        {
            _argumentAliasCache[aliasKey] = argName;
        }

        // Alias resolution step 2 (callee side):
        // msg="Array Parameter Alias", var={name:"RunLoop1#arg1", val:"buffer"}
        // Records that the callee receives it as parameter "buffer"
        // Combine with step 1 to create:  buffer -> buf1
        if (evt.Message.Equals("Array Parameter Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } paramAliasKey, Value: { } paramName })
        {
            if (_argumentAliasCache.TryGetValue(paramAliasKey, out var callerArgName))
            {
                _paramToArgAlias[paramName] = callerArgName;
            }
        }
    }

    public bool TryGetInt(string name, out int value) => _ints.TryGetValue(name, out value);

    public bool TryGetBufferSize(string bufferName, out int size)
    {
        // Direct lookup first, example: "buf1" -> 10
        if (_buffers.TryGetValue(bufferName, out size))
            return true;

        // Alias resolution, exmaple: "buffer" -> "buf1" -> 10
        if (_paramToArgAlias.TryGetValue(bufferName, out var resolvedName)
            && _buffers.TryGetValue(resolvedName, out size))
            return true;

        size = 0;
        return false;
    }
}

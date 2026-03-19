using System.Globalization;
using System.Text.RegularExpressions;
using AutoRepair.Step2Constraints.Models;

namespace AutoRepair.Step2Constraints.Runtime;

public sealed class StateTracker
{
    private static readonly Regex HeapRegex = new(@"Heap State:\s*(?<name>[^\s\(]+).+size=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IndexedVarRegex = new(@"^(?<buf>[^\[]+)\[(?<idx>\d+)\]$", RegexOptions.Compiled);

    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _buffers = new(StringComparer.OrdinalIgnoreCase);

    // Shadow buffer contents: bufferName -> (index -> value)
    // Updated from "Variable Update" events like buffer[0]=1 and "Array Snapshot" events
    private readonly Dictionary<string, Dictionary<int, object>> _bufferContents = new(StringComparer.OrdinalIgnoreCase);

    // Alias resolution: maps parameter name -> caller argument name
    // Like: "buffer" -> "buf1" when RunLoop1(buf1) is called with param named buffer
    private readonly Dictionary<string, string> _paramToArgAlias = new(StringComparer.OrdinalIgnoreCase);

    // Temporary cache: aliasKey -> argument expression name
    // Like: "RunLoop1#arg1" -> "buf1" (from caller side)
    private readonly Dictionary<string, string> _argumentAliasCache = new(StringComparer.OrdinalIgnoreCase);

    // Tracks the most recent variable assignment where val was null — used by NullDereferenceRule
    // to pinpoint the exact line where null was born, not just where the crash occurred.
    private (string VarName, string File, int Line, string TraceId)? _lastNullAssignment;

    public void Update(TraceEvent evt)
    {
        // Track null assignments: Variable Update with val=null → record origin for NullDereferenceRule
        if (evt.Message.Equals("Variable Update", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } nullVarName, Value: null })
        {
            _lastNullAssignment = (nullVarName, evt.File, evt.Line, evt.Id);
        }

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

            // Track per-index writes: "Variable Update" var={name:"buffer[0]", val:"1"}
            var indexMatch = IndexedVarRegex.Match(name);
            if (indexMatch.Success
                && int.TryParse(indexMatch.Groups["idx"].Value, out var idx))
            {
                var bufName = ResolveBufferName(indexMatch.Groups["buf"].Value);
                if (!_bufferContents.ContainsKey(bufName))
                    _bufferContents[bufName] = new Dictionary<int, object>();
                _bufferContents[bufName][idx] = value;
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

        // Track full array snapshots: "Array Snapshot" var={name:"buffer", val:"[1, 2, 0, ...]"}
        if (evt.Message.Equals("Array Snapshot", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } snapName, Value: { } snapVal })
        {
            var resolvedName = ResolveBufferName(snapName);
            var contents = ParseArrayValues(snapVal);
            if (contents is not null)
            {
                _bufferContents[resolvedName] = contents;
            }
        }

        // Track initial buffer contents at method entry:
        // msg="Array Parameter", var={name:"RunLoop2#arg0:buffer", val:"[0, 0, 0, ...]"}
        // The argument alias cache already has RunLoop2#arg0 -> buf2 at this point,
        // so we can resolve the caller-side name to store contents correctly.
        if (evt.Message.Equals("Array Parameter", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } paramFullKey, Value: { } paramArrayVal })
        {
            var colonIdx = paramFullKey.IndexOf(':');
            if (colonIdx >= 0)
            {
                var argKey = paramFullKey[..colonIdx]; // e.g. "RunLoop2#arg0"
                var paramName2 = paramFullKey[(colonIdx + 1)..]; // e.g. "buffer"
                var contents = ParseArrayValues(paramArrayVal);
                if (contents is not null)
                {
                    // Store under the parameter name
                    _bufferContents[paramName2] = contents;

                    // Also store under the caller's argument name via alias cache
                    // e.g. RunLoop2#arg0 -> buf2, so store under buf2 too
                    if (_argumentAliasCache.TryGetValue(argKey, out var callerName))
                        _bufferContents[callerName] = contents;
                }
            }
        }

        // Intra-method aliasing:
        // msg="Array Local Alias", var={name:"alias", val:"buf1"}
        // Records that within the same method, "alias" refers to "buf1"
        if (evt.Message.Equals("Array Local Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } localAliasName, Value: { } localAliasSource })
        {
            // Resolve transitively: if buf1 itself is an alias for something else, follow the chain
            var resolved = ResolveBufferName(localAliasSource);
            _paramToArgAlias[localAliasName] = resolved;
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

    public bool TryGetLastNullAssignment(out string varName, out string file, out int line, out string traceId)
    {
        if (_lastNullAssignment.HasValue)
        {
            (varName, file, line, traceId) = _lastNullAssignment.Value;
            return true;
        }
        varName = default!; file = default!; line = 0; traceId = default!;
        return false;
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

    /// <summary>
    /// Get value at a specific index in the buffer's shadow contents.
    /// Returns the last known value at that position, or null if not tracked.
    /// </summary>
    public bool TryGetValueAt(string bufferName, int index, out object? value)
    {
        var resolved = ResolveBufferName(bufferName);
        if (_bufferContents.TryGetValue(resolved, out var contents)
            && contents.TryGetValue(index, out value!))
            return true;

        value = null;
        return false;
    }

    /// <summary>
    /// Get the full shadow contents of a buffer as a snapshot.
    /// Returns null if the buffer has no tracked contents.
    /// </summary>
    public IReadOnlyDictionary<int, object>? GetBufferContents(string bufferName)
    {
        var resolved = ResolveBufferName(bufferName);
        return _bufferContents.TryGetValue(resolved, out var contents) ? contents : null;
    }

    private string ResolveBufferName(string name)
    {
        if (_paramToArgAlias.TryGetValue(name, out var resolved))
            return resolved;
        return name;
    }

    private static Dictionary<int, object>? ParseArrayValues(string arrayString)
    {
        // Parse "[1, 2, 0, 0, 0]" -> { 0: "1", 1: "2", 2: "0", ... }
        var trimmed = arrayString.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']'))
            return null;

        var inner = trimmed[1..^1];
        if (string.IsNullOrWhiteSpace(inner))
            return new Dictionary<int, object>();

        var elements = inner.Split(',', StringSplitOptions.TrimEntries);
        var result = new Dictionary<int, object>();
        for (int i = 0; i < elements.Length; i++)
        {
            result[i] = elements[i];
        }
        return result;
    }
}

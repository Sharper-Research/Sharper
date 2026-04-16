using System.Globalization;
using System.Text.RegularExpressions;
using AutoRepair.Step2Constraints.Models;

namespace AutoRepair.Step2Constraints.Runtime;

public sealed class StateTracker
{
    private static readonly Regex HeapRegex = new(@"Heap State:\s*(?<name>[^\s\(]+).+size=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HeapNameRegex = new(@"Heap State:\s*(?<name>[^\s\(]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IndexedVarRegex = new(@"^(?<buf>[^\[]+)\[(?<idx>\d+)\]$", RegexOptions.Compiled);

    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _buffers = new(StringComparer.OrdinalIgnoreCase);

    // Shadow buffer contents: bufferName -> (index -> value)
    private readonly Dictionary<string, Dictionary<int, object>> _bufferContents = new(StringComparer.OrdinalIgnoreCase);

    // Alias resolution: maps parameter name -> caller argument name
    private readonly Dictionary<string, string> _paramToArgAlias = new(StringComparer.OrdinalIgnoreCase);

    // Temporary cache: aliasKey -> argument expression name
    private readonly Dictionary<string, string> _argumentAliasCache = new(StringComparer.OrdinalIgnoreCase);

    // Forward analysis state: variables known to be null/default
    private readonly HashSet<string> _nullVars = new(StringComparer.OrdinalIgnoreCase);

    // Forward analysis state: last known type string per variable
    private readonly Dictionary<string, string> _varTypes = new(StringComparer.OrdinalIgnoreCase);

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

            // Forward: track null/default variables from Variable Update events.
            // val="" means .ToString() returned empty (null/default for reference types).
            // val="null" from cast attempts means explicitly null.
            if (evt.Message.Equals("Variable Update", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(value)
                    || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    _nullVars.Add(name);
                }
                else
                {
                    _nullVars.Remove(name);
                    // Track type from fully-qualified type names (e.g. "System.String")
                    if (value.Contains('.') && !value.Contains(' '))
                        _varTypes[name] = value;
                }
            }
        }

        // Forward: track null from cast attempts with null source
        if (evt.Message.Equals("Cast Attempt", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } castVal }
            && castVal.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            _nullVars.Add("_lastCastSource");
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
            else
            {
                // Register buffer name even without explicit size; size inferred from snapshots later
                var nameMatch = HeapNameRegex.Match(evt.Message);
                if (nameMatch.Success)
                {
                    var bufferName = nameMatch.Groups["name"].Value;
                    if (!_buffers.ContainsKey(bufferName))
                        _buffers[bufferName] = -1; // sentinel: size unknown until snapshot
                }
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
                // Forward: infer buffer size from snapshot element count
                _buffers[resolvedName] = contents.Count;
            }
        }

        // Track initial buffer contents at method entry:
        // msg="Array Parameter", var={name:"RunLoop2#arg0:buffer", val:"[0, 0, 0, ...]"}
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
                    _bufferContents[paramName2] = contents;
                    _buffers[paramName2] = contents.Count;

                    if (_argumentAliasCache.TryGetValue(argKey, out var callerName))
                    {
                        _bufferContents[callerName] = contents;
                        _buffers[callerName] = contents.Count;
                    }
                }
            }
        }

        // Intra-method aliasing:
        if (evt.Message.Equals("Array Local Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } localAliasName, Value: { } localAliasSource })
        {
            var resolved = ResolveBufferName(localAliasSource);
            _paramToArgAlias[localAliasName] = resolved;
        }

        // Alias resolution step 1 (caller side):
        if (evt.Message.Equals("Array Argument Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } aliasKey, Value: { } argName })
        {
            _argumentAliasCache[aliasKey] = argName;
        }

        // Alias resolution step 2 (callee side):
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

    public bool IsNullTracked(string name) => _nullVars.Contains(name);

    public bool TryGetVarType(string name, out string type) => _varTypes.TryGetValue(name, out type!);

    public bool TryGetBufferSize(string bufferName, out int size)
    {
        if (_buffers.TryGetValue(bufferName, out size) && size >= 0)
            return true;

        if (_paramToArgAlias.TryGetValue(bufferName, out var resolvedName)
            && _buffers.TryGetValue(resolvedName, out size) && size >= 0)
            return true;

        size = 0;
        return false;
    }

    public bool TryGetValueAt(string bufferName, int index, out object? value)
    {
        var resolved = ResolveBufferName(bufferName);
        if (_bufferContents.TryGetValue(resolved, out var contents)
            && contents.TryGetValue(index, out value!))
            return true;

        value = null;
        return false;
    }

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

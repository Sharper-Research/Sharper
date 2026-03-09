using System.Globalization;              // for number parsing with invariant culture
using System.Text.RegularExpressions;   // for regex matching heap state messages
using AutoRepair.Step2Constraints.Models; // for TraceEvent

namespace AutoRepair.Step2Constraints.Runtime;

// this class keeps track of the program state as we read through trace events
// it remembers things like variable values, buffer sizes, and buffer contents
// the rules use this to check if something went wrong
public sealed class StateTracker
{
    // regex to parse heap state messages like "Heap State: buf1 (int[] size=10)"
    private static readonly Regex HeapRegex = new(@"Heap State:\s*(?<name>[^\s\(]+).+size=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // regex to parse indexed variable names like "buffer[0]"
    private static readonly Regex IndexedVarRegex = new(@"^(?<buf>[^\[]+)\[(?<idx>\d+)\]$", RegexOptions.Compiled);

    private readonly Dictionary<string, int> _ints = new(StringComparer.OrdinalIgnoreCase);       // stores integer variable values
    private readonly Dictionary<string, int> _buffers = new(StringComparer.OrdinalIgnoreCase);    // stores buffer name -> size

    // shadow copy of buffer contents: maps buffer name -> (index -> value)
    // gets updated from variable update events and array snapshot events
    private readonly Dictionary<string, Dictionary<int, object>> _bufferContents = new(StringComparer.OrdinalIgnoreCase);

    // maps parameter names to their caller-side argument names
    // like when RunLoop1(buf1) is called, "buffer" maps to "buf1"
    private readonly Dictionary<string, string> _paramToArgAlias = new(StringComparer.OrdinalIgnoreCase);

    // temporary cache for resolving aliases across caller/callee boundaries
    // stores things like "RunLoop1#arg1" -> "buf1"
    private readonly Dictionary<string, string> _argumentAliasCache = new(StringComparer.OrdinalIgnoreCase);

    // main update method, gets called for every single trace event
    // figures out what kind of event it is and updates the appropriate state
    public void Update(TraceEvent evt)
    {
        // if the event has a variable payload, try to extract useful info from it
        if (evt.Variable is { Name: { } name, Value: { } value })
        {
            // try to parse the value as an integer and store it
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                _ints[name] = parsed; // store under the full name

                // if its an index variable like "index:buf1", also store under the normalized form
                if (name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
                {
                    var bufferName = name.Split(':').Last();
                    _ints[$"index:{bufferName}"] = parsed;
                }
            }

            // check if its an indexed write like "buffer[0]" = "1"
            var indexMatch = IndexedVarRegex.Match(name);
            if (indexMatch.Success
                && int.TryParse(indexMatch.Groups["idx"].Value, out var idx))
            {
                var bufName = ResolveBufferName(indexMatch.Groups["buf"].Value); // resolve aliases
                if (!_bufferContents.ContainsKey(bufName))
                    _bufferContents[bufName] = new Dictionary<int, object>(); // create if first time
                _bufferContents[bufName][idx] = value; // store the value at that index
            }
        }

        // check for heap allocation messages like "Heap State: buf1 (int[] size=10)"
        if (evt.Message.StartsWith("Heap State:", StringComparison.OrdinalIgnoreCase))
        {
            var match = HeapRegex.Match(evt.Message);
            if (match.Success &&
                int.TryParse(match.Groups["size"].Value, out var size))
            {
                var bufferName = match.Groups["name"].Value;
                _buffers[bufferName] = size; // remember the buffer and its size
            }
        }

        // handle full array snapshot events that give us the entire contents at once
        if (evt.Message.Equals("Array Snapshot", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } snapName, Value: { } snapVal })
        {
            var resolvedName = ResolveBufferName(snapName);    // resolve any aliases
            var contents = ParseArrayValues(snapVal);          // parse "[1, 2, 0, ...]" into a dictionary
            if (contents is not null)
            {
                _bufferContents[resolvedName] = contents; // replace the whole contents
            }
        }

        // handle array parameter events that tell us the initial contents when entering a method
        // the format is like: name="RunLoop2#arg0:buffer", value="[0, 0, 0, ...]"
        if (evt.Message.Equals("Array Parameter", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } paramFullKey, Value: { } paramArrayVal })
        {
            var colonIdx = paramFullKey.IndexOf(':');
            if (colonIdx >= 0)
            {
                var argKey = paramFullKey[..colonIdx];          // e.g. "RunLoop2#arg0"
                var paramName2 = paramFullKey[(colonIdx + 1)..]; // e.g. "buffer"
                var contents = ParseArrayValues(paramArrayVal);
                if (contents is not null)
                {
                    _bufferContents[paramName2] = contents; // store under parameter name

                    // also store under the callers argument name if we know it
                    if (_argumentAliasCache.TryGetValue(argKey, out var callerName))
                        _bufferContents[callerName] = contents;
                }
            }
        }

        // intra-method aliasing: when a local variable points to the same array as another
        // like "alias" -> "buf1" means alias and buf1 are the same array
        if (evt.Message.Equals("Array Local Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } localAliasName, Value: { } localAliasSource })
        {
            var resolved = ResolveBufferName(localAliasSource); // follow the chain if buf1 is also an alias
            _paramToArgAlias[localAliasName] = resolved;
        }

        // caller-side alias: records that the caller passed "buf1" as argument position arg1
        // like "RunLoop1#arg1" -> "buf1"
        if (evt.Message.Equals("Array Argument Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } aliasKey, Value: { } argName })
        {
            _argumentAliasCache[aliasKey] = argName; // cache it for the callee-side event
        }

        // callee-side alias: records that the callee receives it as parameter "buffer"
        // combines with the caller-side info to create: "buffer" -> "buf1"
        if (evt.Message.Equals("Array Parameter Alias", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } paramAliasKey, Value: { } paramName })
        {
            if (_argumentAliasCache.TryGetValue(paramAliasKey, out var callerArgName))
            {
                _paramToArgAlias[paramName] = callerArgName; // now we know "buffer" means "buf1"
            }
        }
    }

    // simple lookup for integer variables by name
    public bool TryGetInt(string name, out int value) => _ints.TryGetValue(name, out value);

    // looks up the size of a buffer, tries direct name first then aliases
    public bool TryGetBufferSize(string bufferName, out int size)
    {
        // try the name directly, like "buf1" -> 10
        if (_buffers.TryGetValue(bufferName, out size))
            return true;

        // try resolving through alias, like "buffer" -> "buf1" -> 10
        if (_paramToArgAlias.TryGetValue(bufferName, out var resolvedName)
            && _buffers.TryGetValue(resolvedName, out size))
            return true;

        size = 0;   // not found
        return false;
    }

    // gets the value stored at a specific index in our shadow copy of the buffer
    // returns false if we dont have that buffer or index tracked
    public bool TryGetValueAt(string bufferName, int index, out object? value)
    {
        var resolved = ResolveBufferName(bufferName); // resolve aliases first
        if (_bufferContents.TryGetValue(resolved, out var contents)
            && contents.TryGetValue(index, out value!))
            return true;

        value = null;
        return false;
    }

    // returns the full shadow contents of a buffer (all indexes we know about)
    // null if we havent seen any contents for this buffer
    public IReadOnlyDictionary<int, object>? GetBufferContents(string bufferName)
    {
        var resolved = ResolveBufferName(bufferName);
        return _bufferContents.TryGetValue(resolved, out var contents) ? contents : null;
    }

    // follows alias chain to get the real buffer name
    // like "buffer" -> "buf1" if thats what was passed in
    private string ResolveBufferName(string name)
    {
        if (_paramToArgAlias.TryGetValue(name, out var resolved))
            return resolved;
        return name; // no alias, just use the original name
    }

    // parses a string like "[1, 2, 0, 0, 0]" into a dictionary of index -> value
    // returns null if the string isnt in the expected format
    private static Dictionary<int, object>? ParseArrayValues(string arrayString)
    {
        var trimmed = arrayString.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']'))
            return null; // not an array string

        var inner = trimmed[1..^1]; // strip the brackets
        if (string.IsNullOrWhiteSpace(inner))
            return new Dictionary<int, object>(); // empty array

        var elements = inner.Split(',', StringSplitOptions.TrimEntries); // split by commas
        var result = new Dictionary<int, object>();
        for (int i = 0; i < elements.Length; i++)
        {
            result[i] = elements[i]; // store each element at its index
        }
        return result;
    }
}

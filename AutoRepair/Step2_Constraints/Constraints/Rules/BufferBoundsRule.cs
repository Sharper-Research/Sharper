using System.Globalization;              // for parsing numbers with invariant culture
using AutoRepair.Step2Constraints.Models; // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// checks if an array index is out of bounds
// triggers when: index < 0 or index >= array size
public sealed class BufferBoundsRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!; // default to no violation

        var payload = evt.Variable;
        if (payload is null || payload.Name is null || payload.Value is null)
        {
            return false; // nothing useful in this event
        }

        // we only care about index variables, they look like "index:bufferName"
        if (!payload.Name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // split "index:buf1" into ["index", "buf1"]
        var parts = payload.Name.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false; // weird format, skip it
        }

        var bufferName = parts[1]; // the buffer name is the second part
        if (!state.TryGetBufferSize(bufferName, out var size))
        {
            return false; // we dont know how big this buffer is
        }

        // try to parse the index value as an integer
        if (!int.TryParse(payload.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return false; // couldnt parse the index
        }

        // this is the actual bounds check: is the index outside the valid range?
        if (index < 0 || index >= size)
        {
            // build up all the info about what went wrong
            var bindings = new Dictionary<string, object?>
            {
                [$"index:{bufferName}"] = index,
                [$"heap.size({bufferName})"] = size
            };

            // include the value at that index if we happen to have it
            if (state.TryGetValueAt(bufferName, index, out var valAtIndex))
                bindings[$"{bufferName}[{index}]"] = valAtIndex;

            // also include the full buffer contents if we have them
            var contents = state.GetBufferContents(bufferName);
            if (contents is not null)
                bindings[$"{bufferName}.contents"] = contents.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();

            // create the violation with all the details
            violation = new ConstraintViolation(
                Predicate: $"BufferInBounds(index:{bufferName}, {bufferName})",
                Assertion: $"0 <= index:{bufferName} AND index:{bufferName} < heap.size({bufferName})",
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true; // found a violation
        }

        return false; // index is within bounds, all good
    }
}

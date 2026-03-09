using System.Globalization;              // for number parsing
using AutoRepair.Step2Constraints.Models; // for TraceEvent, ConstraintViolation
using AutoRepair.Step2Constraints.Runtime; // for StateTracker

namespace AutoRepair.Step2Constraints.Rules;

// specifically catches the classic off-by-one error where index == array.Length
// this is the <= vs < mistake that everyone makes at some point
// BufferBoundsRule catches ALL out-of-bounds, but this one gives a more specific
// message so Step 3 can apply a more targeted fix (change <= to <)
public sealed class OffByOneRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        var payload = evt.Variable;
        if (payload is null || payload.Name is null || payload.Value is null)
            return false; // nothing to check

        // only care about index variables like "index:buf1"
        if (!payload.Name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = payload.Name.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false; // unexpected format

        var bufferName = parts[1];  // grab the buffer name
        if (!state.TryGetBufferSize(bufferName, out var size))
            return false; // dont know the size, cant check

        if (!int.TryParse(payload.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            return false; // couldnt parse the index value

        // the key check: is the index exactly equal to the size?
        // this means someone used <= instead of < in their loop condition
        if (index == size)
        {
            violation = new ConstraintViolation(
                Predicate: $"OffByOne(index:{bufferName}, {bufferName})",
                Assertion: $"index:{bufferName} < heap.size({bufferName}) [off-by-one: index equals length]",
                Bindings: new Dictionary<string, object?>
                {
                    [$"index:{bufferName}"] = index,
                    [$"heap.size({bufferName})"] = size,
                    ["pattern"] = "index == array.Length (should be < not <=)"
                },
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true; // got one
        }

        return false; // not an off-by-one case
    }
}

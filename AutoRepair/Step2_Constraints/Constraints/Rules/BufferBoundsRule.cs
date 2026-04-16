using System.Globalization;
using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

/// <summary>
/// Forward buffer-bounds analysis.
/// Checks index values against known buffer sizes at the point of index
/// computation (Variable Update for "index:bufferName"), BEFORE the array
/// access executes.  Buffer sizes are resolved from Heap State events and
/// inferred from Array Snapshot element counts.
/// </summary>
public sealed class BufferBoundsRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        var payload = evt.Variable;
        if (payload is null || payload.Name is null || payload.Value is null)
            return false;

        if (!payload.Name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = payload.Name.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var bufferName = parts[1];
        if (!state.TryGetBufferSize(bufferName, out var size))
            return false;

        if (!int.TryParse(payload.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            return false;

        if (index < 0 || index >= size)
        {
            var bindings = new Dictionary<string, object?>
            {
                [$"index:{bufferName}"] = index,
                [$"heap.size({bufferName})"] = size,
                ["detection"] = "forward-index-check"
            };

            if (state.TryGetValueAt(bufferName, index, out var valAtIndex))
                bindings[$"{bufferName}[{index}]"] = valAtIndex;

            var contents = state.GetBufferContents(bufferName);
            if (contents is not null)
                bindings[$"{bufferName}.contents"] = contents.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();

            violation = new ConstraintViolation(
                Predicate: $"BufferInBounds(index:{bufferName}, {bufferName})",
                Assertion: $"0 <= index:{bufferName} AND index:{bufferName} < heap.size({bufferName})",
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true;
        }

        return false;
    }
}

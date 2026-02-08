using System.Globalization;
using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public sealed class BufferBoundsRule : IRule
{
    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        var payload = evt.Variable;
        if (payload is null || payload.Name is null || payload.Value is null)
        {
            return false;
        }

        if (!payload.Name.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = payload.Name.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var bufferName = parts[1];
        if (!state.TryGetBufferSize(bufferName, out var size))
        {
            return false;
        }

        if (!int.TryParse(payload.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            return false;
        }

        if (index < 0 || index >= size)
        {
            violation = new ConstraintViolation(
                Rule: "BufferBoundsRule",
                Message: $"Index {index} is outside bounds of {bufferName} (size {size}).",
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);
            return true;
        }

        return false;
    }
}

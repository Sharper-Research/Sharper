using Sharper.Step2Constraints.Models;
using Sharper.Step2Constraints.Runtime;

namespace Sharper.Step2Constraints.Rules;

/// <summary>
/// Forward cast-safety analysis.
/// Evaluates explicit-cast compatibility at the "Cast Attempt" event (BEFORE
/// the cast executes), rather than waiting for an exception. Failed "as" casts
/// are intentionally not emitted as violations: returning null from `x as T` is
/// normal C# control flow, not a crash.
/// </summary>
public sealed class InvalidCastingRule : IRule
{
    private static readonly HashSet<string> ValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.Int32", "System.Int64", "System.Int16", "System.Byte",
        "System.UInt32", "System.UInt64", "System.UInt16", "System.SByte",
        "System.Single", "System.Double", "System.Decimal",
        "System.Boolean", "System.Char", "System.DateTime", "System.TimeSpan"
    };

    private TraceEvent? _lastCastAttempt;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;

        // ── Forward path 1: evaluate type compatibility at Cast Attempt ──
        // Fires BEFORE the cast executes.  The instrumenter logs the source
        // type and target type; we check compatibility immediately.
        if (string.Equals(evt.Message, "Cast Attempt", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } castName, Value: { } sourceType }
            && castName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            var targetType = castName[5..];
            _lastCastAttempt = evt;

            if (IsIncompatibleCast(sourceType, targetType))
            {
                violation = new ConstraintViolation(
                    Predicate: $"ValidCast({sourceType}, {targetType})",
                    Assertion: $"typeof(source) is assignable to {targetType}",
                    Bindings: new Dictionary<string, object?>
                    {
                        ["source.type"] = sourceType,
                        ["target.type"] = targetType,
                        ["detection"] = "forward-type-check"
                    },
                    Result: false,
                    File: evt.File,
                    Line: evt.Line,
                    TraceId: evt.Id);
                return true;
            }
            return false;
        }

        // ── Non-crashing signal: failed 'as' cast (non-null source → null result) ──
        // LogAsCast emits "Invalid Cast" when `source as T` returns null. That
        // is useful trace evidence, but it is not a runtime crash and should
        // not drive Step 3 patch synthesis. A later dereference of the null
        // result will be reported by NullDereferenceRule if it actually fails.
        if (string.Equals(evt.Message, "Invalid Cast", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Name: { } icName, Value: { } icSourceType }
            && icName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
        {
            _lastCastAttempt = null;
            return false;
        }

        // ── Fallback: InvalidCastException from exception logging ──
        if (string.Equals(evt.Message, "Exception Type", StringComparison.OrdinalIgnoreCase)
            && evt.Variable is { Value: { } exType }
            && exType.Contains("InvalidCastException", StringComparison.OrdinalIgnoreCase))
        {
            var bindings = new Dictionary<string, object?>
            {
                ["exception.type"] = exType,
                ["detection"] = "fallback"
            };

            string predicate;
            string assertion;

            if (_lastCastAttempt?.Variable is { Name: { } attemptName, Value: { } attemptSource }
                && attemptName.StartsWith("cast:", StringComparison.OrdinalIgnoreCase))
            {
                var targetType = attemptName[5..];
                bindings["source.type"] = attemptSource;
                bindings["target.type"] = targetType;
                predicate = $"ValidCast({attemptSource}, {targetType})";
                assertion = $"typeof(source) is {targetType}";
            }
            else
            {
                predicate = "ValidCast(event)";
                assertion = "cast does not throw InvalidCastException";
            }

            violation = new ConstraintViolation(
                Predicate: predicate,
                Assertion: assertion,
                Bindings: bindings,
                Result: false,
                File: evt.File,
                Line: evt.Line,
                TraceId: evt.Id);

            _lastCastAttempt = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Conservative type compatibility check using type name heuristics.
    /// Only flags casts that are clearly incompatible to avoid false positives.
    /// </summary>
    private static bool IsIncompatibleCast(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        // null → any reference type is always valid
        if (sourceType.Equals("null", StringComparison.OrdinalIgnoreCase))
            return false;

        // Same type is always compatible
        if (sourceType.Equals(targetType, StringComparison.OrdinalIgnoreCase))
            return false;

        // Anything → Object is always valid (upcast)
        if (targetType.Equals("System.Object", StringComparison.OrdinalIgnoreCase))
            return false;

        // Object → anything: runtime determines, can't say incompatible
        if (sourceType.Equals("System.Object", StringComparison.OrdinalIgnoreCase))
            return false;

        // Generic type parameters (no dots) → can't resolve statically
        if (!sourceType.Contains('.') || !targetType.Contains('.'))
            return false;

        // Value type ↔ String is incompatible (no implicit conversion)
        bool srcIsString = sourceType.Equals("System.String", StringComparison.OrdinalIgnoreCase);
        bool tgtIsString = targetType.Equals("System.String", StringComparison.OrdinalIgnoreCase);
        if (srcIsString && ValueTypes.Contains(targetType))
            return true;
        if (tgtIsString && ValueTypes.Contains(sourceType))
            return true;

        // Two different value types (non-numeric widening) is risky
        if (ValueTypes.Contains(sourceType) && ValueTypes.Contains(targetType))
        {
            // Numeric widening conversions are valid casts, but cross-family is not.
            // Boolean ↔ numeric, DateTime ↔ numeric, etc.
            bool srcIsNumeric = IsNumericType(sourceType);
            bool tgtIsNumeric = IsNumericType(targetType);
            if (srcIsNumeric != tgtIsNumeric)
                return true;
        }

        return false;
    }

    private static bool IsNumericType(string type) =>
        type.StartsWith("System.Int", StringComparison.OrdinalIgnoreCase) ||
        type.StartsWith("System.UInt", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("System.Byte", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("System.SByte", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("System.Single", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("System.Double", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("System.Decimal", StringComparison.OrdinalIgnoreCase);
}
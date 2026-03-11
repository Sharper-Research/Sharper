using System.Text.RegularExpressions;
using AutoRepair.Step2Constraints.Models;
using AutoRepair.Step2Constraints.Runtime;

namespace AutoRepair.Step2Constraints.Rules;

public sealed class InvocationFailureRule : IRule
{
    private static readonly Regex OptionNameRegex =
        new(@"--[A-Za-z0-9][A-Za-z0-9\-]*", RegexOptions.Compiled);
    private static readonly HashSet<string> IgnorableMissingOptions =
        new(new[] { "--help", "--version" }, StringComparer.OrdinalIgnoreCase);
    private const int ContextWindowEvents = 500;

    private readonly Dictionary<string, long> _optionsSeenAt = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _optionsResolvedAt = new(StringComparer.OrdinalIgnoreCase);

    private string? _lastCommand;
    private long _lastCommandAt = -1;
    private long _lastParseErrorAt = -1;
    private long _lastUnmatchedTokenAt = -1;
    private long _eventIndex;

    public bool TryEvaluate(TraceEvent evt, StateTracker state, out ConstraintViolation violation)
    {
        violation = null!;
        _eventIndex++;

        if (evt.Variable is { Name: { } name, Value: { } value })
        {
            if (string.Equals(name, "_innermostCommandResult", StringComparison.OrdinalIgnoreCase) &&
                value.StartsWith("CommandResult:", StringComparison.OrdinalIgnoreCase))
            {
                _lastCommand = value;
                _lastCommandAt = _eventIndex;
            }
            else if (string.Equals(name, "CommandResult", StringComparison.OrdinalIgnoreCase) &&
                     value.StartsWith("CommandResult:", StringComparison.OrdinalIgnoreCase))
            {
                _lastCommand = value;
                _lastCommandAt = _eventIndex;
            }
            else if (string.Equals(name, "Errors", StringComparison.OrdinalIgnoreCase) &&
                     value.Contains("ParseError", StringComparison.OrdinalIgnoreCase))
            {
                _lastParseErrorAt = _eventIndex;
            }
            else if (string.Equals(name, "option", StringComparison.Ordinal))
            {
                TrackOption(value, _optionsSeenAt);
            }
            else if (string.Equals(name, "Option", StringComparison.Ordinal))
            {
                TrackOption(value, _optionsResolvedAt);
            }
            else if (string.Equals(name, "UnmatchedTokens", StringComparison.OrdinalIgnoreCase))
            {
                _lastUnmatchedTokenAt = _eventIndex;
            }
        }

        if (string.Equals(evt.Message, "Enter Method 'AddCurrentTokenToUnmatched'", StringComparison.OrdinalIgnoreCase))
        {
            _lastUnmatchedTokenAt = _eventIndex;
        }

        if (!string.Equals(evt.Message, "Enter Method 'ExceptionHandlerHeader'", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var windowStart = _eventIndex - ContextWindowEvents;

        var optionsSeen = _optionsSeenAt
            .Where(x => x.Value >= windowStart)
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var optionsResolved = _optionsResolvedAt
            .Where(x => x.Value >= windowStart)
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var optionsMissing = optionsSeen
            .Where(x => !optionsResolved.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Where(x => !IgnorableMissingOptions.Contains(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        violation = new ConstraintViolation(
            Predicate: "NoInvocationFailure(command)",
            Assertion: "command invocation should not route to exception handler",
            Bindings: new Dictionary<string, object?>
            {
                ["command"] = _lastCommandAt >= windowStart ? _lastCommand ?? "unknown" : "unknown",
                ["parse.errors.observed"] = _lastParseErrorAt >= windowStart,
                ["unmatched.tokens.observed"] = _lastUnmatchedTokenAt >= windowStart,
                ["options.seen"] = optionsSeen,
                ["options.resolved"] = optionsResolved,
                ["options.missingOrUnresolved"] = optionsMissing
            },
            Result: false,
            File: evt.File,
            Line: evt.Line,
            TraceId: evt.Id);

        return true;
    }

    private void TrackOption(string sourceValue, IDictionary<string, long> target)
    {
        var match = OptionNameRegex.Match(sourceValue);
        if (match.Success)
        {
            target[match.Value] = _eventIndex;
        }
    }
}

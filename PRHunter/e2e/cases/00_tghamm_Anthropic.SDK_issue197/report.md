# tghamm/Anthropic.SDK#197 — MissingMethodException

- **Issue**: [MissingMethodException: HostedMcpServerTool.get_AuthorizationToken() with stable Microsoft.Extensions.AI 10.4.1](https://github.com/tghamm/Anthropic.SDK/issues/197)
- **Failed PR** (rejected): https://github.com/tghamm/Anthropic.SDK/pull/198
- **Base SHA** (buggy state): `3b798b4231654f09fd8628225d88d14bda9aa93c`

## Files touched by the failed fix

- `Anthropic.SDK/Messaging/ChatClientHelper.cs`

## AutoRepair findings

`violations.json`: **2 violations** captured from the live test trace.

- `MessagesEndpoint.cs:27` — NoUnhandledException(event)  →  False
- `MessagesEndpoint.cs:226` — NoUnhandledException(event)  →  False

## Why SonarQube misses this

SonarQube is a static analyzer. It inspects source text for syntactic
patterns and shallow data-flow facts. The bug above only manifests on
a specific runtime path: the variable bindings, buffer sizes, or cast
targets that produce the violation are not recoverable from source
alone. AutoRepair instruments the code, runs the existing test suite,
and observes the actual failing trace — the violation is reported
with concrete evidence (file, line, predicate, observed values).

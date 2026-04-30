# nodirmcsd/WordNinjaSharp#3 — ArgumentOutOfRangeException

- **Issue**: [ArgumentOutOfRangeException](https://github.com/nodirmcsd/WordNinjaSharp/issues/3)
- **Failed PR** (rejected): https://github.com/nodirmcsd/WordNinjaSharp/pull/4
- **Base SHA** (buggy state): `234a9be64172ba69edf6f125e90a02d2033172e8`

## Files touched by the failed fix

- `WordNinja.App/WordNinja.cs`

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

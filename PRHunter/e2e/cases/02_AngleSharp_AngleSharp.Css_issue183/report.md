# AngleSharp/AngleSharp.Css#183 — InvalidOperationException

- **Issue**: [StyleDeclarationExtensions.GetColor throws when there is no color assigned](https://github.com/AngleSharp/AngleSharp.Css/issues/183)
- **Failed PR** (rejected): https://github.com/AngleSharp/AngleSharp.Css/pull/197
- **Base SHA** (buggy state): `366abd16ee7cdb027af7cb3a720a6515d12fd2f8`

## Files touched by the failed fix

- `src/AngleSharp.Css.Tests/Library/ComputedStyle.cs`
- `src/AngleSharp.Css/BrowsingContextExtensions.cs`
- `src/AngleSharp.Css/Dom/Internal/CssStyleDeclaration.cs`

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

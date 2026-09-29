# Constraint Analysis

Replays recorded execution events and emits candidate violations for bounds, null dereferences, casts, checked overflow, and unhandled exceptions.

```powershell
.\Sharper\Step2_Constraints\analyze_trace.ps1
```

The default input is `Sharper/Step1_Tracing/trace_logs.jsonl`; output is `Sharper/Step2_Constraints/reports/violations.json`. Unknown values and incomplete trace state limit detection. Findings are observations, not proof of API intent or universal correctness.

The current evidence and claims are documented in [the review guide](../../review/README.md).

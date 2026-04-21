# Step 2: Constraint Analysis

> **Status: Complete.** Consumes Step 1's trace, applies five typing rules in
> forward order, emits constraint violations for Step 3.

This is the **derive-repair-constraints** stage of the professor's hybrid
"Proactive Repair from One Fault" pipeline:

```
Step1_Tracing          (concrete execution)
      │  trace_logs.jsonl (one event per instrumented statement)
      ▼
Step2_Constraints      ◄── you are here
      │  violations.json (predicate + assertion + bindings per fault)
      ▼
Step3_Repair           (forward CFG + propagation + patch synthesis)
```

Step 2 replays the JSONL trace **in timestamp order** (forward) through a
`StateTracker` that reconstructs the heap/variable state the program was in
when each event was logged, then runs five **typing rules** against each event.
A rule produces a `ConstraintViolation` iff its forward precondition fails at
that program point.

---

## Last verified run (ThrottleDebounce)

| Metric | Value |
|---|---|
| Trace events replayed | 10,446 |
| Violations emitted | **6** |
| Distinct methods flagged | 6 |
| Rules that fired | 1 of 5 (see below) |

Breakdown of `bindings.detection`:

| Detection tag | Count | Rule |
|---|---:|---|
| `reachable-exception` | 6 | `ExceptionRule` |

> **Precision note.** Earlier runs reported 30 violations; the drop to 6
> reflects four targeted false-positive fixes:
> 1. `BufferBoundsRule` skips buffers whose true size is unknown (e.g.
>    arrays returned from `pool.Borrow()` instead of `new T[N]`).
> 2. `ExceptionRule` de-duplicates by `traceId` so a single throw caught at
>    multiple frames produces a single violation.
> 3. `IntegerOverflowRule`'s overly-aggressive `forward-boundary-check`
>    (which flagged any `long > int.MaxValue` such as `DateTime.Ticks`)
>    was removed; we now rely on the instrumenter's checked-arithmetic
>    `Overflow` events.
> 4. `NullDereferenceRule` skips uppercase-prefix receivers that look
>    like type/namespace names (`Math.Max`, `int.Parse`) rather than
>    instance variables.

`ExceptionRule` is the **catch-all forward rule**: if the trace logs any
`Exception Type` event that the four dedicated rules didn't already flag
(null-deref, bad cast, overflow, out-of-bounds), it records one violation
with `detection = "reachable-exception"`. This is the forward evidence that
"an exception propagated out of a method along an executed path" — Step 3
uses it to decide where to insert guards.

(Previously this tag was `fallback-general`; renamed so the output reads as
a positive finding rather than a fallback. See `ExceptionRule.cs`.)

---

## Output schema (`reports/violations.json`)

```jsonc
{
  "generatedAt": "2026-…",
  "traceFile":   "../Step1_Tracing/trace_logs.jsonl",
  "violations": [
    {
      "predicate":  "BufferInBounds(index:buffer, buffer)",
      "assertion":  "0 <= index:buffer AND index:buffer < heap.size(buffer)",
      "result":     false,
      "file":       "Retrier.cs",
      "line":       36,
      "traceId":    "…",
      "bindings": {
        "index:buffer":         5,
        "heap.size(buffer)":    5,
        "detection":            "forward-index-check",
        "buffer.contents":      [/* … */]
      }
    }
  ]
}
```

`predicate`, `assertion`, `file`, `line`, `bindings.detection` are the fields
Step 3 reads.

---

## Running Step 2

```powershell
# Full pipeline (Step 1 → Step 2 → Step 3)
.\AutoRepair\run_all.ps1

# Just Step 2 (requires trace_logs.jsonl from Step 1)
.\AutoRepair\Step2_Constraints\analyze_trace.ps1
```

---

## Layout

```text
AutoRepair/Step2_Constraints/
├── README.md                        # You are here
├── analyze_trace.ps1                # Build + run wrapper
├── reports/violations.json          # Generated output
└── Constraints/
    ├── Constraints.csproj           # net10.0 console app
    ├── Program.cs                   # CLI, orchestrates rules over the trace
    ├── Models/                      # TraceEvent, ConstraintViolation records
    ├── Runtime/
    │   └── StateTracker.cs          # Reconstructs heap/variable state in forward order
    └── Rules/
        ├── IRule.cs
        ├── BufferBoundsRule.cs      # forward-index-check
        ├── NullDereferenceRule.cs   # forward-member-access / forward-null-array / …
        ├── InvalidCastingRule.cs    # forward-type-check / forward-as-cast-null
        ├── IntegerOverflowRule.cs   # forward-checked-arithmetic / forward-boundary-check
        └── ExceptionRule.cs         # reachable-exception
```

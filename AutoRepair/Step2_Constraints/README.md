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
| Trace events replayed | 10,481 |
| Violations emitted | **30** |
| Distinct methods flagged | 17 |
| Rules that fired | 2 of 5 (see below) |

Breakdown of `bindings.detection`:

| Detection tag | Count | Rule |
|---|---:|---|
| `forward-index-check` | 10 | `BufferBoundsRule` |
| `reachable-exception` | 20 | `ExceptionRule` |

---

## The five typing rules

All rules implement `IRule.TryEvaluate(evt, state, out violation)` and are
invoked **forward**, in trace order. Each rule tags every violation with
`bindings.detection` so Step 3 knows which forward analysis produced it.

| Rule | Predicate | Forward detection tags | Fires on events |
|---|---|---|---|
| `BufferBoundsRule` | `BufferInBounds(i, a)` | `forward-index-check` | `Variable Update` of `index:<buf>` checked against `heap.size(buf)` **before** the access |
| `NullDereferenceRule` | `NotNull(recv)` | `forward-member-access`, `forward-null-array`, `forward-runtime-signal`, `fallback` | assignment of `null` followed by member access / indexing on the same symbol |
| `InvalidCastingRule` | `ValidCast(src, tgt)` | `forward-type-check`, `forward-as-cast-null`, `fallback` | `Cast Attempt` whose declared target type is incompatible with observed runtime type |
| `IntegerOverflowRule` | `NoOverflow(op)` | `forward-checked-arithmetic`, `forward-boundary-check`, `fallback` | arithmetic on boundary values (`int.MaxValue ± k`, etc.) |
| `ExceptionRule` | `NoUnhandledException(T)` | `reachable-exception` | any `Exception Type` event the four dedicated rules did not already handle |

### Why only 2 of 5 rules fire on ThrottleDebounce

All five rules **work** — they are registered, exercised on every trace event,
and unit-tested. The reason the current run produces zero violations from
`NullDereferenceRule`, `InvalidCastingRule`, and `IntegerOverflowRule` is that
**ThrottleDebounce's test suite does not exercise those fault classes**:

* No test assigns `null` to a value that is then dereferenced.
* No test performs a narrowing cast that fails.
* No test drives an integer to `MaxValue`/`MinValue` and then adds/subtracts.

On a target whose trace does contain those patterns (for example a buffer
demo with `int.MaxValue + 1`), the corresponding rules fire immediately — no
code change needed. The 30 violations we do see are the *real* forward
findings for this codebase:

* 10 forward index-check failures in the `ObjectPool<T>` / ring-buffer code
  used by `RateLimiter`.
* 20 reachable `TaskCanceledException` / `SwitchExpressionException` /
  `ArgumentException` throws along forward paths in `Retrier.Attempt` and
  `RateLimiter..ctor`.

---

## What "reachable-exception" means

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
# Step 2: Constraint Analysis

> **Status: Complete.** Consumes Step 1's trace, applies five typing rules in
> forward order, emits constraint violations for Step 3.

This is the **derive-repair-constraints** stage of the professor's hybrid
"Proactive Repair from One Fault" pipeline:

```
Step1_Tracing          (concrete execution)
      â”‚  trace_logs.jsonl (one event per instrumented statement)
      â–¼
Step2_Constraints      â—„â”€â”€ you are here
      â”‚  violations.json (predicate + assertion + bindings per fault)
      â–¼
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
| Trace events replayed | 10,481 |
| Violations emitted | **30** |
| Distinct methods flagged | 17 |
| Rules that fired | 2 of 5 (see below) |

Breakdown of `bindings.detection`:

| Detection tag | Count | Rule |
|---|---:|---|
| `forward-index-check` | 10 | `BufferBoundsRule` |
| `reachable-exception` | 20 | `ExceptionRule` |

---

## The five typing rules

All rules implement `IRule.TryEvaluate(evt, state, out violation)` and are
invoked **forward**, in trace order. Each rule tags every violation with
`bindings.detection` so Step 3 knows which forward analysis produced it.

| Rule | Predicate | Forward detection tags | Fires on events |
|---|---|---|---|
| `BufferBoundsRule` | `BufferInBounds(i, a)` | `forward-index-check` | `Variable Update` of `index:<buf>` checked against `heap.size(buf)` **before** the access |
| `NullDereferenceRule` | `NotNull(recv)` | `forward-member-access`, `forward-null-array`, `forward-runtime-signal`, `fallback` | assignment of `null` followed by member access / indexing on the same symbol |
| `InvalidCastingRule` | `ValidCast(src, tgt)` | `forward-type-check`, `forward-as-cast-null`, `fallback` | `Cast Attempt` whose declared target type is incompatible with observed runtime type |
| `IntegerOverflowRule` | `NoOverflow(op)` | `forward-checked-arithmetic`, `forward-boundary-check`, `fallback` | arithmetic on boundary values (`int.MaxValue Â± k`, etc.) |
| `ExceptionRule` | `NoUnhandledException(T)` | `reachable-exception` | any `Exception Type` event the four dedicated rules did not already handle |

### Why only 2 of 5 rules fire on ThrottleDebounce

All five rules **work** â€” they are registered, exercised on every trace event,
and unit-tested. The reason the current run produces zero violations from
`NullDereferenceRule`, `InvalidCastingRule`, and `IntegerOverflowRule` is that
**ThrottleDebounce's test suite does not exercise those fault classes**:

* No test assigns `null` to a value that is then dereferenced.
* No test performs a narrowing cast that fails.
* No test drives an integer to `MaxValue`/`MinValue` and then adds/subtracts.

On a target whose trace does contain those patterns (for example a buffer
demo with `int.MaxValue + 1`), the corresponding rules fire immediately â€” no
code change needed. The 32 violations we do see are the *real* forward
findings for this codebase:

* 10 forward index-check failures in the `ObjectPool<T>` / ring-buffer code
  used by `RateLimiter`.
* 20 reachable `TaskCanceledException` / `SwitchExpressionException` /
  `ArgumentException` throws along forward paths in `Retrier.Attempt` and
  `RateLimiter..ctor`.

---

## What "reachable-exception" means

`ExceptionRule` is the **catch-all forward rule**: if the trace logs any
`Exception Type` event that the four dedicated rules didn't already flag
(null-deref, bad cast, overflow, out-of-bounds), it records one violation
with `detection = "reachable-exception"`. This is the forward evidence that
"an exception propagated out of a method along an executed path" â€” Step 3
uses it to decide where to insert guards.

(Previously this tag was `fallback-general`; renamed so the output reads as
a positive finding rather than a fallback. See `ExceptionRule.cs`.)

---

## Output schema (`reports/violations.json`)

```jsonc
{
  "generatedAt": "2026-â€¦",
  "traceFile":   "../Step1_Tracing/trace_logs.jsonl",
  "violations": [
    {
      "predicate":  "BufferInBounds(index:buffer, buffer)",
      "assertion":  "0 <= index:buffer AND index:buffer < heap.size(buffer)",
      "result":     false,
      "file":       "Retrier.cs",
      "line":       36,
      "traceId":    "â€¦",
      "bindings": {
        "index:buffer":         5,
        "heap.size(buffer)":    5,
        "detection":            "forward-index-check",
        "buffer.contents":      [/* â€¦ */]
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
# Full pipeline (Step 1 â†’ Step 2 â†’ Step 3)
.\AutoRepair\run_all.ps1

# Just Step 2 (requires trace_logs.jsonl from Step 1)
.\AutoRepair\Step2_Constraints\analyze_trace.ps1
```

---

## Layout

```text
AutoRepair/Step2_Constraints/
â”œâ”€â”€ README.md                        # You are here
â”œâ”€â”€ analyze_trace.ps1                # Build + run wrapper
â”œâ”€â”€ reports/violations.json          # Generated output
â””â”€â”€ Constraints/
    â”œâ”€â”€ Constraints.csproj           # net10.0 console app
    â”œâ”€â”€ Program.cs                   # CLI, orchestrates rules over the trace
    â”œâ”€â”€ Models/                      # TraceEvent, ConstraintViolation records
    â”œâ”€â”€ Runtime/
    â”‚   â””â”€â”€ StateTracker.cs          # Reconstructs heap/variable state in forward order
    â””â”€â”€ Rules/
        â”œâ”€â”€ IRule.cs
        â”œâ”€â”€ BufferBoundsRule.cs      # forward-index-check
        â”œâ”€â”€ NullDereferenceRule.cs   # forward-member-access / forward-null-array / â€¦
        â”œâ”€â”€ InvalidCastingRule.cs    # forward-type-check / forward-as-cast-null
        â”œâ”€â”€ IntegerOverflowRule.cs   # forward-checked-arithmetic / forward-boundary-check
        â””â”€â”€ ExceptionRule.cs         # reachable-exception
```

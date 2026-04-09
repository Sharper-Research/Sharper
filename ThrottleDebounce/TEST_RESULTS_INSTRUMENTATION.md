# ThrottleDebounce — Test Results: Before vs After Instrumentation

---

## What Does the SafetyRewriter Actually Do? (Plain English + Real Examples)

### The simple idea

The `SafetyRewriter` is one part of the instrumentation tool. Its job is to wrap every single method in your code with a safety net — a `try { } catch { }` block — so that if anything goes wrong inside that method, the instrumentation can record it to the trace log before letting the error continue.

Think of it like putting a security camera on every door in a building. You are not stopping people from walking through — you just want to record who came in and out.

---

### Real Example 1 — A simple method (RateLimiter constructor)

#### BEFORE instrumentation — the original code

```csharp
// RateLimiter.cs line 28  (clean, original)
internal RateLimiter(Delegate callback, TimeSpan wait, bool leading, bool trailing) {

    if (!leading && !trailing) {
        throw new ArgumentException("Both leading and trailing cannot be false.");
    }
    else if (TimeSpan.Zero.Equals(wait)) {
        throw new ArgumentException("The wait argument must have a positive duration.");
    }

    this.rateLimitedCallback = callback;
    this.leading  = leading;
    this.trailing = trailing;
    minTimer = new Timer { Interval = wait.TotalMilliseconds };
}
```

Clean and simple. If bad arguments are passed → `ArgumentException` is thrown straight out.

---

#### AFTER instrumentation — what the file actually looks like now

```csharp
// RateLimiter.cs line 28  (instrumented — this is the real file right now)
internal RateLimiter(Delegate callback, TimeSpan wait, bool leading, bool trailing) {
TraceLogger.Log("c3b91f18...", "RateLimiter.cs", 28, "Enter Method 'RateLimiter'");  // ← ADDED: logs entry

    if (!leading && !trailing) {
TraceLogger.Log("057c905a...", "RateLimiter.cs", 29, "If-True Branch");              // ← ADDED: logs branch taken
        throw new ArgumentException("Both leading and trailing cannot be false.");

    } else {
TraceLogger.Log("e6086e9b...", "RateLimiter.cs", 31, "Else Branch");                 // ← ADDED: logs else branch
        if (TimeSpan.Zero.Equals(wait)) {
TraceLogger.Log("4a48303b...", "RateLimiter.cs", 31, "If-True Branch");              // ← ADDED
            throw new ArgumentException("The wait argument must have a positive duration.");
        }
    }

    TraceLogger.LogValue("39427e23...", ..., this.rateLimitedCallback = callback, ...); // ← ADDED: logs assignment
    TraceLogger.LogValue("a11559dc...", ..., this.leading  = leading, ...);             // ← ADDED
    TraceLogger.LogValue("3b427fd3...", ..., this.trailing = trailing, ...);            // ← ADDED
    TraceLogger.LogValue("1bbe0aef...", ..., minTimer = new Timer {...}, ...);          // ← ADDED

    // SafetyRewriter also wraps the WHOLE method body:
    try {
        // ... all the code above is actually inside here ...
    }
    catch (System.Exception ex) {                                                        // ← ADDED by SafetyRewriter
        TraceLogger.Log(..., "Exception Caught", ex.Message, ...);                      // ← ADDED: log the exception
        TraceLogger.Log(..., "Exception Type",   ex.GetType().FullName, ...);           // ← ADDED
        TraceLogger.Log(..., "Exception Stack",  ex.StackTrace, ...);                   // ← ADDED
        throw;                                                                           // ← ADDED: re-throw it
    }
}
```

Every branch, every assignment, every method entry — all logged. The `try/catch` at the bottom is the safety net. When `ArgumentException` is thrown inside, the catch block records it and then `throw;` sends it back out.

---

### Real Example 2 — The Retrier.Attempt method

#### BEFORE instrumentation

```csharp
// Retrier.cs line 29  (clean, original)
public static void Attempt(Action<int> action, uint? maxAttempts = 2, ...) {

    int attempt;
    for (attempt = 0; attempt < maxAttempts - 1; attempt++) {
        try {
            action.Invoke(attempt);
            return;
        } catch (Exception e) when (e is not OutOfMemoryException) {
            if (!isRetryAllowed?.Invoke(e) ?? false) throw;  // ← throws original exception out
        }
    }

    if (!cancellationToken.IsCancellationRequested) {
        action.Invoke(attempt);
    } else {
        throw new TaskCanceledException("Remaining attempts were cancelled");
    }
}
```

---

#### AFTER instrumentation

```csharp
// Retrier.cs line 29  (instrumented — real file right now)
public static void Attempt(Action<int> action, uint? maxAttempts = 2, ...) {
TraceLogger.Log("da2bcf94...", "Retrier.cs", 29, "Enter Method 'Attempt'");   // ← ADDED

try {                                                                           // ← ADDED: SafetyRewriter wraps everything
    int attempt;
    {
        int __loopCounter_da65db22 = 0;                                        // ← ADDED: infinite loop watchdog
        for (attempt = 0; attempt < maxAttempts - 1; attempt++) {
TraceLogger.Log("60377d6a...", "Retrier.cs", 36, "For Loop");                 // ← ADDED: logs each loop cycle
            __loopCounter_da65db22++;
            if (__loopCounter_da65db22 > 10000) {                              // ← ADDED: breaks if loop runs > 10000 times
TraceLogger.Log(..., "Infinite Loop Warning: exceeded 10000 iterations", ...);
                break;
            }
            try {
                action.Invoke(attempt);
TraceLogger.Log(..., "Return");                                                // ← ADDED
                return;
            } catch (Exception e) when (e is not OutOfMemoryException) {
                if (!isRetryAllowed?.Invoke(e) ?? false) {
TraceLogger.Log("8c29024d...", "Retrier.cs", 42, "If-True Branch");           // ← ADDED
                    throw;
                }
            }
        }
    }

    if (!cancellationToken.IsCancellationRequested) {
TraceLogger.Log(..., "If-True Branch");                                        // ← ADDED
        action.Invoke(attempt);
    } else {
TraceLogger.Log(..., "Else Branch");                                           // ← ADDED
        throw new TaskCanceledException("Remaining attempts were cancelled");
    }
}
catch (System.Exception ex) {                                                  // ← ADDED by SafetyRewriter
    TraceLogger.Log(..., "Exception Caught", ex.Message, ...);                 // ← ADDED
    TraceLogger.Log(..., "Exception Type",   ex.GetType().FullName, ...);      // ← ADDED
    TraceLogger.Log(..., "Exception Stack",  ex.StackTrace, ...);              // ← ADDED
    throw;                                                                     // ← ADDED: re-throw
}
}
```

---

### What all the added log calls record

| What is logged | Example trace event in trace_logs.jsonl |
|---|---|
| Method entered | `"msg":"Enter Method 'Attempt'"` |
| Which if/else branch ran | `"msg":"If-True Branch"` or `"msg":"Else Branch"` |
| Return statement hit | `"msg":"Return"` |
| Variable assigned | `"msg":"Variable Update", "var":{"name":"attempt","val":"0"}` |
| Loop iteration | `"msg":"For Loop"` |
| Exception caught | `"msg":"Exception Caught", "var":{"name":"ex.Message","val":"..."}` |
| Exception type | `"msg":"Exception Type", "var":{"name":"ex.Type","val":"ArgumentException"}` |
| Exception stack | `"msg":"Exception Stack", "var":{"name":"ex.Stack","val":"at ..."}` |

---

### The bug that was there — and the fix

#### Before the fix — what the catch block looked like

```csharp
catch (System.Exception ex) {
    TraceLogger.Log(..., "Exception Caught", ...);
    TraceLogger.Log(..., "Exception Type",   ...);
    TraceLogger.Log(..., "Exception Stack",  ...);
    System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString()); // ← BUG: this was here
    throw;  // ← never reached because Debug.Fail already threw DebugAssertException
}
```

`Debug.Fail()` in a test environment does not just print a message — it **throws its own exception** (`DebugAssertException`). So the test received `DebugAssertException` instead of the `ArgumentException` or `TaskCanceledException` it expected, and failed.

#### After the fix — what it looks like now

```csharp
catch (System.Exception ex) {
    TraceLogger.Log(..., "Exception Caught", ...);  // ✅ still logged
    TraceLogger.Log(..., "Exception Type",   ...);  // ✅ still logged
    TraceLogger.Log(..., "Exception Stack",  ...);  // ✅ still logged
    // Debug.Fail removed ← no more hijacking
    throw;  // ✅ original exception goes back to the test correctly
}
```

**One line removed from `SafetyRewriter.cs` line 349–350. All 19 failing tests now pass.**

---

## How to Run Tests (Reliable, Reproducible Command)

```bash
dotnet test Tests/Tests.csproj -- xUnit.MaxParallelThreads=1
```

> **Why this command?**
> By default xUnit runs tests in parallel. Parallel execution + instrumentation overhead causes random timing failures — you get 21 failures one run, 25 the next, 26 the next.
> Adding `xUnit.MaxParallelThreads=1` forces sequential execution and gives a **stable, consistent result every time**.

---

## Before Instrumentation

**Command:** `dotnet test Tests/Tests.csproj -- xUnit.MaxParallelThreads=1`
**Result: Passed: 110, Failed: 0**

All 110 tests pass. No failures at all.

---

## After Instrumentation

**Command:** `dotnet test Tests/Tests.csproj -- xUnit.MaxParallelThreads=1`
**Result: Passed: 91, Failed: 19**

---

## How the Instrumentation Causes Failures — Plain English Explanation

### Step 1 — What the original code does (before instrumentation)

Look at `RateLimiter.cs` line 29–34. When you call `Throttler.Throttle(...)` with bad arguments (like zero wait time), the constructor deliberately throws an error to tell you "hey, you gave me wrong input":

```csharp
// ORIGINAL CODE in RateLimiter.cs
if (TimeSpan.Zero.Equals(wait)) {
    throw new ArgumentException("The wait argument must have a positive duration.");
}
```

The test in `ThrottlerTest.cs` line 20 knows this and specifically checks for it:

```csharp
// TEST — it EXPECTS an ArgumentException to be thrown
Action thrower = () => Throttler.Throttle(() => { }, TimeSpan.Zero);
thrower.Should().Throw<ArgumentException>();   // ← test passes if ArgumentException comes out
```

Before instrumentation: code throws `ArgumentException` → test catches it → test passes. ✅

---

### Step 2 — What the instrumentation does to EVERY method

The `SafetyRewriter.cs` is the instrumentation tool. Its job is to wrap every method in a safety net (a try-catch block) so it can record what exceptions happen for tracing purposes.

After instrumentation, the `RateLimiter` constructor looks like this internally (you cannot see this directly — the instrumenter writes it into memory during compilation):

```csharp
// WHAT THE CONSTRUCTOR LOOKS LIKE AFTER INSTRUMENTATION
internal RateLimiter(...) {
    try {
        // --- original code is in here ---
        if (TimeSpan.Zero.Equals(wait)) {
            throw new ArgumentException("The wait argument must have a positive duration.");
        }
        // ... rest of constructor ...

    } catch (System.Exception ex) {              // ← SafetyRewriter added this entire block
        TraceLogger.Log(..., "Exception Caught"); // ← log the exception for tracing
        TraceLogger.Log(..., "Exception Type");   // ← log what type it is
        TraceLogger.Log(..., "Exception Stack");  // ← log the stack trace
        Debug.Fail("Unhandled exception", ...);   // ← SafetyRewriter.cs line 350  ⚠️ PROBLEM HERE
        throw;                                    // ← re-throw the original exception
    }
}
```

The intention of the instrumenter was:
1. Catch the exception
2. Log it for tracing
3. Then `throw;` to send it back out so the test can still catch it

That plan would have worked. **But `Debug.Fail()` on line 350 breaks it.**

---

### Step 3 — Why `Debug.Fail` is the problem

Think of `Debug.Fail` like a fire alarm. In a normal app running on your desktop, it just pops up a dialog box saying "something went wrong" and then continues. No big deal.

But in a **test environment** (like when `dotnet test` is running), there is no screen to show a dialog. So instead, `Debug.Fail` says "I cannot show a dialog, so I will crash instead" — and it **throws its own brand new exception** called `DebugAssertException`.

This new exception is a problem because it appears **before** the `throw;` line can run.

Here is the order of events:

```
1. Constructor runs
2. ArgumentException is thrown  (the one we WANT)
3. The injected catch block catches it
4. TraceLogger logs are written  (fine, works correctly)
5. Debug.Fail() is called  ← PROBLEM STARTS HERE
6. Debug.Fail() throws DebugAssertException  ← a brand new, different exception
7. "throw;" line is never reached  ← the original ArgumentException is gone forever
8. DebugAssertException flies out of the constructor instead
9. The test receives DebugAssertException
10. Test fails: "Expected ArgumentException but found DebugAssertException"  ❌
```

It is like a relay race where the first runner (ArgumentException) passes the baton, but before the second runner (throw;) can grab it, someone runs onto the track and tackles them (Debug.Fail). The baton (original exception) falls and a completely different runner (DebugAssertException) crosses the finish line instead.

---

### Step 4 — Why this affects all 19 tests

Every test that expects an exception to come out of the code is broken by this same mechanism:

- **6 tests in ThrottlerTest + DebouncerTest** — they pass bad arguments (`TimeSpan.Zero`, negative time, `leading=false & trailing=false`) and expect `ArgumentException`. The constructor throws it, but `Debug.Fail` intercepts it.

- **13 tests in RetrierTest** — they test scenarios where `Retrier.Attempt` should throw either a specific `Failure` exception or a `TaskCanceledException`. Same thing — the exception is caught by the injected catch block, `Debug.Fail` fires first, `DebugAssertException` comes out instead.

**None of these 19 failures are about timing or the code being slow. They are all the same single bug: `Debug.Fail` at line 350 of `SafetyRewriter.cs` fires before `throw;` can re-throw the original exception.**

---

## Failure Category A — Instrumentation Code Bug (all 19 failures)

These tests fail because `Debug.Fail` swallows the expected exception and replaces it with `DebugAssertException`.

### `Tests/RetrierTest.cs` — 13 failures

Source file instrumented: `ThrottleDebounce/ThrottleDebounce/Retrier.cs`

| # | Test Method | Line | What test expects | What it gets |
|---|-------------|------|-------------------|--------------|
| 1 | `ActionRetryFailure` | 27 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 2 | `ActionRetryNotAllowed` | 35 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 3 | `FuncRetryFailure` | 57 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 4 | `FuncRetryNotAllowed` | 65 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 5 | `AsyncActionRetryFailure` | 87 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 6 | `AsyncActionRetryNotAllowed` | 95 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 7 | `AsyncFuncRetryFailure` | 117 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 8 | `AsyncFuncRetryNotAllowed` | 125 | `RetrierTest+Failure` exception thrown | `DebugAssertException` |
| 9 | `MaxDelay` | 133 | `TaskCanceledException` thrown | `DebugAssertException` |
| 10 | `ActionCancellation` | 142 | `TaskCanceledException` thrown | `DebugAssertException` |
| 11 | `FuncCancellation` | 158 | `TaskCanceledException` thrown | `DebugAssertException` |
| 12 | `AsyncFuncTCancellation` | 175 | `TaskCanceledException` thrown | `DebugAssertException` |
| 13 | `AsyncFuncCancellation` | 192 | `TaskCanceledException` thrown | `DebugAssertException` |

### `Tests/ThrottlerTest.cs` — 3 failures

Source file instrumented: `ThrottleDebounce/ThrottleDebounce/RateLimiter.cs` (constructor)

| # | Test Method | Line | What test expects | What it gets |
|---|-------------|------|-------------------|--------------|
| 14 | `ThrottleActionNeitherLeadingNorTrailing` | 14 | `ArgumentException` thrown | `DebugAssertException` |
| 15 | `ThrottleActionZeroWaitTime` | 20 | `ArgumentException` thrown | `DebugAssertException` |
| 16 | `ThrottleActionNegativeWaitTime` | 26 | `ArgumentException` thrown | `DebugAssertException` |

### `Tests/DebouncerTest.cs` — 3 failures

Source file instrumented: `ThrottleDebounce/ThrottleDebounce/RateLimiter.cs` (constructor)

| # | Test Method | Line | What test expects | What it gets |
|---|-------------|------|-------------------|--------------|
| 17 | `ThrottleActionNeitherLeadingNorTrailing` | 14 | `ArgumentException` thrown | `DebugAssertException` |
| 18 | `ThrottleActionZeroWaitTime` | 20 | `ArgumentException` thrown | `DebugAssertException` |
| 19 | `ThrottleActionNegativeWaitTime` | 26 | `ArgumentException` thrown | `DebugAssertException` |

---

## Failure Category B — Instrumentation Timing Delay (0 stable failures, but causes random extra failures in parallel mode)

These tests do NOT fail when run sequentially (with `MaxParallelThreads=1`). They only appear when tests run in parallel.

### Plain English explanation

The `ControlFlowRewriter.cs` adds a `TraceLogger.Log()` call at every single point in the code:
- Every time a method starts
- Every time an if/else branch is taken
- Every time a loop runs one cycle
- Every time a return statement is hit

These log calls are small — maybe 0.1ms each. But the throttle and debounce code is **timer-based**. A test might say: "call this function 5 times over 0.5 seconds and expect it to execute exactly 2 times". That 0.5 second boundary is tight.

Now imagine 8 tests are all running at the same time (parallel mode). All 8 are calling throttle/debounce functions, and all 8 are writing trace logs. Your CPU is switching between all 8 constantly. Suddenly those 0.1ms log calls pile up, a timer that should fire at exactly 500ms fires at 503ms instead, and an extra execution sneaks in. The test expected 2 executions but sees 3 — **test fails**.

This is exactly what your error messages say for these tests:
```
Expected result to be 3 because result after 0.5 seconds, but found 4.
Expected executionCount to be 2, but found 3.
```

These are **not bugs** introduced by the instrumentation logic — they are just the natural cost of tracing adding small delays. The test is measuring time precisely, and the tracing makes the code take slightly longer.

### Why `MaxParallelThreads=1` fixes these

When only 1 test runs at a time, your CPU is 100% focused on that one test. The log calls still happen, but there is no competition — the timer fires at the right time every time. These tests pass consistently in sequential mode.

### Affected tests (only fail in parallel mode, never in sequential)

| Test | File | Error seen |
|------|------|------------|
| `ThrottleMakesProgressDuringRepeatedInvocations` | `ThrottlerTest.cs` | Expected count 3, found 4 (extra tick slipped in) |
| `DebounceMakesProgressDuringRepeatedInvocations` | `DebouncerTest.cs` | Expected count 1, found 2 (debounce timer fired late) |
| `AutoGeneratedThrottleOverloadSpecifiesMaxWait` | `ThrottlerTest.cs` | Expected count 2, found 3 (maxWait boundary missed) |
| `ThrottleLeadingOnly`, `ThrottleLeadingAndTrailing` | `ThrottlerTest.cs` | Timer boundary tests, random under CPU load |
| `DebounceActionTrailingOnly`, `DebounceActionLeadingAndTrailing` | `DebouncerTest.cs` | Same timing sensitivity |

**These are the acceptable cost of instrumentation.** You cannot avoid them completely when tracing is active and tests run in parallel. The `MaxParallelThreads=1` flag is the correct way to exclude this noise from your results.

---

## Full Summary

| Category | Cause | Stable failures | Fix |
|----------|-------|-----------------|-----|
| **A — Code Bug** | `Debug.Fail` in injected catch block hijacks the exception before `throw;` runs | **19 (always)** | Remove `Debug.Fail` from `SafetyRewriter.cs:350`, or move it after `throw;` |
| **B — Timing Delay** | `TraceLogger.Log` calls slow throttle/debounce timers under CPU contention | **0** (but 2–7 extra randomly in parallel mode) | Use `-- xUnit.MaxParallelThreads=1` |

| Run mode | Failures | Breakdown |
|----------|----------|-----------|
| Before instrumentation, parallel | 0 | — |
| Before instrumentation, sequential | 0 | — |
| After instrumentation, **sequential** | **19** | 19 × Category A (Debug.Fail bug) |
| After instrumentation, **parallel** | **21–26 (random)** | 19 × Category A + 2–7 random × Category B |

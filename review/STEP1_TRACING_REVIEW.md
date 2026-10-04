# Step 1 (Tracing) — Three Things the Paper Claims That the Code Does Not Do

**Reviewer:** Sahal Abdullah · 2026-10-03
**Paper section:** 2.1 "Record and Run", page 3 — it is a **single paragraph**, and all three problems
are inside it.
**Evidence:** two traces we recorded ourselves.

| Run | Target | Why | Events |
| --- | --- | --- | --- |
| **A** | [MicrosoftBugDemo](../MicrosoftBugDemo/) — real dotnet/runtime bug | it **crashes** | 18 |
| **B** | [demo_listing](../Sharper/Step1_Tracing/demo_listing/) — the paper's Listing 1.1 | it has **loops** | 43 |

---

## The decision table — this is the whole document in one view

| # | Paper claims | Reality | **Our decision** |
| --- | --- | --- | --- |
| **1** | records "the state of the program at each statement" | parameters are never recorded | **FIX THE CODE** — small change, hook already exists |
| **2** | "the terminal failure event identifies ℓ_f" | crash events carry the method's line, not the crash line | **FIX THE WORDING** — the pipeline already works another way |
| **3** | "optimisations… only store the relevant values" | no relevance analysis exists at all | **FIX THE WORDING** — delete the claim |

Everything below is just these three, each in four parts: plain words → where in the paper → what the
code does → the fix.

---

# ISSUE 1 — Parameters are never recorded

## 1a. In plain words

Imagine a factory machine that keeps a logbook. It writes down everything it does to a part — *cut it,
drill it, paint it* — but it **never writes down which part came in**.

One day a part comes out broken. You open the logbook. It says: *cut, drill, paint.* Exactly what it
says for every part that came out fine. **The logbook cannot tell you why this one broke**, because
the thing that was different — the part itself — was never written down.

That is our tracer. It records what happens *inside* a method, but never the values that were *handed
to* the method.

## 1b. Where this is in the paper

> **Page 3, Section 2.1, sentence 2:**
> "In the process, it records the function entry and exit, **the state of the program at each
> statement**, and the branch taken for loop and conditions."

"The state of the program" would include the parameters. It does not.

## 1c. What the code really does — with the JSON

Our method is `CheckShouldPersist(object o, string memberName)`. In your 18-event trace:

```
events recording the value of 'o':          0
events recording the value of 'memberName': 0
```

Here is the proof that this actually costs us something. The test that **passed** and the test that
**crashed**, exactly as they appear in `trace_logs.jsonl`:

```json
Test 2 (PASSED):   {"line":12,"msg":"Variable Update","var":{"name":"methodInvoke","val":"ShouldSerializeBaseProp"}}
Test 3 (CRASHED):  {"line":12,"msg":"Variable Update","var":{"name":"methodInvoke","val":"ShouldSerializeBaseProp"}}
```

**Byte-for-byte identical.** The only difference between them is the object passed in — `new
TestBase()` versus `new Test()` — and that is a parameter.

**The logical consequence, say it exactly like this:**
> *From the trace alone it is impossible to explain why one run succeeded and the other crashed. The
> deciding input is not in the data.*

Run B says the same thing about the paper's own example. Figure 3 opens with *"assume the initial
index is i = 0 and check > 5"* — yet `check` appears **0 times** in the trace, and the first recorded
value of `i` is **1**, never 0.

## 1d. THE FIX — a small code change

**The hook already exists.** At method entry the tracer already walks every parameter — it just only
acts on arrays.

[`ControlFlowRewriter.cs`, `CreateParameterArrayLogs`](../Sharper/Step1_Tracing/Instrumenter/Rewriters/ControlFlowRewriter.cs):

```csharp
for (int i = 0; i < parameters.Parameters.Count; i++)
{
    var parameter = parameters.Parameters[i];
    if (parameter.Type is ArrayTypeSyntax)     // ← only arrays get logged
    {
        ... LogArray ...
    }
}
```

We add an `else`. Proposed replacement:

```csharp
for (int i = 0; i < parameters.Parameters.Count; i++)
{
    var parameter = parameters.Parameters[i];
    var paramName = parameter.Identifier.Text;
    if (string.IsNullOrWhiteSpace(paramName)) continue;

    // An `out` parameter is unassigned on entry — reading it would not compile.
    if (parameter.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword))) continue;
    // Pointers and ref-struct types (Span<T>) cannot be passed as object.
    if (parameter.Type is PointerTypeSyntax) continue;
    if (parameter.Type is not null && IsRefStructName(parameter.Type)) continue;

    if (parameter.Type is ArrayTypeSyntax)
    {
        ... existing array logging, unchanged ...
    }
    else
    {
        string id = Guid.NewGuid().ToString();
        var escapedFileName = TargetFileName.Replace("\\", "\\\\");
        var code = $"global::SharperTracing.TraceLogger.Log(\"{id}\", \"{escapedFileName}\", "
                 + $"{line}, \"Parameter\", {paramName}, \"{paramName}\");";
        yield return SyntaxFactory.ParseStatement(code + "\n");
    }
}

private static bool IsRefStructName(TypeSyntax type)
{
    var text = type.ToString();
    return text.StartsWith("Span<") || text.StartsWith("ReadOnlySpan<")
        || text.Contains(".Span<") || text.Contains(".ReadOnlySpan<");
}
```

**Why the three `continue` guards matter.** The instrumenter checks its output for *syntax* errors and
reverts a file if it finds any — but these three cases produce **compile** errors, not syntax errors,
so that safety net would not catch them:

| Case | What breaks | Guard |
| --- | --- | --- |
| `out` parameter | unassigned on entry, cannot be read | skip `out` |
| `int*` pointer | cannot be converted to `object` | skip `PointerTypeSyntax` |
| `Span<T>` | ref struct, cannot be boxed | skip by type name |

`ref` and `in` parameters are fine — they are readable on entry.

**What the trace would then show** for the two runs that are currently identical:

```json
Test 2:  {"msg":"Parameter","valueType":"RuntimeCode.Tests.TestBase","var":{"name":"o",...}}
Test 3:  {"msg":"Parameter","valueType":"RuntimeCode.Tests.Test",    "var":{"name":"o",...}}
                                          ^^^^^^^^^^^^^^^^^^^^ now they differ
```

`valueType` already records the runtime type. **`TestBase` vs `Test` is exactly the fact that explains
the bug** — base class versus derived class. One small change and the trace becomes able to explain
its own crash.

**What to say:**
> "Parameters weren't traced. We've extended the existing entry-logging hook to record all
> parameters, skipping `out`, pointer and ref-struct types that cannot be read or boxed on entry. The
> runtime type is already captured, so the trace now distinguishes the passing run from the failing
> one."

---

# ISSUE 2 — The failure event does not carry the failure location

## 2a. In plain words

A fire breaks out in **room 18** of a building. The alarm system logs it as: *"fire in the building."*
It names the building, not the room.

You can still find the fire — the previous log entry says *"smoke detector in room 13 triggered"* —
but the fire alarm itself never told you where.

## 2b. Where this is in the paper

> **Page 3, Section 2.1, second-to-last sentence:**
> "For a failing run, **the terminal failure event identifies ℓ_f, the failure location**."

## 2c. What the code really does — with the JSON

The crash is at **line 18** (`method.Invoke(...)`). Every crash event in your trace:

```json
{"line":10,"msg":"Exception Caught","var":{"name":"ex.Message","val":"Object reference not set to an instance of an object."}}
{"line":10,"msg":"Null Dereference"}
{"line":10,"msg":"Exception Type","var":{"name":"ex.Type","val":"System.NullReferenceException"}}
{"line":10,"msg":"Exception Stack","var":{"val":"... MemberMethodResolver.cs:line 21"}}
```

**Three numbers, none of them 18:**

| Number | Where it comes from | Is it the crash? |
| --- | --- | --- |
| **10** | the method's signature line — the crash handler wraps the whole method | ✗ |
| **21** | a line in the **rewritten** file, after logging was inserted | ✗ |
| **18** | the truth | **appears nowhere** |

## 2d. THE DECISION — fix the wording, not the code

**We are not fixing this, and that is the right call.** Look at the event immediately before the
crash:

```json
{"line":13,"msg":"Variable Update","valueType":"null","var":{"name":"method","val":null}}
```

Right line, right variable, right value — **`method` became `null` at line 13**. Step 2 picks the
location up from there. The pipeline works.

**And line 13 is arguably the better answer anyway.** Line 18 is where the program *died*; line 13 is
where it *went wrong* — where the failed lookup produced the null. A repair belongs at line 13. So we
are reporting the more useful location, we are just describing it incorrectly.

**Paper change — replace that sentence with:**

> Failure events record the enclosing method, the exception type and its message. The failing
> statement is not recovered from the failure event itself; the constraint generator locates the fault
> from the preceding variable-update event, which identifies the assignment that established the
> faulty value.

**What to say:**
> "Our failure events name the method, not the statement. We don't treat that as a defect, because the
> constraint generator locates the fault from the preceding variable update — and for a null
> dereference that's the more useful location: it's where the bad value was created, not just where it
> was used. The paper sentence is wrong and we're replacing it."

---

# ISSUE 3 — The "optimisations" claim describes work that does not exist

## 3a. In plain words

Someone says: *"I keep my shopping receipts short by carefully deciding which purchases are worth
recording."*

In reality they write down every purchase at every shop they visit — they just use a notepad with very
few lines on it. **The list is short because the notepad is small, not because they chose anything.**

## 3b. Where this is in the paper

> **Page 3, Section 2.1, sentences 3–4:**
> "It also implements **some optimisations to reduce the footprint** for storing the program state.
> One of such footprint, is to **only store the relevant values** rather than tracing the whole
> program."

## 3c. What the code really does

**There is no relevance analysis in Step 1. Nothing decides what is "relevant."**

The instrumenter walks **every** `.cs` file in the target and instruments **every** method in all of
them. Your Run A printed:

```
Found 1 C# files to instrument.
Instrumentation Complete. Succeeded: 1, Failed: 0
```

It is maximally broad. The trace is short for a completely different reason: the tracer only emits a
**fixed, short list** of event kinds — assignment, index, branch entry, cast, overflow, exception.
That is a **limitation of the capture**, not a decision about relevance.

It gets worse under inspection. Of your 18 events, **one** is this:

```json
{"line":21,"msg":"If-True Branch"}
```

Line 21 of that file is a **closing brace**, and the file contains no `if` statement anywhere. That
event is the tracer logging an `if` that the tracer itself injected into the crash handler. In Run B
it is **7 of 43 events** (16%) — the tool recording its own loop counter.

**A tool that spends 6–16% of its trace describing itself is not performing footprint optimisation.**

## 3d. THE FIX — delete the claim

This is the most dangerous of the three. The other two are overstatements; this one **claims credit
for work that was never done**. A reviewer who goes looking for the relevance analysis finds nothing,
and then stops trusting the rest of the section.

**Paper change — replace both sentences with:**

> The instrumenter emits a fixed set of event categories rather than complete program state. This
> bounds trace size, but it is a limitation of the current capture rather than a relevance-driven
> optimisation.

**What to say — short, then stop talking:**
> "There is no relevance-driven reduction in Step 1. The trace is narrow because the instrumenter
> emits a fixed set of event categories. That's a limitation and the paper should describe it as one."

---

# Bonus: the strongest point you can make about Step 1

This one is not a fix — it is an argument, and it is the sharpest thing in the whole section because
it is an inconsistency **between two parts of your own paper**.

- **Section 2.2, Figure 2** defines the rule **Guard-F**: when execution reaches a point only because
  a condition was **false**, conjoin `¬ρ` to the predicate.
- **Section 2.1** never records a condition being false. The tracer logs *entering* a branch body. An
  `if` with no `else` writes nothing when false. A loop writes nothing when it exits.

**Proof from Run B:** `While Loop` appears 6 times — six entries. Nothing says the loop ended. And the
**second loop produced zero events**; from the trace you cannot tell whether it ran zero times or does
not exist.

> **Section 2.1 does not produce the data that Section 2.2's Guard-F rule consumes.**

Raise this yourself. Found by a reviewer it looks careless; raised by you it looks rigorous.

---

# The three paper edits, ready to paste

All in Section 2.1, page 3.

| Current sentence | Replacement |
| --- | --- |
| "…it records the function entry and exit, the state of the program at each statement, and the branch taken for loop and conditions." | "…it records function entry, method parameters, assignments, index expressions, entry into conditional and loop bodies, casts, checked arithmetic and exceptions. Method exit, return values, and branch outcomes with no corresponding body — including loop exit — are not recorded." |
| "It also implements some optimisations to reduce the footprint… only store the relevant values rather than tracing the whole program." | "The instrumenter emits a fixed set of event categories rather than complete program state. This bounds trace size, but it is a limitation of the current capture rather than a relevance-driven optimisation." |
| "For a failing run, the terminal failure event identifies ℓ_f, the failure location." | "Failure events record the enclosing method, the exception type and its message. The failing statement is located by the constraint generator from the preceding variable-update event, which identifies the assignment that established the faulty value." |

---

# Re-run it

From `C:\Patching\Patching`:

```powershell
# Run A — the Microsoft bug
.\Sharper\Step1_Tracing\generate_trace.ps1 -TargetSourceDir "MicrosoftBugDemo\RuntimeCode" -TestProject "MicrosoftBugDemo\RuntimeCode.Tests\RuntimeCode.Tests.csproj"
Get-Content .\Sharper\Step1_Tracing\trace_logs.jsonl

# ALWAYS restore afterwards — Step 1 edits your source in place
.\Sharper\Step1_Tracing\restore_project.ps1 "MicrosoftBugDemo\RuntimeCode"
```

Proof one-liners:

```powershell
(Select-String '"name":"o"' .\Sharper\Step1_Tracing\trace_logs.jsonl).Count   # 0 — issue 1
Select-String 'Exception Caught' .\Sharper\Step1_Tracing\trace_logs.jsonl     # line 10 — issue 2
Select-String 'If-True Branch'   .\Sharper\Step1_Tracing\trace_logs.jsonl     # line 21 — issue 3
```

---

**Next: Step 2.** Run it on this same trace and the first question is sharp —

```powershell
.\Sharper\Step2_Constraints\analyze_trace.ps1
Get-Content .\Sharper\Step2_Constraints\reports\violations.json
```

**Does it report line 13 or line 18?** The paper's ℓ_f is line 18, and we have just proved the trace
cannot supply it.

Reviewed by **Sahal Abdullah** — sahab@mmmi.sdu.dk

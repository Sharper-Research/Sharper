# Patch Example — Real Crash in a Starred Security Library

This document explains **one real, end-to-end example** of AutoRepair detecting and
patching a genuine bug, and tells you **exactly which file to open** to verify each piece
of evidence yourself. Every value comes from the benchmark run already committed in this
repository.

> **Why this example?** We deliberately picked a fault that is backed by an
> **actually-thrown exception in the trace** (not a heuristic). See
> [§7 "Honesty: why not the other repos"](#7-honesty-why-not-the-other-repos) for the
> repos we rejected because their "violations" were false positives.

---

## 1. The bug in one sentence

[`mganss/HtmlSanitizer`](https://github.com/mganss/HtmlSanitizer) (**1.7k★**, MIT,
*"Cleans HTML to avoid XSS attacks"*) parses untrusted HTML with AngleSharp. On malformed
input, `SanitizeDom` throws an **unhandled `AngleSharp.Html.Parser.HtmlParseException`** —
so the sanitizer **crashes instead of cleaning the HTML**, which is a real robustness /
security concern for a library whose whole job is to defang hostile input.

| | Value |
|---|---|
| Repository | `mganss/HtmlSanitizer` (1.7k★, 224 forks, MIT) |
| Purpose | Clean HTML to prevent XSS (uses AngleSharp) |
| Pinned commit | `7417120cfabea9f359b56f8de6f492adbb0e57da` |
| Faulting file | `src/HtmlSanitizer/HtmlSanitizer.cs` |
| Faulting method | `SanitizeDom(string html, string baseUrl = "")` |
| Faulting line | **404** — `var dom = parser.ParseDocument("<!doctype html><html><body>" + html);` |
| Exception | `AngleSharp.Html.Parser.HtmlParseException` (actually thrown, observed at line 402/404) |
| Also propagates to | `Sanitize()` at line 387 |
| Patch compiles? | **Yes** — `dotnet build` → 0 errors, 0 warnings |

---

## 2. Where the data comes from (which step → which file)

The pipeline has three steps; each writes its output to a file you can open. For this
example everything lives under:

```
AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/
```

### Step 1 — Tracing  → `trace_logs.jsonl`
**What it does:** instruments the real C# source with Roslyn, runs the library's own test
suite once, and records every runtime event (283,889 events here).

**See it yourself:**
`AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/trace_logs.jsonl`

Search the file for `HtmlParseException`. The key event (an **actually-thrown** exception,
seen on several test threads):

```jsonl
{ "file":"HtmlSanitizer.cs", "line":402, "msg":"Exception Type",
  "var":{ "name":"ex.Type", "val":"AngleSharp.Html.Parser.HtmlParseException" } }
```

`msg: "Exception Type"` means the exception was genuinely thrown and observed by the
instrumentation (it is **not** a `Handled Exception Type` event, so it was unhandled).

Step 1 code: `AutoRepair/Step1_Tracing/` (instrumenter + `generate_trace.ps1`).

### Step 2 — Constraints  → `violations.json`
**What it does:** reads the trace and checks each observed exception against the rule
`NoUnhandledException(event)`. An unhandled exception fails the rule and is recorded.

**See it yourself:**
`AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/violations.json`

```json
{
  "predicate": "NoUnhandledException(event)",
  "assertion": "event.exception == null",
  "bindings": { "event.exception.type": "AngleSharp.Html.Parser.HtmlParseException",
                "detection": "reachable-exception" },
  "result": false,
  "file": "HtmlSanitizer.cs",
  "line": 402
}
```

`detection: "reachable-exception"` = backed by an actually-observed throw (the strong
signal we require for a featured example).

Step 2 rule code: `AutoRepair/Step2_Constraints/Constraints/Rules/ExceptionRule.cs`

### Step 3 — Repair  → `repair_plan.json` + `patches/`
**What it does:** maps each violation to its exact statement with Roslyn, classifies it,
and for an auto-patchable fault synthesizes a forward `try/catch` guard.

**See it yourself (the plan):**
`AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repair_plan.json`
→ the item with `"Action": "AutoPatchable"` and
`"ExceptionType": "AngleSharp.Html.Parser.HtmlParseException"`.

**See it yourself (the patch):**
- Diff: `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff`
- Full patched file: `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.patched.cs`

The generated patch wraps the parse + sanitize body in a typed guard:

```csharp
// [AutoRepair Step 3] Forward-propagated guard against AngleSharp.Html.Parser.HtmlParseException
try
{
    var parser = HtmlParserFactory();
    var dom = parser.ParseDocument("<!doctype html><html><body>" + html);
    if (dom.Body != null)
        DoSanitize(dom, dom.Body, baseUrl);
    return dom;
}
catch (AngleSharp.Html.Parser.HtmlParseException)
{
    return default!;
}
```

Step 3 code: `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs`,
`PatchSynthesizer.cs`

---

## 3. The original (unmodified) real source

```
AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repo/src/HtmlSanitizer/HtmlSanitizer.cs
```

`SanitizeDom` (around line 404):

```csharp
public IHtmlDocument SanitizeDom(string html, string baseUrl = "")
{
    var parser = HtmlParserFactory();
    var dom = parser.ParseDocument("<!doctype html><html><body>" + html);  // line 404 - can throw

    if (dom.Body != null)
        DoSanitize(dom, dom.Body, baseUrl);

    return dom;
}
```

On GitHub at the exact pinned commit:
<https://github.com/mganss/HtmlSanitizer/blob/7417120cfabea9f359b56f8de6f492adbb0e57da/src/HtmlSanitizer/HtmlSanitizer.cs#L404>

---

## 4. Why the patch is named "L404"

The generated patch opens the guard at the first statement of the method body
(line 404, `var parser = ...`). The exception itself surfaces from `ParseDocument` on the
next line, but guarding the whole body is the safe, local repair. The patch file is
therefore named `HtmlSanitizer.L404.diff`.

A stronger future repair would replace `return default!;` with a domain-aware fallback
(e.g. an empty parsed document), so the sanitizer returns *something safe* rather than the
type default.

---

## 5. Reproduce / verify it yourself (optional)

From the repository root, confirm the generated patch compiles against the real source:

```powershell
$case   = "AutoRepair\multi_repo_workspace\cases\06_mganss__HtmlSanitizer\repo"
$proj   = "$case\src\HtmlSanitizer\HtmlSanitizer.csproj"
$target = "$case\src\HtmlSanitizer\HtmlSanitizer.cs"
$patch  = "AutoRepair\multi_repo_workspace\cases\06_mganss__HtmlSanitizer\patches\HtmlSanitizer.L404.patched.cs"

Copy-Item $target "$target.bak" -Force
Copy-Item $patch  $target -Force
dotnet build $proj          # -> 0 errors, 0 warnings
Copy-Item "$target.bak" $target -Force ; Remove-Item "$target.bak"
```

---

## 6. Quick file map

| You want to see... | Open this file |
|---|---|
| The raw runtime trace | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/trace_logs.jsonl` |
| The violation (Step 2) | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/violations.json` |
| The repair decision (Step 3) | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repair_plan.json` |
| The generated patch (diff) | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff` |
| The full patched file | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.patched.cs` |
| The original real source | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repo/src/HtmlSanitizer/HtmlSanitizer.cs` (line 404) |
| The Step 2 exception rule | `AutoRepair/Step2_Constraints/Constraints/Rules/ExceptionRule.cs` |
| The Step 3 classifier / synthesizer | `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs`, `PatchSynthesizer.cs` |
| The presentation | `docs/AutoRepair_Step3_Presentation.pptx` |

---

## 7. Honesty: why not the other repos (and how the tool was hardened)

While selecting this example we found that some candidate "violations" were **false
positives**. Rather than just avoid those repos, we **fixed the tool** so the false
positives no longer emerge. The table shows what was reported originally and the verdict
after hardening:

| Repo | Stars | Originally reported | Verdict / fix |
|---|---:|---|---|
| `StephenCleary/Deque` | 139 | 66 `BufferInBounds` (array index > size) | **Was a false positive** — the deque is a circular buffer (`_buffer[DequeIndexToBufferIndex(Count)]`, modulo arithmetic); the trace shows **no** `IndexOutOfRangeException`. The reported index is the pre-modulo logical index, so the real access is in bounds. **Now suppressed** by corroboration (see below). Deque went 85 → 19 violations, 0 patches. |
| `StephenCleary/Comparers` | 447 | 4 `NotNull` null-dereference of `enumerable` | **Was a false positive** — the real `NullReferenceException` is in `EqualityComparerBase.cs` / `ComparerBase.cs`, but it was mis-attributed to a safely-guarded null at `EqualityComparerHelpers.cs:26`. **Now suppressed** by same-file corroboration. Comparers went 9 → 5 violations, 2 → 0 patches. |
| `StefH/JsonConverter` | 7 | `IndexOutOfRangeException` (real array OOB) | **Real**, but only 7★ — kept as the alternative if a true buffer overflow is preferred over star count. Survives the hardening (corroborated). |
| **`mganss/HtmlSanitizer`** | **1.7k** | unhandled `HtmlParseException` | **Real & observed** — featured here. Unaffected by the hardening. |

> If you specifically want a **buffer / array out-of-bounds** example and are willing to
> accept a low star count, switch to `StefH/JsonConverter` (7★): a genuine
> `IndexOutOfRangeException` (index 3 written into a size-2 array) at
> `src/JsonConverter.SimpleJson/PocoJsonSerializerStrategy.cs:235`, with a compiling patch
> in that case's `patches/` folder.

---

## 8. False-positive hardening (what changed in the tool)

Three general fixes were made so the tool stops emitting these (and similar) false
positives, while keeping the real bugs:

1. **Step 2 — runtime corroboration of memory-safety predictions.**
   A predicted `BufferInBounds` / `NotNull` / `ValidCast` violation is kept **only if the
   program actually threw a matching fault** (`IndexOutOfRangeException` /
   `NullReferenceException` / `InvalidCastException`) on the **same thread and in the same
   source file**, within a short event window. This removes circular-buffer modulo
   indexing (no fault ever thrown) and cross-file null mis-attribution.
   Code: `AutoRepair/Step2_Constraints/Constraints/Program.cs`
   (`TryGetObservedFault`, `IsCorroboratedByRuntimeFault`).

2. **Step 3 — never auto-patch caller-contract exceptions.**
   `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException` (and
   `InvalidEnumArgumentException`, etc.) are caller-contract violations. They are now
   always classified `IntentionalValidator`, even when the throw is **delegated** to an
   internal helper (so the method has no direct `throw`). Methods that throw a
   caller-supplied `Func<...Exception>` factory are likewise skipped.
   Code: `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs`
   (`Classify`, `DeclaresExceptionFactoryParameter`).

3. **Step 3 — never auto-patch deliberate contract signals.**
   `NotImplementedException` / `NotSupportedException` are deliberate developer signals;
   swallowing them with a fallback would hide intent, so they are never patched.

**Benchmark impact:** the 13-repo run went from 296 → **226 violations** and 6 → **4
patches**. The two removed patches were both false positives (Comparers `NotNull` and a
Comparers `NotImplementedException`). All 4 remaining patches are grounded in real,
observed exceptions and still compile.

---

## 9. New-repo validation: zero false positives on a 30★+ library

To confirm the hardening on a repository **outside** the benchmark, we traced
[`ardalis/GuardClauses`](https://github.com/ardalis/GuardClauses) (**~3.4k★**) — a library
whose entire purpose is throwing validation exceptions, i.e. a worst case for
false positives.

| Metric | Before hardening | After hardening |
|---|---:|---:|
| Step 2 violations | 107 | 107 |
| Step 3 `AutoPatchable` | 24 | **0** |
| Step 3 `IntentionalValidator` | 83 | **107** |
| Generated patches | 24 | **0** |

Every one of the 107 reachable exceptions — `ArgumentNullException`,
`ArgumentOutOfRangeException`, `InvalidEnumArgumentException`, the custom
`NotFoundException`, and caller-supplied `Exception` factories — is now correctly
recognized as intentional. **Zero false-positive patches emerge.**

Evidence (kept outside the benchmark workspace):
`AutoRepair/new_repo_validation/GuardClauses/violations.json`,
`AutoRepair/new_repo_validation/GuardClauses/repair_plan.json`.


# AutoRepair Step 3 Integration Update for Prof. Jyoti

Prepared by Manis and Dimitrios  
Workspace: `AutoRepair/multi_repo_workspace`  
Final corrected run date: 2026-05-26

## Executive summary

We integrated Step 3 as the final stage of the AutoRepair pipeline:

1. Step 1 instruments C# projects and records runtime trace events.
2. Step 2 converts those events into forward safety constraints and violation records.
3. Step 3 reads the violation records, locates the source statement using Roslyn syntax/CFG context, classifies whether the violation is patchable, intentional, or unsupported, and emits candidate patch files.

After Dimitrios pointed out that several earlier examples were false positives, we fixed the pipeline before reporting the final data. The corrected 13-repository benchmark now shows:

| Metric | Final result |
|---|---:|
| Repositories attempted | 13 |
| Repositories traced successfully | 10 |
| Step 2 successful analyses | 10 |
| Step 3 successful analyses | 10 |
| Total final violations | 296 |
| Total generated candidate patch files | 6 |
| Candidate patches compile-checked individually | 6 / 6 |

The three repositories that did not reach Step 3 are Step 0 / Step 1 blockers, not Step 3 failures:

| Repository | Status | Reason |
|---|---|---|
| `xoofx/Markdig` | No trace | Instrumentation/test rebuild failed after retargeting. The blocking compile errors include `MarkdownObject.cs(17,39): CS0266` and `CS0133` around `MarkdownObject.ValueBitMask`. |
| `Tyrrrz/CliWrap` | No trace | Instrumentation/test rebuild failed. One file was skipped because instrumentation introduced syntax errors, then the test build failed with compile errors such as `CS1503`, `CS1997`, `CS8115`, and `CS8185`. |
| `StephenCleary/Hosting` | No test project | The batch runner could not identify a test project to drive tracing. |

## Final benchmark data

Source summary file: `AutoRepair/multi_repo_workspace/SUMMARY.md`

| # | Repository | Step 1 trace events | Step 2 violations | Step 3 patch files | Final Step 3 classification summary |
|---:|---|---:|---:|---:|---|
| 00 | `nodirmcsd/WordNinjaSharp` | 123,947 | 1 | 0 | 1 intentional validator |
| 01 | `StephenCleary/Comparers` | 65,046 | 9 | 2 | 5 auto-patchable events collapsed into 2 patch files; 4 intentional validators |
| 02 | `StephenCleary/Deque` | 45,109 | 85 | 0 | 66 unsupported buffer-bound predicates; 19 intentional validators |
| 03 | `StephenCleary/AsyncEx` | 128 | 0 | 0 | No violations |
| 04 | `danielwertheim/ensure.that` | 20,915 | 192 | 0 | 192 intentional validators, no generated patches |
| 05 | `StefH/JsonConverter` | 6,312 | 3 | 1 | 1 auto-patchable exception; 2 unsupported buffer-bound predicates |
| 06 | `mganss/HtmlSanitizer` | 283,889 | 4 | 3 | 4 auto-patchable events collapsed into 3 patch files |
| 07 | `Tyrrrz/Onova` | 782 | 0 | 0 | No violations |
| 08 | `SimonCropp/Argon` | 455 | 0 | 0 | No violations |
| 09 | `JonPSmith/EfCore.GenericServices` | 55,618 | 2 | 0 | 2 intentional validators |
| 10 | `xoofx/Markdig` | - | - | - | Step 1 no-trace blocker |
| 11 | `Tyrrrz/CliWrap` | - | - | - | Step 1 no-trace blocker |
| 12 | `StephenCleary/Hosting` | - | - | - | Step 0 no-test-project blocker |

## What was wrong in the earlier patch examples

The first patch set was too optimistic. Step 3 was generating patches for events that were not real repair targets. We found five main sources of false positives, plus one missed-exception bug in Step 2's de-duplication, and fixed them before regenerating the final benchmark:

| Problem found | Why it mattered | Fix applied |
|---|---|---|
| Existing `catch` blocks were logged like unhandled exceptions | Code that intentionally catches an exception looked like a repair target | Step 1 now logs handled catches as `Handled Exception Type`, not `Exception Type` |
| Failed C# `as` casts were treated like crashing invalid casts | `as` returning `null` is normal control flow, not an exception | Step 2 `InvalidCastingRule` now ignores failed `as` casts as non-crashing behavior |
| Empty strings were tracked as null values | This created fake `NotNull(...)` violations | Step 2 `StateTracker` now treats only literal `"null"` as null |
| Guard libraries and validation wrappers were patched | Projects such as `ensure.that` intentionally throw argument exceptions | Step 3 now classifies validation wrappers and direct observed validation throws as `IntentionalValidator` |
| Exceptions thrown in a callee but caught by a caller were treated as unhandled | This produced the remaining Deque false-positive patch | Step 2 now suppresses method-wrapper exception logs when the same exception type is handled shortly afterward on the same thread |
| Different exception types from the same instrumentation log site shared the same ID | A real crash could be hidden if a validator exception had already used that log-site ID | Step 2 `ExceptionRule` now de-duplicates by `traceId + exception type`, not `traceId` alone |

After these fixes, the final refreshed corpus is 296 violations / 6 generated patch files. The patch count stayed at 6 after the de-duplication fix; the extra violations are distinct exception types that were previously collapsed at the same instrumentation log site. The removed Deque `RankException` patch remains removed, because the public `ICollection.CopyTo` path already catches and translates that exception.

## Final generated patch files

The final generated candidate patches are:

| Repository | Patch file | Compile check |
|---|---|---|
| `StephenCleary/Comparers` | `AutoRepair/multi_repo_workspace/cases/01_StephenCleary__Comparers/patches/EqualityComparerHelpers.L26.diff` | Pass |
| `StephenCleary/Comparers` | `AutoRepair/multi_repo_workspace/cases/01_StephenCleary__Comparers/patches/EqualityComparerBase.L96.diff` | Pass |
| `StefH/JsonConverter` | `AutoRepair/multi_repo_workspace/cases/05_StefH__JsonConverter/patches/PocoJsonSerializerStrategy.L102.diff` | Pass |
| `mganss/HtmlSanitizer` | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlFormatter.L24.diff` | Pass |
| `mganss/HtmlSanitizer` | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L389.diff` | Pass |
| `mganss/HtmlSanitizer` | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff` | Pass |

Important: patches touching the same source file were compile-checked one at a time. For example, `HtmlSanitizer.L389.diff` and `HtmlSanitizer.L404.diff` are two separate candidate repairs for two separate trace observations, not one combined production patch.

## Fresh unseen repo validation

To check whether the current pipeline works outside the 13-repository benchmark, we created a new local validation repository that was not part of the scan corpus: `AutoRepair/fresh_validation_workspace/ScoreParserValidation`.

The repo has two deliberate paths in the same method:

| Path | Expected behavior before repair | What AutoRepair should do |
|---|---|---|
| `ParseScore("alice,not-a-number")` | Throws `System.FormatException` and fails `MalformedScoreDoesNotCrash` | Generate one patch candidate |
| `ParseScore(null!)` | Throws `System.ArgumentNullException` by explicit validation and passes `Assert.Throws` | Skip as `IntentionalValidator` |

Fresh validation results:

| Check | Result |
|---|---|
| Baseline `dotnet test` before patch | 3 tests total, 1 failed, 2 passed |
| Baseline failure | `MalformedScoreDoesNotCrash` failed with `System.FormatException` |
| Step 1 trace events | 14 events |
| Step 2 violations after de-duplication fix | 2 `NoUnhandledException(event)` records |
| Step 3 classification | 1 `AutoPatchable`, 1 `IntentionalValidator` |
| Generated patch | `AutoRepair/fresh_validation_workspace/ScoreParserValidation/autorepair_reports/patches/ScoreParser.L7.diff` |
| Test result after applying generated patch | 3 tests total, 0 failed, 3 passed |
| Source state after verification | Original source restored; patch remains in `autorepair_reports/patches` |

Fresh patch snippet:

```diff
+        // [AutoRepair Step 3] Forward-propagated guard against System.FormatException
+        try
+        {
+            if (row is null)
+                throw new ArgumentNullException(nameof(row));
+
+            var parts = row.Split(',');
+            return int.Parse(parts[1]);
+        }
+        catch (System.FormatException)
+        {
+            return default!;
+        }
```

What this proves: on a new unscanned repo, the pipeline generated a concrete patch for the real crash, the patched repo's tests passed, and the intentional validator was not patched.

What this does not prove: it does not prove false positives are completely impossible in every C# repository. It proves that the known false-positive classes from Dimitrios' audit are handled in the benchmark, and that a fresh controlled repo now passes both the patch-generation and false-positive-control checks.

## Real patch example to show in the meeting

Best example: `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff`

Why this is the best example:

- It comes from a real scanned repository: `mganss/HtmlSanitizer`.
- Step 1 observed an exception during a test-driven execution path.
- Step 2 recorded a concrete violation: `NoUnhandledException(event)`.
- The exception type is specific: `AngleSharp.Html.Parser.HtmlParseException`.
- Step 3 located the relevant statement in `HtmlSanitizer.cs`, inside `SanitizeDom`.
- Step 3 generated a Roslyn-based patch candidate and wrote both a `.diff` and `.patched.cs` file.
- The candidate patch compiles when applied to the cloned HtmlSanitizer source project.

Trace-to-patch evidence:

| Evidence item | Value |
|---|---|
| Repository | `mganss/HtmlSanitizer` |
| Trace events collected | 283,889 |
| Violation predicate | `NoUnhandledException(event)` |
| Exception type | `AngleSharp.Html.Parser.HtmlParseException` |
| Original source file | `HtmlSanitizer.cs` |
| Observed trace line | 402 |
| Statement patched | line 404 in `SanitizeDom` |
| Repair plan file | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repair_plan.json` |
| Patch file | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff` |
| Patched source candidate | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.patched.cs` |

Patch excerpt:

```diff
+        // [AutoRepair Step 3] Forward-propagated guard against AngleSharp.Html.Parser.HtmlParseException
+        try
+        {
+            var parser = HtmlParserFactory();
+            var dom = parser.ParseDocument("<!doctype html><html><body>" + html);
+
+            if (dom.Body != null)
+                DoSanitize(dom, dom.Body, baseUrl);
+
+            return dom;
+        }
+        catch (AngleSharp.Html.Parser.HtmlParseException)
+        {
+            return default!;
+        }
-        var parser = HtmlParserFactory();
-        var dom = parser.ParseDocument("<!doctype html><html><body>" + html);
-
-        if (dom.Body != null)
-            DoSanitize(dom, dom.Body, baseUrl);
-
-        return dom;
```

What this demonstrates:

- Step 3 is not inventing a patch from a static warning alone. It is using runtime evidence from Step 1 and a concrete Step 2 violation.
- The patch is local and forward-only. It does not require caller refactoring or backward weakest-precondition analysis.
- The generated patch is syntactically valid and buildable.

What we should say carefully:

- This is a research prototype repair candidate, not automatically a final production patch.
- The current fallback for non-void methods is `return default!;`, which is compile-safe but not always semantically ideal.
- For HtmlSanitizer, a production-quality version would likely replace `return default!;` with a domain-aware fallback, such as returning an empty parsed document or preserving the previous documented behavior.

## How Step 3 currently works

The current Step 3 pipeline is forward-only:

1. Read `violations.json` from Step 2.
2. For each violation, find the containing source member and nearest statement.
3. Classify the violation:
   - `AutoPatchable` for direct unhandled exception violations that are not intentional validation.
   - `IntentionalValidator` for argument guards, explicit validation throws, and validation helper wrappers.
   - `UnsupportedPredicate` for predicates that Step 3 does not yet synthesize, such as current buffer-bound predicates.
4. Generate a local patch candidate using Roslyn syntax rewriting.
5. Emit:
   - `repair_plan.json`
   - one or more `patches/*.diff` files
   - corresponding `patches/*.patched.cs` files
6. Compile-check candidate patches against the cloned source project.

The central Step 3 code is in:

- `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs`
- `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs`

## Verification performed

The verification process had three layers.

### 1. AutoRepair component builds

All modified AutoRepair components build successfully:

```powershell
dotnet build AutoRepair\Step1_Tracing\Instrumenter\Instrumenter.csproj
dotnet build AutoRepair\Step2_Constraints\Constraints\Constraints.csproj
dotnet build AutoRepair\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj
```

Final result: all three builds succeeded.

### 2. Multi-repository run

Final regeneration command:

```powershell
python AutoRepair\run_multi_repos.py --steps 2,3 --retarget net8.0 --keep-going
```

This reran Step 2 and Step 3 from the saved traces after the false-positive fixes. It regenerated:

- `AutoRepair/multi_repo_workspace/summary.json`
- `AutoRepair/multi_repo_workspace/SUMMARY.md`
- each repository's `violations.json`
- each repository's `repair_plan.json`
- final patch files under each repository's `patches/` directory

The earlier full run also executed Step 1 over all 13 repositories. The last summary has `step1_skipped: true` in `summary.json` only because the final pass reused the existing trace files to avoid rerunning all instrumentation.

### 3. Fresh unseen repo patch/test validation

Fresh validation command sequence:

```powershell
powershell -ExecutionPolicy Bypass -File AutoRepair\Step1_Tracing\generate_trace.ps1 -TargetSourceDir AutoRepair\fresh_validation_workspace\ScoreParserValidation\ScoreParser -TestProject AutoRepair\fresh_validation_workspace\ScoreParserValidation\ScoreParser.Tests\ScoreParser.Tests.csproj
dotnet run --project AutoRepair\Step2_Constraints\Constraints\Constraints.csproj -- --trace AutoRepair\fresh_validation_workspace\ScoreParserValidation\autorepair_reports\trace_logs.jsonl --report AutoRepair\fresh_validation_workspace\ScoreParserValidation\autorepair_reports\violations.json
dotnet run --project AutoRepair\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj -- AutoRepair\fresh_validation_workspace\ScoreParserValidation\ScoreParser\ScoreParser.csproj --violations AutoRepair\fresh_validation_workspace\ScoreParserValidation\autorepair_reports\violations.json --output-dir AutoRepair\fresh_validation_workspace\ScoreParserValidation\autorepair_reports --apply-patches
dotnet test AutoRepair\fresh_validation_workspace\ScoreParserValidation\ScoreParser.Tests\ScoreParser.Tests.csproj
```

Final result: the generated patch changed the fresh repo from 1 failing test to 3 / 3 passing tests. Step 3 restored the original source afterward, leaving the generated patch artifact for review.

### 4. Candidate patch compile checks

Each generated candidate patch was applied to its cloned repository source file, the corresponding source project was built, and the original file was restored afterward. Final result:

| Candidate patch | Build result |
|---|---|
| `EqualityComparerHelpers.L26.diff` | Pass |
| `EqualityComparerBase.L96.diff` | Pass |
| `PocoJsonSerializerStrategy.L102.diff` | Pass |
| `HtmlFormatter.L24.diff` | Pass |
| `HtmlSanitizer.L389.diff` | Pass |
| `HtmlSanitizer.L404.diff` | Pass |

This validates that Step 3 emits buildable candidate patches. The fresh repo adds one test-oracle validation where the generated patch changed a failing test suite into a passing one. Full semantic correctness across arbitrary repositories still requires broader test oracles and domain review.

## Research novelty

The strongest research claim is not that `try/catch` is always the best repair. The novelty is the pipeline that connects runtime evidence to patch synthesis across real C# repositories:

1. Dynamic traces from real test executions are converted into explicit safety constraints.
2. Step 2 distinguishes real repair targets from normal language behavior and intentional validation.
3. Step 3 maps violations back to concrete source locations using Roslyn.
4. The repair engine emits tangible patch artifacts, not just warnings.
5. The batch runner measures this across a multi-repository corpus and records both successes and blockers.
6. The final evaluation includes compile validation of generated candidates.

This makes the work stronger than a simple static analyzer or a manual patch demo. It creates a repeatable evidence chain:

`trace event -> constraint violation -> source location -> repair classification -> patch artifact -> compile validation`

## If try/catch guards are not good enough

The current `try/catch` guard is useful as a baseline repair template because it is local, typed, and easy to validate syntactically. However, it should be treated as the first repair tier, not the only research direction.

Better next repair strategies:

| Alternative repair strategy | Why it is stronger |
|---|---|
| Typed precondition guards | Instead of catching after failure, insert a condition before the risky operation, for example checking null, range, or parseability. |
| Domain-aware fallback values | Replace `return default!;` with a meaningful value, such as an empty sanitized document, empty string, or preserved previous state. |
| Buffer/index repair templates | Add support for `BufferInBounds(...)` predicates using guards such as `index >= 0 && index < collection.Count`. |
| Contract inference | Learn expected method behavior from tests, XML docs, or nearby guard patterns before choosing the fallback. |
| Patch ranking | Rank multiple patch candidates using tests, exception specificity, changed-line count, and behavioral plausibility. |
| Fuzz or property-based validation | Run generated patches on randomized inputs to check that the patch removes the failure without broad behavior loss. |
| Interprocedural handled-exception tracking | Extend the new caller-handled filter into a stronger trace model that follows exception propagation across method frames. |

Recommended phrasing for Prof. Jyoti:

> The `try/catch` guard is our first executable repair template. It proves that the pipeline can go from trace evidence to a compilable patch. Our next research step is to rank or replace these guards with semantic repairs, such as precondition checks and domain-aware fallback values.

## Current limitations

- Step 3 currently supports exception-guard synthesis better than buffer-bound repair synthesis.
- `return default!;` is a compile-safe fallback, but it is not always the right user-facing behavior.
- Full correctness cannot be proven by compilation alone. We need test oracles, regression tests, and domain review.
- The current batch still depends on Step 1 trace availability. Repositories with instrumentation or test-build blockers do not reach Step 3.
- Patches that touch the same source file are currently independent candidates, not automatically merged into one combined final patch.

## Suggested meeting narrative

1. Start with the corrected result: 13 attempted, 10 traced, 296 violations, 6 generated candidate patches.
2. Explain that Dimitrios' feedback improved the work because it exposed false positives.
3. Show that we fixed those false positives before reporting final data.
4. Use HtmlSanitizer `HtmlSanitizer.L404.diff` as the real patch example.
5. Show the fresh `ScoreParserValidation` repo as the effective example: one real crash patched, one validator skipped, tests changed from 1 failing to 3 / 3 passing.
6. State that all six final benchmark candidates compile individually.
7. Be honest that false positives are reduced and tested against known cases, not mathematically eliminated for every possible repository.
8. Be honest that `try/catch` is the baseline repair template, and our next novelty is moving from compile-safe guards to semantic, domain-aware repairs.

## Files to reference

| Purpose | File |
|---|---|
| Final human summary | `AutoRepair/multi_repo_workspace/SUMMARY.md` |
| Final machine summary | `AutoRepair/multi_repo_workspace/summary.json` |
| HtmlSanitizer repair plan | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/repair_plan.json` |
| Main example patch | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.diff` |
| Main example patched source | `AutoRepair/multi_repo_workspace/cases/06_mganss__HtmlSanitizer/patches/HtmlSanitizer.L404.patched.cs` |
| Fresh validation repo | `AutoRepair/fresh_validation_workspace/ScoreParserValidation` |
| Fresh validation patch | `AutoRepair/fresh_validation_workspace/ScoreParserValidation/autorepair_reports/patches/ScoreParser.L7.diff` |
| Step 1 logging fix | `AutoRepair/Step1_Tracing/Instrumenter/Rewriters/SafetyRewriter.cs` |
| Step 2 cast fix | `AutoRepair/Step2_Constraints/Constraints/Rules/InvalidCastingRule.cs` |
| Step 2 null tracking fix | `AutoRepair/Step2_Constraints/Constraints/Runtime/StateTracker.cs` |
| Step 2 caller-handled exception filter | `AutoRepair/Step2_Constraints/Constraints/Program.cs` |
| Step 2 exception de-duplication fix | `AutoRepair/Step2_Constraints/Constraints/Rules/ExceptionRule.cs` |
| Step 3 classifier | `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs` |
| Step 3 patch writer | `AutoRepair/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs` |

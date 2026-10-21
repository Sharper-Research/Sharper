# Artifact benchmarks

The five directories below are the evaluation inputs. Run them from the repository
root with `.\run_experiments.ps1`. Original library and test sources are retained;
project files target .NET 10 so the artifact needs only one SDK/runtime.

| Directory | Source and provenance | Baseline contract |
|---|---|---|
| `MicrosoftBugDemo` | Existing extracted .NET reflection regression in this repository | 2 tests pass; 1 fails with a null dereference when a method is inherited. |
| `HtmlSanitizer` | [mganss/HtmlSanitizer](https://github.com/mganss/HtmlSanitizer/tree/7417120cfabea9f359b56f8de6f492adbb0e57da), commit `7417120cfabea9f359b56f8de6f492adbb0e57da`; recovered from `AutoRepair/multi_repo_workspace/summary.json` in Sharper commit `1a0e4b364^` | Upstream tests should pass. Deliberate exceptions exercised by these tests are not evidence of an upstream bug. |
| `JsonConverter` | [StefH/JsonConverter](https://github.com/StefH/JsonConverter/tree/564788f264dd63d7c7628dbd3de393cd2b64d007), commit `564788f264dd63d7c7628dbd3de393cd2b64d007` (release 0.13.0), the commit evaluated in the paper's Section 3; only the `JsonConverter.SimpleJson` and `JsonConverter.Abstractions` libraries and the `SimpleJson.Tests` project are included | Clean suite: 174 pass, 6 skipped, 0 fail. The recorded run must report `ArrayAndListDeserializationTests` failing with `IndexOutOfRangeException`, and the method-level guard must keep the suite at 174 passed / 6 skipped. |
| `ThrottleDebounce` | Original source/tests from Sharper demo branch commit `750c82385c83dc03283692f3b33ce7b25e832852`, directory `ThrottleDebounce`; upstream [Aldaviva/ThrottleDebounce](https://github.com/Aldaviva/ThrottleDebounce) | All 110 tests should pass. Invalid-argument, fixture, and cancellation exceptions must be classified as intentional and produce **no patches**. |
| `PaperArrayBounds` | Array adaptation of paper Listing 1.1 (former `review/fixtures/listing`); original listing included separately | Of 4 tests, 1 passes and 3 fail before repair. The tests express the desired repaired behavior, not expected-exception assertions. |

## Local project adaptations

- The recovered upstream libraries and their test projects target `net10.0`
  (JsonConverter.Abstractions: `netstandard2.1`).
  Legacy .NET Framework targets, GUI demos, and unrelated benchmarks are not built.
- JsonConverter's library and test sources are unchanged. Its `Directory.Build.props`
  drops only the SourceLink package and NuGet packaging metadata, and its project files
  keep their dependency versions. Its other serializer adapters are not included.
- HtmlSanitizer retains its source, signing key, and dependency versions.
  Its nullable warnings are not promoted to errors because instrumentation introduces
  nullable temporaries; nullable analysis remains enabled.
- ThrottleDebounce's FluentAssertions dependency is pinned to `7.2.2` instead
  of an open version range. Its library source is unchanged.
- The HtmlSanitizer baseline runs all 198 tests. Only the instrumented run excludes
  `ThreadTest`, which invokes 16,000 nested tests and otherwise produces an
  impractically large trace. The other 197 tests and their assertions are unchanged.
- Original licenses are included: HtmlSanitizer and JsonConverter use MIT; ThrottleDebounce uses
  Apache-2.0. The paper fixture is covered by the repository license.
- These are historical research inputs, not production dependencies. NuGet may
  report advisories in their pinned dependencies. Do not expose the benchmarks as
  services or use the generated repair candidates as production sanitizer code.

## Paper example

[Listing.paper.cs.txt](PaperArrayBounds/Listing.paper.cs.txt) reproduces the C# code
in the submitted paper's Listing 1.1, without its printed line numbers. It uses
`Span<int>` and `stackalloc`. The `.cs.txt` extension keeps this reference listing
out of compilation and instrumentation; the experiment runner does not execute it.

[Listing.cs](PaperArrayBounds/Listing.cs) is the runnable adaptation, not the exact
paper listing. It replaces the stack-allocated spans with heap-allocated `int[]`
buffers and makes the class and method public for the test project. The loop
conditions, writes, increments/decrements, and buffer lengths are retained.
The source model supports fixed-size `Int32` arrays, not `Span<int>`/`stackalloc`.
Consequently, the artifact's model-check and test results below apply to the array
adaptation only; they do not verify the original listing's span or stack-allocation
semantics.

The adaptation allocates buffers of lengths 5 and 2. With `check = 6`
and `i = 0`, the first loop attempts `buf1[5]`. Fixing only that loop still leaves
the second loop capable of indexing beyond `buf2.Length`. The regression tests
also cover a negative initial index, an oversized initial index, and a valid
execution. The experiment runner requires a `VerifiedExploredModel` candidate
that is `AcceptedByTests` for this example. The verifier is bounded and restricted;
this is not a proof for arbitrary C# programs.

The MicrosoftBugDemo, HtmlSanitizer, and ThrottleDebounce cases emit analysis plans without the optional test-validation
gate. ThrottleDebounce is a negative control: the runner requires all three
intentional-exception classifications and zero patches. MicrosoftBugDemo and
HtmlSanitizer emit unverified candidates. Exception traces, generated patches,
and successful pipeline execution must not be described as verified upstream fixes.

## JsonConverter (paper Section 3)

The runner checks every Section 3 claim (see *Paper claims* in the top-level
README) and records the evidence in `results/experiments/JsonConverter/paper-claims.json`.
The `IndexOutOfRangeException` occurs in the recorded, instrumented run: when it
logs the index of `list[i++]`, the trace logger evaluates `i++` again, so the array
`[null, English]` of length 2 is written at index 3. The uninstrumented suite passes.
The method-level guard (`PocoJsonSerializerStrategy.L102.diff`) wraps the body of
`DeserializeObject` in a `try/catch` that logs the exception and returns `default`,
as in upstream pull request [StefH/JsonConverter#27](https://github.com/StefH/JsonConverter/pull/27).
The runner applies it to an isolated copy of the benchmark and requires 174 passed /
6 skipped. Its model check is `Incomplete`.

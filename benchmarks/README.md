# Artifact benchmarks

The four directories below are the evaluation inputs. Run them from the repository
root with `.\run_experiments.ps1`. Original library and test sources are retained;
project files target .NET 10 so the artifact needs only one SDK/runtime.

| Directory | Source and provenance | Baseline contract |
|---|---|---|
| `MicrosoftBugDemo` | Existing extracted .NET reflection regression in this repository | 2 tests pass; 1 fails with a null dereference when a method is inherited. |
| `HtmlSanitizer` | [mganss/HtmlSanitizer](https://github.com/mganss/HtmlSanitizer/tree/7417120cfabea9f359b56f8de6f492adbb0e57da), commit `7417120cfabea9f359b56f8de6f492adbb0e57da`; recovered from `AutoRepair/multi_repo_workspace/summary.json` in Sharper commit `1a0e4b364^` | Upstream tests should pass. Deliberate exceptions exercised by these tests are not evidence of an upstream bug. |
| `ThrottleDebounce` | Original source/tests from Sharper demo branch commit `750c82385c83dc03283692f3b33ce7b25e832852`, directory `ThrottleDebounce`; upstream [Aldaviva/ThrottleDebounce](https://github.com/Aldaviva/ThrottleDebounce) | All 110 tests should pass. Invalid-argument, fixture, and cancellation exceptions must be classified as intentional and produce **no patches**. |
| `PaperArrayBounds` | Former `review/fixtures/listing` in this repository | Of 4 tests, 1 passes and 3 fail before repair. The tests express the desired repaired behavior, not expected-exception assertions. |

## Local project adaptations

- Both recovered upstream libraries and their test projects target `net10.0`.
  Legacy .NET Framework targets, GUI demos, and unrelated benchmarks are not built.
- HtmlSanitizer retains its source, signing key, and dependency versions.
  Its nullable warnings are not promoted to errors because instrumentation introduces
  nullable temporaries; nullable analysis remains enabled.
- ThrottleDebounce's FluentAssertions dependency is pinned to `7.2.2` instead
  of an open version range. Its library source is unchanged.
- The HtmlSanitizer baseline runs all 198 tests. Only the instrumented run excludes
  `ThreadTest`, which invokes 16,000 nested tests and otherwise produces an
  impractically large trace. The other 197 tests and their assertions are unchanged.
- Original licenses are included: HtmlSanitizer uses MIT; ThrottleDebounce uses
  Apache-2.0. The paper fixture is covered by the repository license.
- These are historical research inputs, not production dependencies. NuGet may
  report advisories in their pinned dependencies. Do not expose the benchmarks as
  services or use the generated repair candidates as production sanitizer code.

## Paper example

`PaperArrayBounds/Listing.cs` allocates buffers of lengths 5 and 2. With `check = 6`
and `i = 0`, the first loop attempts `buf1[5]`. Fixing only that loop still leaves
the second loop capable of indexing beyond `buf2.Length`. The regression tests
also cover a negative initial index, an oversized initial index, and a valid
execution. The experiment runner requires a `VerifiedExploredModel` candidate
that is `AcceptedByTests` for this example. The verifier is bounded and restricted;
this is not a proof for arbitrary C# programs.

The other three cases emit analysis plans without the optional test-validation
gate. ThrottleDebounce is a negative control: the runner requires all three
intentional-exception classifications and zero patches. MicrosoftBugDemo and
HtmlSanitizer emit unverified candidates. Exception traces, generated patches,
and successful pipeline execution must not be described as verified upstream fixes.

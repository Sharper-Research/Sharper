# AutoRepair multi-repo pipeline — summary

- repos attempted: **13**
- Step 1 (trace) success: **10**
- Step 2 (constraints) success: **10**
- Step 3 (CFG / patches) success: **10**
- target met (>=10 traces): **YES**
- total violations across all repos: **369**
- total patches synthesised across all repos: **97**

| # | Repo | Step1 events | Step2 violations | Step3 patches | Test project |
|---|---|---:|---:|---:|---|
| 00 | `nodirmcsd/WordNinjaSharp` | 123947 | 2 | 1 | `WordNinja.Tests\WordNinjaSharp.Tests.csproj` |
| 01 | `StephenCleary/Comparers` | 65046 | 9 | 3 | `test\UnitTests\UnitTests.csproj` |
| 02 | `StephenCleary/Deque` | 45109 | 81 | 7 | `test\UnitTests\UnitTests.csproj` |
| 03 | `StephenCleary/AsyncEx` | 128 | 0 | 0 | `test\AsyncEx.Oop.UnitTests\AsyncEx.Oop.UnitTests.csproj` |
| 04 | `danielwertheim/ensure.that` | 19722 | 143 | 78 | `src\tests\UnitTests\UnitTests.csproj` |
| 05 | `StefH/JsonConverter` | 6312 | 47 | 2 | `tests\SimpleJson.Tests\SimpleJson.Tests.csproj` |
| 06 | `mganss/HtmlSanitizer` | 283889 | 85 | 5 | `test\HtmlSanitizer.Tests\HtmlSanitizer.Tests.csproj` |
| 07 | `Tyrrrz/Onova` | 782 | 0 | 0 | `Onova.Tests\Onova.Tests.csproj` |
| 08 | `SimonCropp/Argon` | 462 | 0 | 0 | `src\ArgonTests\ArgonTests.csproj` |
| 09 | `JonPSmith/EfCore.GenericServices` | 55618 | 2 | 1 | `Tests\Tests.csproj` |
| 10 | `xoofx/Markdig` |  |  |  | `src\Markdig.Tests\Markdig.Tests.csproj` |
| 11 | `Tyrrrz/CliWrap` |  |  |  | `CliWrap.Tests\CliWrap.Tests.csproj` |
| 12 | `StephenCleary/Hosting` |  |  |  | `` |

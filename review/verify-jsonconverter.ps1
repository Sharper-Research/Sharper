param([string]$Output = "review/jsonconverter-run")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$baseCommit = "564788f264dd63d7c7628dbd3de393cd2b64d007"
$candidateCommit = "e0a064cc16e5b82755c1cb95c6270e56b44e439d"
$checkoutRoot = Join-Path ([IO.Path]::GetTempPath()) ("Sharper-jsonconverter-" + [guid]::NewGuid().ToString("N"))
$previousTracePath = $env:TRACE_LOG_JSON_PATH
Push-Location $root

function Invoke-Git([string]$Repository, [string[]]$Arguments) {
    & git -C $Repository @Arguments
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed" }
}

function Invoke-Test([string]$Project, [string]$Name, [string]$Filter, [int]$ExpectedExit) {
    $directory = Join-Path $Output $Name
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $arguments = @("test", $Project, "--framework", "net6.0", "--logger", "trx;LogFileName=$Name.trx",
        "--results-directory", $directory, "--verbosity", "minimal")
    if ($Filter) { $arguments += @("--filter", $Filter) }
    & dotnet @arguments *> (Join-Path $directory "test.log")
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne $ExpectedExit) {
        throw "$Name exited $exitCode; expected $ExpectedExit"
    }
    [xml]$trx = Get-Content (Join-Path $directory "$Name.trx") -Raw
    $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
    if (!$counters) { throw "$Name did not produce TRX counters" }
    return [ordered]@{
        exit_code = $exitCode
        total = [int]$counters.total
        executed = [int]$counters.executed
        passed = [int]$counters.passed
        failed = [int]$counters.failed
        skipped = [int]$counters.total - [int]$counters.executed
        trx = Join-Path $Name "$Name.trx"
    }
}

try {
    if (![IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }
    $Output = [IO.Path]::GetFullPath($Output)
    if (Test-Path $Output) { throw "Use a fresh output directory: $Output" }
    New-Item -ItemType Directory -Path $Output | Out-Null

    $repository = Join-Path $checkoutRoot "repository"
    $baseline = Join-Path $checkoutRoot "baseline"
    $instrumented = Join-Path $checkoutRoot "instrumented"
    $candidate = Join-Path $checkoutRoot "candidate"
    git clone --quiet --no-checkout https://github.com/StefH/JsonConverter.git $repository
    if ($LASTEXITCODE -ne 0) { throw "Could not clone the JsonConverter repository" }
    Invoke-Git $repository @("fetch", "--quiet", "--depth", "1", "origin", $baseCommit)
    Invoke-Git $repository @("fetch", "--quiet", "--depth", "1", "https://github.com/jim-daf/JsonConverter.git", $candidateCommit)
    Invoke-Git $repository @("worktree", "add", "--quiet", "--detach", $baseline, $baseCommit)
    Invoke-Git $repository @("worktree", "add", "--quiet", "--detach", $instrumented, $baseCommit)
    Invoke-Git $repository @("worktree", "add", "--quiet", "--detach", $candidate, $candidateCommit)

    $testRelative = "tests\SimpleJson.Tests\SimpleJson.Tests.csproj"
    $baselineProject = Join-Path $baseline $testRelative
    $instrumentedProject = Join-Path $instrumented $testRelative
    $candidateProject = Join-Path $candidate $testRelative
    $baselineResult = Invoke-Test $baselineProject "baseline-clean" "" 0
    if ($baselineResult.total -ne 180 -or $baselineResult.passed -ne 174 -or $baselineResult.failed -ne 0 -or $baselineResult.skipped -ne 6) {
        throw "Clean upstream baseline no longer has the expected 174 passed / 6 skipped results"
    }

    $tracePath = Join-Path $Output "instrumented-trace.jsonl"
    $env:TRACE_LOG_JSON_PATH = $tracePath
    $instrumenterLog = Join-Path $Output "instrumenter.log"
    & dotnet run --project (Join-Path $root "Sharper\Step1_Tracing\Instrumenter\Instrumenter.csproj") -- --test-project $instrumentedProject *> $instrumenterLog
    if ($LASTEXITCODE -ne 0) { throw "Instrumentation failed" }
    $instrumentedResult = Invoke-Test $instrumentedProject "instrumented-baseline" "FullyQualifiedName~ArrayAndListDeserializationTests" 1
    if (!(Test-Path $tracePath)) { throw "Instrumented test run produced no trace" }
    $instrumentedTrx = Get-Content (Join-Path $Output $instrumentedResult.trx) -Raw
    if ($instrumentedResult.failed -ne 1 -or $instrumentedTrx -notmatch "IndexOutOfRangeException") {
        throw "Expected the instrumented array test to fail with IndexOutOfRangeException"
    }

    $violationsPath = Join-Path $Output "violations.json"
    & dotnet run --project (Join-Path $root "Sharper\Step2_Constraints\Constraints\Constraints.csproj") -- `
        --trace $tracePath --report $violationsPath
    if ($LASTEXITCODE -ne 0 -or !(Test-Path $violationsPath)) { throw "Constraint analysis failed" }
    $violations = Get-Content $violationsPath -Raw | ConvertFrom-Json
    if (@($violations | Where-Object { $_.bindings.'event.exception.type' -eq "System.IndexOutOfRangeException" }).Count -eq 0) {
        throw "Trace analysis did not detect the array exception"
    }

    $planDirectory = Join-Path $Output "repair-plan"
    $libraryProject = Join-Path $instrumented "src\JsonConverter.SimpleJson\JsonConverter.SimpleJson.csproj"
    & dotnet run --project (Join-Path $root "Sharper\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj") -- `
        $libraryProject --violations $violationsPath --output-dir $planDirectory
    if ($LASTEXITCODE -ne 0) { throw "Repair analysis failed" }
    $plan = Get-Content (Join-Path $planDirectory "repair_plan.json") -Raw | ConvertFrom-Json
    if ($plan.Items.Count -eq 0 -or @($plan.Items | Where-Object { $_.CandidateModelCheck.Status -ne "Incomplete" }).Count -gt 0) {
        throw "Expected generated candidates to remain Incomplete for this unsupported case"
    }

    $candidateResult = Invoke-Test $candidateProject "pull-request-candidate-clean" "" 0
    if ($candidateResult.total -ne 180 -or $candidateResult.passed -ne 174 -or $candidateResult.failed -ne 0 -or $candidateResult.skipped -ne 6) {
        throw "Pinned pull-request candidate did not produce the expected 174 passed / 6 skipped result"
    }

    [ordered]@{
        source_repository = "https://github.com/StefH/JsonConverter"
        baseline_commit = $baseCommit
        pull_request = "https://github.com/StefH/JsonConverter/pull/27"
        candidate_repository = "https://github.com/jim-daf/JsonConverter"
        candidate_commit = $candidateCommit
        target_framework = "net6.0 (the pinned test project targets net6.0 and net45)"
        clean_baseline = $baselineResult
        instrumented_baseline = $instrumentedResult
        candidate_model_statuses = @($plan.Items | ForEach-Object { $_.CandidateModelCheck.Status })
        candidate_tests = $candidateResult
        interpretation = "The selected test passes on the clean baseline and fails after instrumentation. Sharper detects the traced exception, but this source case's generated candidates are Incomplete. The upstream PR candidate passes tests; this is not a model proof or an independent confirmation of a real uninstrumented bug."
    } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $Output "result.json") -Encoding UTF8
    Write-Output "JsonConverter artifact check passed. See $Output\result.json"
} finally {
    $env:TRACE_LOG_JSON_PATH = $previousTracePath
    if (Test-Path $checkoutRoot) { Remove-Item -LiteralPath $checkoutRoot -Recurse -Force }
    Pop-Location
}

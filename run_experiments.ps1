#Requires -Version 5.1
# Runs the full Sharper pipeline on every folder in .\benchmarks\ (MicrosoftBugDemo first)
# and saves each run under .\results\experiments\<BenchmarkName>\.
param(
    # Optional: run only these benchmarks, e.g. -Only MicrosoftBugDemo
    [string[]]$Only = @()
)
$ErrorActionPreference = "Stop"
$root           = $PSScriptRoot
$runAll         = Join-Path $root "Sharper/run_all.ps1"
$experimentsDir = Join-Path $root "results/experiments"
$restoreScript  = Join-Path $root "Sharper/Step1_Tracing/restore_project.ps1"

function Restore-Source([string]$dir) {
    # Undo instrumentation left by an earlier or interrupted run: *.cs.bak -> *.cs, remove TraceLogger.cs.
    $leftovers = @(Get-ChildItem $dir -Recurse -File -Include *.cs.bak, TraceLogger.cs |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
    if ($leftovers.Count -eq 0) { return }
    Write-Host "Restoring original source in $dir ($($leftovers.Count) instrumentation file(s) found)" -ForegroundColor Yellow
    & $restoreScript $dir | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Source restoration failed in $dir" }
}

function Resolve-Benchmark([string]$benchmarkDir) {
    # Test project = the .csproj that references Microsoft.NET.Test.Sdk.
    # Library under repair = that test project's first ProjectReference.
    $test = Get-ChildItem $benchmarkDir -Recurse -Filter *.csproj |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and (Select-String -Path $_.FullName -Pattern 'Microsoft.NET.Test.Sdk' -SimpleMatch -Quiet) } |
        Sort-Object FullName | Select-Object -First 1
    if (-not $test) { throw "No test project (Microsoft.NET.Test.Sdk) found" }
    [xml]$xml = Get-Content $test.FullName -Raw
    $ref = @($xml.SelectNodes("//*[local-name()='ProjectReference']"))[0]
    if (-not $ref) { throw "Test project $($test.Name) has no ProjectReference" }
    $library = [IO.Path]::GetFullPath((Join-Path $test.DirectoryName $ref.Include))
    return @{ SourceDir = (Split-Path $library); DriverProject = $test.FullName; Solution = $library }
}

function Get-TrxCounters([string]$trxPath) {
    [xml]$trx = Get-Content $trxPath -Raw
    $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
    if (-not $counters) { throw "No TRX counters in $trxPath" }
    return [ordered]@{
        Total = [int]$counters.total; Passed = [int]$counters.passed; Failed = [int]$counters.failed
        Skipped = [int]$counters.total - [int]$counters.executed
    }
}

function Assert-JsonConverterClaims($target, [string]$outputDir, $violations, $plan) {
    # Checks each JsonConverter claim of the paper's Section 3 and records the evidence in paper-claims.json.
    $baseline = Get-TrxCounters (Join-Path $outputDir "baseline/baseline.trx")
    if ($baseline.Passed -ne 174 -or $baseline.Skipped -ne 6 -or $baseline.Failed -ne 0) {
        throw "Clean JsonConverter suite should give 174 passed / 6 skipped; got $($baseline.Passed) / $($baseline.Skipped)."
    }

    [xml]$instrumented = Get-Content (Join-Path $outputDir "step1_traces/instrumented.trx") -Raw
    $failure = @($instrumented.SelectNodes("//*[local-name()='UnitTestResult']") | Where-Object {
        $_.testName -eq "ArrayAndListDeserializationTests" -and $_.outcome -eq "Failed" -and $_.InnerXml -match "IndexOutOfRangeException" })
    if ($failure.Count -eq 0) { throw "Recorded run did not report ArrayAndListDeserializationTests failing with IndexOutOfRangeException." }

    $boundsViolation = @($violations | Where-Object {
        $_.predicate -like "BufferInBounds*" -and $_.file -eq "PocoJsonSerializerStrategy.cs" -and
        $_.bindings.'index:list' -eq 3 -and $_.bindings.'heap.size(list)' -eq 2 -and
        (@($_.bindings.'list.contents') -join ",") -eq "null,English" }) | Select-Object -First 1
    if (-not $boundsViolation) { throw "Trace did not show the array [null, English] of length 2 written at index 3." }

    $sourceFile = Join-Path $target.SourceDir "PocoJsonSerializerStrategy.cs"
    $writeLine = (Get-Content $sourceFile)[$boundsViolation.line - 1]
    if ($writeLine -notmatch 'list\[i\+\+\]\s*=') { throw "Violation line $($boundsViolation.line) is not the list[i++] write." }

    # The method-level candidate: the body of DeserializeObject wrapped in try/catch that logs and returns default.
    $methodGuard = '(?s)DeserializeObject\(object\? value, Type type\)\s*\{\s*//[^\n]*\n\s*try\s*\{.*' +
        'catch \(System\.IndexOutOfRangeException (\w+)\)\s*\{\s*System\.Diagnostics\.Debug\.WriteLine\([^;]*\1\);\s*return default!;'
    $candidate = @($plan.Items | Where-Object { $_.PatchedFilePath -and (Test-Path $_.PatchedFilePath) -and
        (Get-Content $_.PatchedFilePath -Raw) -match $methodGuard }) | Select-Object -First 1
    if (-not $candidate) { throw "No candidate wraps the DeserializeObject body in a logging try/catch that returns default." }

    # Apply that candidate to an isolated copy of the unmodified benchmark and run the full suite.
    $benchmarkRoot = Split-Path (Split-Path $target.SourceDir)
    $sandbox = Join-Path ([IO.Path]::GetTempPath()) ("Sharper-jsonconverter-" + [guid]::NewGuid().ToString("N"))
    try {
        Copy-Item $benchmarkRoot $sandbox -Recurse
        Get-ChildItem $sandbox -Recurse -Directory -Include bin, obj | Remove-Item -Recurse -Force
        $relativeSource = $sourceFile.Substring($benchmarkRoot.Length).TrimStart('\', '/')
        $relativeTest = $target.DriverProject.Substring($benchmarkRoot.Length).TrimStart('\', '/')
        Copy-Item $candidate.PatchedFilePath (Join-Path $sandbox $relativeSource) -Force
        $patchedDir = Join-Path $outputDir "patched-suite"
        & dotnet test (Join-Path $sandbox $relativeTest) --verbosity minimal --logger "trx;LogFileName=patched.trx" `
            --results-directory $patchedDir | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Patched JsonConverter suite failed (exit $LASTEXITCODE)." }
        $patched = Get-TrxCounters (Join-Path $patchedDir "patched.trx")
    } finally {
        if (Test-Path $sandbox) { Remove-Item $sandbox -Recurse -Force }
    }
    if ($patched.Passed -ne 174 -or $patched.Skipped -ne 6 -or $patched.Failed -ne 0) {
        throw "Patched JsonConverter suite should give 174 passed / 6 skipped; got $($patched.Passed) / $($patched.Skipped)."
    }

    [ordered]@{
        RecordedFailure = [ordered]@{ Test = "ArrayAndListDeserializationTests"; Exception = "System.IndexOutOfRangeException" }
        ArrayWrite = [ordered]@{ File = $boundsViolation.file; Line = $boundsViolation.line; Statement = $writeLine.Trim()
            Contents = @($boundsViolation.bindings.'list.contents'); Length = $boundsViolation.bindings.'heap.size(list)'
            Index = $boundsViolation.bindings.'index:list'; Predicate = $boundsViolation.predicate }
        MethodGuardPatch = $candidate.DiffPath
        CleanSuite = $baseline
        PatchedSuite = $patched
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $outputDir "paper-claims.json") -Encoding UTF8
    Write-Host "JsonConverter: all Section 3 claims reproduced (see paper-claims.json)" -ForegroundColor Green
}

$benchmarks = Get-ChildItem (Join-Path $root "benchmarks") -Directory |
    Where-Object { $Only.Count -eq 0 -or $Only -contains $_.Name } |
    Sort-Object @{ Expression = { $_.Name -ne "MicrosoftBugDemo" } }, Name
$benchmarks = @($benchmarks)
if ($benchmarks.Count -eq 0) { throw "No matching benchmarks found. Check -Only." }
foreach ($name in $Only) {
    if ($benchmarks.Name -notcontains $name) { throw "Unknown benchmark: $name" }
}
New-Item -ItemType Directory -Path $experimentsDir -Force | Out-Null
$sdk = ([string](& dotnet --version | Out-String)).Trim()
if ($LASTEXITCODE -ne 0) { throw "Could not identify the .NET SDK." }
# Cast to [string] rather than calling .ToString(): a null here must not abort the whole run.
[ordered]@{
    CapturedUtc = (Get-Date).ToUniversalTime().ToString("o")
    OS = [string][Environment]::OSVersion.VersionString
    Architecture = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    DotnetSdk = $sdk
    PowerShell = [string]$PSVersionTable.PSVersion
} | ConvertTo-Json | Set-Content (Join-Path $experimentsDir "environment.json") -Encoding UTF8
$expectedFailures = @{ MicrosoftBugDemo = 1; HtmlSanitizer = 0; ThrottleDebounce = 0; PaperArrayBounds = 3; JsonConverter = 0 }

$summary = @()
foreach ($benchmark in $benchmarks) {
    $outputDir = Join-Path $experimentsDir $benchmark.Name
    if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Write-Host ""
    Write-Host "##### Benchmark: $($benchmark.Name) #####" -ForegroundColor Magenta
    $status = "Passed"; $message = ""; $target = $null; $baselineFailed = $null; $baselinePassed = $null
    $violationCount = 0; $repairCount = 0; $patchCount = 0; $sourceHashes = @{}
    try {
        $target = Resolve-Benchmark $benchmark.FullName
        Restore-Source $target.SourceDir
        Get-ChildItem $target.SourceDir -Recurse -Filter *.cs -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
            ForEach-Object { $sourceHashes[$_.FullName] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
        $baselineDir = Join-Path $outputDir "baseline"
        & dotnet test $target.DriverProject --verbosity minimal --logger "trx;LogFileName=baseline.trx" `
            --results-directory $baselineDir -- xUnit.MaxParallelThreads=1 xUnit.ParallelizeTestCollections=false
        $baselineExit = $LASTEXITCODE
        $baselineTrx = Join-Path $baselineDir "baseline.trx"
        if (-not (Test-Path $baselineTrx)) { throw "Baseline produced no TRX (exit $baselineExit)." }
        [xml]$baseline = Get-Content $baselineTrx -Raw
        $counters = $baseline.SelectSingleNode("//*[local-name()='Counters']")
        if (-not $counters -or [int]$counters.executed -eq 0 -or [int]$counters.error -gt 0 -or
            ($baselineExit -ne 0 -and [int]$counters.failed -eq 0)) {
            throw "Baseline tests did not execute successfully (exit $baselineExit)."
        }
        $baselineFailed = [int]$counters.failed
        $baselinePassed = [int]$counters.passed
        if ($expectedFailures.ContainsKey($benchmark.Name) -and $baselineFailed -ne $expectedFailures[$benchmark.Name]) {
            throw "Baseline had $baselineFailed failing tests; expected $($expectedFailures[$benchmark.Name]). See $baselineTrx"
        }
        # Run the pipeline in a child PowerShell so its output can be logged: redirecting a script's
        # streams in-process turns dotnet's stderr ("Test Run Failed.") into terminating errors.
        $powershell = (Get-Process -Id $PID).Path
        $global:LASTEXITCODE = 0
        $ErrorActionPreference = "Continue"
        try {
            $validationArgs = @()
            if ($benchmark.Name -eq "HtmlSanitizer") {
                $validationArgs += @("-TestFilter", "FullyQualifiedName!~ThreadTest")
            }
            if ($benchmark.Name -eq "PaperArrayBounds") {
                $validationArgs += @("-ValidationRoot", $benchmark.FullName, "-ReproducerFilter", "FullyQualifiedName~PaperAssumptionReturnsNormally")
            }
            & $powershell -NoProfile -ExecutionPolicy Bypass -File $runAll `
                -SourceDir $target.SourceDir -DriverProject $target.DriverProject `
                -Solution $target.Solution -OutputDir $outputDir @validationArgs 2>&1 |
                ForEach-Object { "$_" } |
                Tee-Object -FilePath (Join-Path $outputDir "pipeline.log")
            $pipelineExit = $LASTEXITCODE
        } finally { $ErrorActionPreference = "Stop" }
        if ($pipelineExit -ne 0) { throw "run_all.ps1 exited with code $pipelineExit - see pipeline.log" }
        $violations = Get-Content (Join-Path $outputDir "step2_constraints/violations.json") -Raw | ConvertFrom-Json
        $plan = Get-Content (Join-Path $outputDir "step3_repairs/repair_plan.json") -Raw | ConvertFrom-Json
        $violationCount = @($violations).Count
        $repairCount = @($plan.Items).Count
        $patchCount = @(Get-ChildItem (Join-Path $outputDir "step3_repairs/patches") -Filter *.diff -ErrorAction SilentlyContinue).Count
        if ($violationCount -eq 0 -or $repairCount -eq 0) { throw "No violations or repair items were produced." }
        if ($benchmark.Name -eq "ThrottleDebounce") {
            $expectedActions = @("IntentionalValidator", "TestFixtureException", "DocumentedCancellation")
            if ($patchCount -ne 0 -or @($plan.Items | Where-Object { $_.Action -notin $expectedActions }).Count -gt 0) {
                throw "ThrottleDebounce should suppress only intentional validation, fixture exceptions, and documented cancellation without generating patches."
            }
            foreach ($action in $expectedActions) {
                if (@($plan.Items | Where-Object Action -eq $action).Count -eq 0) {
                    throw "ThrottleDebounce did not exercise the expected $action classification."
                }
            }
        } elseif ($patchCount -eq 0) { throw "No candidate diff files were produced." }
        if ($benchmark.Name -eq "PaperArrayBounds" -and
            @($plan.Items | Where-Object { $_.Validation.Status -eq "AcceptedByTests" -and
                $_.CandidateModelCheck.Status -eq "VerifiedExploredModel" }).Count -eq 0) {
            throw "Paper example did not produce a model-checked, test-accepted candidate."
        }
        if ($benchmark.Name -eq "JsonConverter") { Assert-JsonConverterClaims $target $outputDir $violations $plan }
    } catch {
        $status = "Failed"; $message = $_.Exception.Message
        Write-Host "Benchmark $($benchmark.Name) failed: $message" -ForegroundColor Red
    } finally {
        # Always leave the benchmark uninstrumented, even if a step failed or the run was interrupted.
        if ($target) {
            Restore-Source $target.SourceDir
            foreach ($file in $sourceHashes.Keys) {
                if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $sourceHashes[$file]) {
                    $status = "Failed"; $message = "Original source changed: $file"
                    Write-Host $message -ForegroundColor Red
                }
            }
            $sourceHashes | ConvertTo-Json | Set-Content (Join-Path $outputDir "original-source-sha256.json") -Encoding UTF8
        }
    }

    $summary += [pscustomobject]@{
        Benchmark   = $benchmark.Name
        Status      = $status
        BaselinePassed = $baselinePassed
        BaselineFailed = $baselineFailed
        Violations  = $violationCount
        RepairItems = $repairCount
        Patches     = $patchCount
        Message     = $message
    }
}

$summary | Export-Csv (Join-Path $experimentsDir "summary.csv") -NoTypeInformation
Write-Host ""
Write-Host "##### Experiment summary #####" -ForegroundColor Magenta
$summary | Format-Table -AutoSize
if (@($summary | Where-Object Status -eq "Failed").Count -gt 0) { exit 1 }
exit 0

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
$expectedFailures = @{ MicrosoftBugDemo = 1; HtmlSanitizer = 0; ThrottleDebounce = 0; PaperArrayBounds = 3 }

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

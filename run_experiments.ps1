# Runs the full Sharper pipeline on every folder in .\benchmarks\ (MicrosoftBugDemo first)
# and saves each run under .\results\experiments\<BenchmarkName>\.
param(
    # Optional: run only these benchmarks, e.g. -Only MicrosoftBugDemo
    [string[]]$Only = @()
)
$ErrorActionPreference = "Stop"
$root           = $PSScriptRoot
$runAll         = Join-Path $root "Sharper\run_all.ps1"
$experimentsDir = Join-Path $root "results\experiments"
$restoreScript  = Join-Path $root "Sharper\Step1_Tracing\restore_project.ps1"

function Restore-Source([string]$dir) {
    # Undo instrumentation left by an earlier or interrupted run: *.cs.bak -> *.cs, remove TraceLogger.cs.
    $leftovers = @(Get-ChildItem $dir -Recurse -File -Include *.cs.bak, TraceLogger.cs |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
    if ($leftovers.Count -eq 0) { return }
    Write-Host "Restoring original source in $dir ($($leftovers.Count) instrumentation file(s) found)" -ForegroundColor Yellow
    & $restoreScript $dir *> $null
}

function Resolve-Benchmark([string]$benchmarkDir) {
    # Test project = the .csproj that references Microsoft.NET.Test.Sdk.
    # Library under repair = that test project's first ProjectReference.
    $test = Get-ChildItem $benchmarkDir -Recurse -Filter *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and (Select-String -Path $_.FullName -Pattern 'Microsoft.NET.Test.Sdk' -SimpleMatch -Quiet) } |
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

$summary = @()
foreach ($benchmark in $benchmarks) {
    $outputDir = Join-Path $experimentsDir $benchmark.Name
    if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Write-Host ""
    Write-Host "##### Benchmark: $($benchmark.Name) #####" -ForegroundColor Magenta
    $status = "Passed"; $message = ""; $target = $null
    try {
        $target = Resolve-Benchmark $benchmark.FullName
        Restore-Source $target.SourceDir
        # Run the pipeline in a child PowerShell so its output can be logged: redirecting a script's
        # streams in-process turns dotnet's stderr ("Test Run Failed.") into terminating errors.
        $powershell = (Get-Process -Id $PID).Path
        $global:LASTEXITCODE = 0
        $ErrorActionPreference = "Continue"
        try {
            & $powershell -NoProfile -ExecutionPolicy Bypass -File $runAll `
                -SourceDir $target.SourceDir -DriverProject $target.DriverProject `
                -Solution $target.Solution -OutputDir $outputDir 2>&1 |
                ForEach-Object { "$_" } |
                Tee-Object -FilePath (Join-Path $outputDir "pipeline.log")
            $pipelineExit = $LASTEXITCODE
        } finally { $ErrorActionPreference = "Stop" }
        if ($pipelineExit -ne 0) { throw "run_all.ps1 exited with code $pipelineExit - see pipeline.log" }
    } catch {
        $status = "Failed"; $message = $_.Exception.Message
        Write-Host "Benchmark $($benchmark.Name) failed: $message" -ForegroundColor Red
    } finally {
        # Always leave the benchmark uninstrumented, even if a step failed or the run was interrupted.
        if ($target) { Restore-Source $target.SourceDir }
    }

    $violationsFile = Join-Path $outputDir "step2_constraints\violations.json"
    $planFile       = Join-Path $outputDir "step3_repairs\repair_plan.json"
    $patchesDir     = Join-Path $outputDir "step3_repairs\patches"
    $summary += [pscustomobject]@{
        Benchmark   = $benchmark.Name
        Status      = $status
        Violations  = if (Test-Path $violationsFile) { @(Get-Content $violationsFile -Raw | ConvertFrom-Json).Count } else { 0 }
        RepairItems = if (Test-Path $planFile) { @((Get-Content $planFile -Raw | ConvertFrom-Json).Items).Count } else { 0 }
        Patches     = if (Test-Path $patchesDir) { @(Get-ChildItem $patchesDir -Filter *.diff).Count } else { 0 }
        Message     = $message
    }
}

$summary | Export-Csv (Join-Path $experimentsDir "summary.csv") -NoTypeInformation
Write-Host ""
Write-Host "##### Experiment summary #####" -ForegroundColor Magenta
$summary | Format-Table -AutoSize
if (@($summary | Where-Object Status -eq "Failed").Count -gt 0) { exit 1 }
exit 0

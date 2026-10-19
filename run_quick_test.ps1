# Quick smoke test: runs the full Sharper pipeline on the MicrosoftBugDemo benchmark
# and passes if Step 3 produced candidate patches.
$ErrorActionPreference = "Stop"
$root          = $PSScriptRoot
$bench         = Join-Path $root "benchmarks\MicrosoftBugDemo"
$sourceDir     = Join-Path $bench "RuntimeCode"
$outputDir     = Join-Path $root "results\quick-test"
$patches       = Join-Path $outputDir "step3_repairs\patches"
$restoreScript = Join-Path $root "Sharper\Step1_Tracing\restore_project.ps1"

function Restore-Source([string]$dir) {
    # Undo instrumentation left by an earlier or interrupted run: *.cs.bak -> *.cs, remove TraceLogger.cs.
    $leftovers = @(Get-ChildItem $dir -Recurse -File -Include *.cs.bak, TraceLogger.cs |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
    if ($leftovers.Count -eq 0) { return }
    Write-Host "Restoring original source in $dir ($($leftovers.Count) instrumentation file(s) found)" -ForegroundColor Yellow
    & $restoreScript $dir *> $null
}

# Fixed output path: start from an empty folder so a stale result cannot make the test pass.
if (Test-Path $outputDir) { Remove-Item $outputDir -Recurse -Force }

$failure = $null
try {
    Restore-Source $sourceDir
    $global:LASTEXITCODE = 0
    & (Join-Path $root "Sharper\run_all.ps1") `
        -SourceDir     $sourceDir `
        -DriverProject (Join-Path $bench "RuntimeCode.Tests\RuntimeCode.Tests.csproj") `
        -Solution      (Join-Path $bench "RuntimeCode\RuntimeCode.csproj") `
        -OutputDir     $outputDir
    if ($LASTEXITCODE -ne 0) { $failure = "run_all.ps1 exited with code $LASTEXITCODE" }
} catch {
    $failure = $_.Exception.Message
} finally {
    # Always leave the benchmark uninstrumented, even if a step failed or the run was interrupted.
    Restore-Source $sourceDir
}

Write-Host ""
if (-not $failure -and -not (Test-Path $patches)) { $failure = "no patches found at $patches" }
if ($failure) {
    Write-Host "QUICK TEST FAILED: $failure" -ForegroundColor Red
    exit 1
}
Write-Host "QUICK TEST PASSED" -ForegroundColor Green
exit 0

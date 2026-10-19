param(
    [string]$TargetSourceDir = "",
    [string]$TestProject = "",
    [string]$TestFilter = "",
    [string]$TraceLogPath = "",
    [string]$TestResultsDirectory = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter/Instrumenter.csproj"
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"

if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "benchmarks/MicrosoftBugDemo/RuntimeCode"
}

$TargetSourceDir = Resolve-Path $TargetSourceDir

Write-Host "=== Instrumenting Source Code ===" -ForegroundColor Cyan
Write-Host "Target source: $TargetSourceDir"

if (-not $SkipRestore -and (Test-Path $restoreScript)) {
    Write-Host "Restoring instrumented files from backups..." -ForegroundColor Yellow
    & $restoreScript $TargetSourceDir | Out-Null
}

Write-Host "Running Instrumenter on: $TargetSourceDir" -ForegroundColor Yellow
dotnet run --project $instrumenterProject -- "$TargetSourceDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Instrumenter failed!"
    exit 1
}

Write-Host "=== Instrumentation complete ===" -ForegroundColor Green

# Set a fixed, known path for the trace log so it is always easy to find
if (-not $TraceLogPath) { $TraceLogPath = Join-Path $repoRoot "results/tracing/trace_logs.jsonl" }
$traceLogPath = [IO.Path]::GetFullPath($TraceLogPath)
New-Item -ItemType Directory -Path (Split-Path $traceLogPath) -Force | Out-Null

# Clear the old trace file before running so each run starts fresh
if (Test-Path $traceLogPath) {
    Remove-Item $traceLogPath -Force
    Write-Host "Cleared old trace log." -ForegroundColor Yellow
}

# TraceLogger appends once per event; write to local temp storage (fast even when results\ is a
# Docker bind mount) and move the finished trace into place afterwards.
$scratchTracePath = Join-Path ([IO.Path]::GetTempPath()) ("sharper-trace-{0}.jsonl" -f [guid]::NewGuid().ToString("N"))
$previousTracePath = $env:TRACE_LOG_JSON_PATH
$env:TRACE_LOG_JSON_PATH = $scratchTracePath

# Run tests
if ([string]::IsNullOrWhiteSpace($TestProject)) {
    $testProject = Join-Path $repoRoot "benchmarks/MicrosoftBugDemo/RuntimeCode.Tests/RuntimeCode.Tests.csproj"
} else {
    $testProject = Resolve-Path $TestProject
}

Write-Host ""
Write-Host "=== Running Tests ===" -ForegroundColor Cyan
Write-Host "Test project: $testProject"
Write-Host "Trace log:    $traceLogPath"
Write-Host ""

$testArgs = @("test", $testProject, "/p:GeneratePackageOnBuild=false", "--verbosity", "minimal")
if ($TestFilter) { $testArgs += @("--filter", $TestFilter) }
if (-not $TestResultsDirectory) { $TestResultsDirectory = Split-Path $traceLogPath }
$trxPath = Join-Path $TestResultsDirectory "instrumented.trx"
if (Test-Path $trxPath) { Remove-Item -LiteralPath $trxPath -Force }
$testArgs += @("--logger", "trx;LogFileName=instrumented.trx", "--results-directory", $TestResultsDirectory,
    "--", "xUnit.MaxParallelThreads=1", "xUnit.ParallelizeTestCollections=false")
try {
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE
} finally {
    $env:TRACE_LOG_JSON_PATH = $previousTracePath
    if (Test-Path $scratchTracePath) { Move-Item -LiteralPath $scratchTracePath -Destination $traceLogPath -Force }
}
if (-not (Test-Path $trxPath)) { throw "Test execution did not produce $trxPath (exit $testExitCode). Check build/test errors above." }
[xml]$trx = Get-Content $trxPath -Raw
$counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
if (-not $counters -or [int]$counters.executed -eq 0 -or [int]$counters.error -gt 0 -or
    ($testExitCode -ne 0 -and [int]$counters.failed -eq 0)) {
    throw "Test execution failed or ran no tests (exit $testExitCode). See $trxPath."
}

# Show trace log summary
Write-Host ""
if (Test-Path $traceLogPath) {
    $lineCount = (Get-Content $traceLogPath | Measure-Object -Line).Lines
    Write-Host "=== Trace Log Written ===" -ForegroundColor Cyan
    Write-Host "File:    $traceLogPath"
    Write-Host "Entries: $lineCount trace events recorded"
} else {
    throw "No trace log was written to $traceLogPath"
}
if ((Get-Item $traceLogPath).Length -eq 0) { throw "Trace log is empty: $traceLogPath" }

if ($testExitCode -ne 0) {
    Write-Host ""
    Write-Host "=== Tests FAILED (continuing - failures are signal, not error) ===" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "=== All Tests Passed ===" -ForegroundColor Green
exit 0

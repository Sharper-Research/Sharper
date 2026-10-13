param(
    [string]$TargetSourceDir = "",
    [string]$TestProject = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"

if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "review\fixtures\runtime\RuntimeCode"
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
$traceLogPath = Join-Path $PSScriptRoot "trace_logs.jsonl"

# Clear the old trace file before running so each run starts fresh
if (Test-Path $traceLogPath) {
    Remove-Item $traceLogPath -Force
    Write-Host "Cleared old trace log." -ForegroundColor Yellow
}

# Tell TraceLogger where to write via environment variable
$env:TRACE_LOG_JSON_PATH = $traceLogPath

# Run tests
if ([string]::IsNullOrWhiteSpace($TestProject)) {
    $testProject = Join-Path $repoRoot "review\fixtures\runtime\RuntimeCode.Tests\RuntimeCode.Tests.csproj"
} else {
    $testProject = Resolve-Path $TestProject
}

Write-Host ""
Write-Host "=== Running Tests ===" -ForegroundColor Cyan
Write-Host "Test project: $testProject"
Write-Host "Trace log:    $traceLogPath"
Write-Host ""

dotnet test $testProject /p:GeneratePackageOnBuild=false --verbosity normal -- xUnit.MaxParallelThreads=1

$testExitCode = $LASTEXITCODE

# Show trace log summary
Write-Host ""
if (Test-Path $traceLogPath) {
    $lineCount = (Get-Content $traceLogPath | Measure-Object -Line).Lines
    Write-Host "=== Trace Log Written ===" -ForegroundColor Cyan
    Write-Host "File:    $traceLogPath"
    Write-Host "Entries: $lineCount trace events recorded"
} else {
    Write-Host "Warning: No trace log was written to $traceLogPath" -ForegroundColor Yellow
}

if ($testExitCode -ne 0) {
    Write-Host ""
    Write-Host "=== Tests FAILED (continuing - failures are signal, not error) ===" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "=== All Tests Passed ===" -ForegroundColor Green

param(
    [string]$TargetSourceDir = "",
    [string]$DriverProject = "",
    [string]$TestFilter = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$baseScript = Join-Path $PSScriptRoot "generate_trace.ps1"

Write-Host "=== Step 1: Full Test Suite Trace Run ===" -ForegroundColor Cyan
$outputDir = Join-Path $PSScriptRoot "../../results/full-test-tracing"

& $baseScript `
    -TargetSourceDir $TargetSourceDir `
    -TestProject $DriverProject `
    -TestFilter $TestFilter `
    -SkipRestore:$SkipRestore `
    -TraceLogPath (Join-Path $outputDir "trace_logs.jsonl") `
    -TestResultsDirectory $outputDir
exit $LASTEXITCODE

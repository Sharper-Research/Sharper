param(
    [string]$TargetSourceDir = "",
    [string]$DriverProject = "",
    [string]$TestFilter = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$baseScript = Join-Path $PSScriptRoot "generate_trace.ps1"

Write-Host "=== Step 1: Full Test Suite Trace Run ===" -ForegroundColor Cyan
Write-Host "Stop-on-first-failure: OFF" -ForegroundColor DarkGray
Write-Host "Output files:" -ForegroundColor DarkGray
Write-Host "  trace_logs_full_test_run.txt" -ForegroundColor DarkGray
Write-Host "  trace_logs_full_test_run.jsonl" -ForegroundColor DarkGray

& $baseScript `
    -TargetSourceDir $TargetSourceDir `
    -DriverProject $DriverProject `
    -TestFilter $TestFilter `
    -SkipRestore:$SkipRestore `
    -StopOnFirstFailure:$false `
    -LogFileTxtName "trace_logs_full_test_run.txt" `
    -LogFileJsonName "trace_logs_full_test_run.jsonl"

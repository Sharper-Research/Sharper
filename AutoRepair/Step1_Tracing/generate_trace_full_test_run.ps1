# script parameters, same as the base generate_trace.ps1 script
param(
    [string]$TargetSourceDir = "",   # folder with source code
    [string]$DriverProject = "",     # test project .csproj path
    [string]$TestFilter = "",        # optional test filter
    [switch]$SkipRestore             # skip restoring backups before instrumenting
)

$ErrorActionPreference = "Stop" # stop on any errors

# this is just a wrapper around generate_trace.ps1
$baseScript = Join-Path $PSScriptRoot "generate_trace.ps1" # path to the main tracing script

Write-Host "=== Step 1: Full Test Suite Trace Run ===" -ForegroundColor Cyan
Write-Host "Stop-on-first-failure: OFF" -ForegroundColor DarkGray # this version runs ALL tests
Write-Host "Output files:" -ForegroundColor DarkGray
Write-Host "  trace_logs_full_test_run.txt" -ForegroundColor DarkGray   # different filenames so we dont
Write-Host "  trace_logs_full_test_run.jsonl" -ForegroundColor DarkGray # overwrite the regular trace logs

# call the base script with StopOnFirstFailure set to false
# and different output filenames so the regular logs are preserved
& $baseScript `
    -TargetSourceDir $TargetSourceDir `
    -DriverProject $DriverProject `
    -TestFilter $TestFilter `
    -SkipRestore:$SkipRestore `
    -StopOnFirstFailure:$false `
    -LogFileTxtName "trace_logs_full_test_run.txt" `
    -LogFileJsonName "trace_logs_full_test_run.jsonl"

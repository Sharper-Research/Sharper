$ErrorActionPreference = "Stop"

$targetAppDir = Resolve-Path (Join-Path $PSScriptRoot "..\..\TargetApp")
$targetAppProject = Join-Path $targetAppDir "TargetApp.csproj"
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$logFileTxt = Join-Path $PSScriptRoot "trace_logs.txt"
$logFileJson = Join-Path $PSScriptRoot "trace_logs.jsonl"
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"

Write-Host "=== Step 1: Generating Trace Logs ===" -ForegroundColor Cyan

if (Test-Path $restoreScript) {
    Write-Host "0. Restoring TargetApp from backups..." -ForegroundColor Yellow
    & $restoreScript $targetAppDir | Out-Null
}

# Clean previous logs
if (Test-Path $logFileJson) { Remove-Item $logFileJson }
if (Test-Path $logFileTxt) { Remove-Item $logFileTxt }

Write-Host "1. Running Instrumenter on Directory: $targetAppDir" -ForegroundColor Yellow
dotnet run --project $instrumenterProject -- "$targetAppDir"
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed!"; exit 1 }

Write-Host "2. Running Target App..." -ForegroundColor Yellow
Write-Host "   JSON Log: $logFileJson"

# Set environment variable for the JSON log path so TraceLogger knows where to write
$env:TRACE_LOG_JSON_PATH = $logFileJson

# We run standard 'dotnet run'. The code is already instrumented in-place.
dotnet run --project $targetAppProject > $logFileTxt

Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Trace logs available:"
Write-Host "  $logFileTxt"
Write-Host "  $logFileJson"

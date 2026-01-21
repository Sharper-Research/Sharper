$ErrorActionPreference = "Stop"

$targetAppProject = Join-Path $PSScriptRoot "..\..\TargetApp\TargetApp.csproj"
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$logFileTxt = Join-Path $PSScriptRoot "trace_logs.txt"
$logFileJson = Join-Path $PSScriptRoot "trace_logs.jsonl"

Write-Host "=== Step 1: Generating Trace Logs ===" -ForegroundColor Cyan

Write-Host "1. Running Instrumenter..." -ForegroundColor Yellow
dotnet run --project $instrumenterProject
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed!"; exit 1 }

Write-Host "2. Running Target App (Trace Mode)..." -ForegroundColor Yellow
Write-Host "   TXT Log: $logFileTxt"
Write-Host "   JSON Log: $logFileJson"

# Set environment variable for the JSON log path so TraceLogger knows where to write
$env:TRACE_LOG_JSON_PATH = $logFileJson

dotnet run --project $targetAppProject -c Trace > $logFileTxt

Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Trace logs available:"
Write-Host "  $logFileTxt"
Write-Host "  $logFileJson"
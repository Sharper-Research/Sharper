$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$constraintsDir = Join-Path $scriptRoot "Constraints"
$tracePath = Resolve-Path (Join-Path $scriptRoot "..\Step1_Tracing\trace_logs_command_line_api.jsonl")
$outputDir = Join-Path $scriptRoot "reports"

if (-not (Test-Path $tracePath)) {
    Write-Error "Trace file not found at $tracePath. Run Step 1 first."
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host "Building Constraint Analyzer..." -ForegroundColor Cyan
dotnet build $constraintsDir | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Constraint analyzer build failed."
}

Write-Host "Analyzing trace: $tracePath" -ForegroundColor Cyan
dotnet run --project $constraintsDir -- `
    --trace "$tracePath" `
    --report (Join-Path $outputDir "violations.json")
if ($LASTEXITCODE -ne 0) {
    throw "Constraint analysis run failed."
}

Write-Host "Analysis complete. See reports folder for details." -ForegroundColor Green

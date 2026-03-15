param(
    [string]$TracePath = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$constraintsDir = Join-Path $scriptRoot "Constraints"

if ([string]::IsNullOrWhiteSpace($TracePath)) {
    $TracePath = Resolve-Path (Join-Path $scriptRoot "..\Step1_Tracing\trace_logs.jsonl")
}

$outputDir = Join-Path $scriptRoot "reports"

if (-not (Test-Path $TracePath)) {
    Write-Error "Trace file not found at $TracePath. Run Step 1 first."
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host "Building Constraint Analyzer..." -ForegroundColor Cyan
dotnet build $constraintsDir | Out-Null

Write-Host "Analyzing trace: $TracePath" -ForegroundColor Cyan
dotnet run --project $constraintsDir -- `
    --trace "$TracePath" `
    --report (Join-Path $outputDir "violations.json")

Write-Host "Analysis complete. See reports folder for details." -ForegroundColor Green

param(
    [string]$TracePath = "",
    [string]$OutputDir = ""
)
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$constraintsDir = Join-Path $scriptRoot "Constraints"
if (-not $TracePath) { $TracePath = Join-Path $scriptRoot "../../results/tracing/trace_logs.jsonl" }
if (-not $OutputDir) { $OutputDir = Join-Path $scriptRoot "../../results/constraints" }

if (-not (Test-Path $tracePath)) {
    Write-Error "Trace file not found at $tracePath. Run Step 1 first."
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host "Building Constraint Analyzer..." -ForegroundColor Cyan
dotnet build $constraintsDir --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Constraint Analyzer build failed." }

Write-Host "Analyzing trace: $tracePath" -ForegroundColor Cyan
dotnet run --project $constraintsDir -- `
    --trace "$tracePath" `
    --report (Join-Path $outputDir "violations.json")
if ($LASTEXITCODE -ne 0) { throw "Constraint analysis failed (exit $LASTEXITCODE)." }

Write-Host "Analysis complete. See reports folder for details." -ForegroundColor Green
exit 0

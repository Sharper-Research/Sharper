$ErrorActionPreference = "Stop" # stop on any errors

# set up paths for the constraint analyzer
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path              # folder this script is in
$constraintsDir = Join-Path $scriptRoot "Constraints"                      # the c# constraint analyzer project
$tracePath = Resolve-Path (Join-Path $scriptRoot "..\Step1_Tracing\trace_logs.jsonl") # trace file from step 1
$outputDir = Join-Path $scriptRoot "reports"                               # folder for output reports

# make sure the trace file exists (step 1 must be run first)
if (-not (Test-Path $tracePath)) {
    Write-Error "Trace file not found at $tracePath. Run Step 1 first."
}

# create the reports folder if it doesnt exist yet
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# build the constraint analyzer project
Write-Host "Building Constraint Analyzer..." -ForegroundColor Cyan
dotnet build $constraintsDir | Out-Null # build output is noisy so pipe to null

# run the analyzer, passing in the trace file and where to write the violations report
Write-Host "Analyzing trace: $tracePath" -ForegroundColor Cyan
dotnet run --project $constraintsDir -- `
    --trace "$tracePath" `
    --report (Join-Path $outputDir "violations.json")

Write-Host "Analysis complete. See reports folder for details." -ForegroundColor Green

$ErrorActionPreference = "Stop"

$repoRoot       = Resolve-Path (Join-Path $PSScriptRoot "..")
$step1Script    = Join-Path $repoRoot "AutoRepair\Step1_Tracing\generate_trace.ps1"
$restoreScript  = Join-Path $repoRoot "AutoRepair\Step1_Tracing\restore_project.ps1"
$step2Script    = Join-Path $repoRoot "AutoRepair\Step2_Constraints\analyze_trace.ps1"
$step3Script    = Join-Path $repoRoot "AutoRepair\Step3_Repair\build_cfg.ps1"
$sourceDir      = Join-Path $repoRoot "ThrottleDebounce\ThrottleDebounce"

Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "   AutoRepair: Full Pipeline (Steps 1-3)"     -ForegroundColor Magenta
Write-Host "   Target: ThrottleDebounce"                   -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

#--- STEP 1: Tracing ---
Write-Host ">>> STEP 1: Instrument + Trace <<<" -ForegroundColor Cyan
& $step1Script
if ($LASTEXITCODE -ne 0) {
    Write-Host "Step 1 failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""

#--- STEP 2: Constraint Analysis ---
Write-Host ">>> STEP 2: Analyze Constraints <<<" -ForegroundColor Cyan
& $step2Script
if ($LASTEXITCODE -ne 0) {
    Write-Host "Step 2 failed!" -ForegroundColor Red
    exit 1
}
Write-Host ""

#--- Restore original source before CFG analysis ---
Write-Host "Restoring original (uninstrumented) source for CFG analysis..." -ForegroundColor Yellow
& $restoreScript $sourceDir | Out-Null
Write-Host ""

#--- STEP 3 Task 1: Build CFG ---
Write-Host ">>> STEP 3 (Task 1): Build CFG <<<" -ForegroundColor Cyan
& $step3Script -SourceDir $sourceDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "Step 3 failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "   Full Pipeline Complete!"                    -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  Trace:      AutoRepair\Step1_Tracing\trace_logs.jsonl"
Write-Host "  Violations: AutoRepair\Step2_Constraints\reports\violations.json"
Write-Host "  CFG Report: AutoRepair\Step3_Repair\reports\cfg_report.json"

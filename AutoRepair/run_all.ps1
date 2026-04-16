param(
    [string]$SourceDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot       = Resolve-Path (Join-Path $PSScriptRoot "..")
$step1Script    = Join-Path $repoRoot "AutoRepair\Step1_Tracing\generate_trace.ps1"
$restoreScript  = Join-Path $repoRoot "AutoRepair\Step1_Tracing\restore_project.ps1"
$step2Script    = Join-Path $repoRoot "AutoRepair\Step2_Constraints\analyze_trace.ps1"
$step3Script    = Join-Path $repoRoot "AutoRepair\Step3_Repair\build_cfg.ps1"
if (-not $SourceDir) { $SourceDir = Join-Path $repoRoot "ThrottleDebounce\ThrottleDebounce" }

Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "   AutoRepair: Proactive Repair from One Fault"-ForegroundColor Magenta
Write-Host "   Target: $SourceDir"                         -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

#--- STEP 1: Concrete execution — capture trace + state for one failing input
Write-Host ">>> STEP 1: Instrument + Trace (seed from one failing input) <<<" -ForegroundColor Cyan
& $step1Script -TargetSourceDir $SourceDir
if ($LASTEXITCODE -ne 0) { Write-Host "Step 1 failed!" -ForegroundColor Red; exit 1 }
Write-Host ""

#--- STEP 2: Derive constraints from trace via typing rules
Write-Host ">>> STEP 2: Derive Repair Constraints from Trace <<<" -ForegroundColor Cyan
& $step2Script
if ($LASTEXITCODE -ne 0) { Write-Host "Step 2 failed!" -ForegroundColor Red; exit 1 }
Write-Host ""

#--- Restore original source before static CFG analysis
Write-Host "Restoring original (uninstrumented) source for static analysis..." -ForegroundColor Yellow
& $restoreScript $SourceDir | Out-Null
Write-Host ""

#--- STEP 3: Forward-propagate constraints on Roslyn CFG, synthesize patch
Write-Host ">>> STEP 3 (Task 1): Build CFG for constraint propagation <<<" -ForegroundColor Cyan
& $step3Script -SourceDir $SourceDir
if ($LASTEXITCODE -ne 0) { Write-Host "Step 3 failed!" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "   Full Pipeline Complete!"                    -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  Trace (Step 1):       AutoRepair\Step1_Tracing\trace_logs.jsonl"
Write-Host "  Constraints (Step 2): AutoRepair\Step2_Constraints\reports\violations.json"
Write-Host "  CFG (Step 3):         AutoRepair\Step3_Repair\reports\cfg_report.json"

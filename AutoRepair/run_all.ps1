param(
    [string]$SourceDir = "",
    [string]$DriverProject = "",
    [string]$Solution = ""
)

$ErrorActionPreference = "Stop"

$repoRoot         = Resolve-Path (Join-Path $PSScriptRoot "..")
$step1Script      = Join-Path $repoRoot "AutoRepair\Step1_Tracing\generate_trace.ps1"
$restoreScript    = Join-Path $repoRoot "AutoRepair\Step1_Tracing\restore_project.ps1"
$step2Script      = Join-Path $repoRoot "AutoRepair\Step2_Constraints\analyze_trace.ps1"
$cfgAnalyzerProj  = Join-Path $repoRoot "AutoRepair\CFGAnalyzer\CFGAnalyzer.csproj"
$cfgReportDir     = Join-Path $repoRoot "AutoRepair\Step3_Repair\reports"
$cfgReportLog     = Join-Path $cfgReportDir "cfg_analyzer.log"

if (-not $SourceDir)     { $SourceDir     = Join-Path $repoRoot "ThrottleDebounce\ThrottleDebounce" }
if (-not $DriverProject) { $DriverProject = Join-Path $repoRoot "ThrottleDebounce\Tests\Tests.csproj" }
if (-not $Solution)      { $Solution      = Join-Path $repoRoot "ThrottleDebounce\ThrottleDebounce\ThrottleDebounce.csproj" }

if (-not (Test-Path $cfgReportDir)) { New-Item -ItemType Directory -Path $cfgReportDir | Out-Null }

Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "   AutoRepair: Proactive Repair from One Fault"-ForegroundColor Magenta
Write-Host "   Target source : $SourceDir"                 -ForegroundColor Magenta
Write-Host "   Test driver   : $DriverProject"             -ForegroundColor Magenta
Write-Host "   CFG solution  : $Solution"                  -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

#--- STEP 1: Concrete execution - capture trace + state from the test suite
Write-Host ">>> STEP 1: Instrument + Trace <<<" -ForegroundColor Cyan
& $step1Script -TargetSourceDir $SourceDir -DriverProject $DriverProject
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

#--- STEP 3: Build CFG + call graph + path conditions (CFGAnalyzer by jpksh90)
Write-Host ">>> STEP 3: Build CFG and Path Conditions (CFGAnalyzer) <<<" -ForegroundColor Cyan
Write-Host "Building CFGAnalyzer..." -ForegroundColor Gray
dotnet build $cfgAnalyzerProj | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "CFGAnalyzer build failed!" -ForegroundColor Red; exit 1 }

Write-Host "Running CFGAnalyzer on $Solution" -ForegroundColor Gray
Write-Host "(output captured to $cfgReportLog)" -ForegroundColor Gray
$cfgOutput = & dotnet run --project $cfgAnalyzerProj --no-build -- "$Solution" 2>&1
$cfgExit = $LASTEXITCODE
$cfgOutput | Tee-Object -FilePath $cfgReportLog
if ($cfgExit -ne 0) { Write-Host "CFGAnalyzer run failed (exit $cfgExit)!" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "   Full Pipeline Complete!"                    -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  Trace (Step 1):       AutoRepair\Step1_Tracing\trace_logs.jsonl"
Write-Host "  Constraints (Step 2): AutoRepair\Step2_Constraints\reports\violations.json"
Write-Host "  CFG (Step 3):         AutoRepair\Step3_Repair\reports\cfg_analyzer.log"

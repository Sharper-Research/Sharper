param(
    [string]$SourceDir = "",
    [string]$DriverProject = "",
    [string]$Solution = "",
    [string]$ValidationRoot = "",
    [string]$ReproducerFilter = "",
    [switch]$ApplyPatches,
    [switch]$RestorePatches
)

$ErrorActionPreference = "Stop"

$repoRoot         = Resolve-Path (Join-Path $PSScriptRoot "..")
$step1Script      = Join-Path $repoRoot "Sharper\Step1_Tracing\generate_trace.ps1"
$restoreScript    = Join-Path $repoRoot "Sharper\Step1_Tracing\restore_project.ps1"
$step2Script      = Join-Path $repoRoot "Sharper\Step2_Constraints\analyze_trace.ps1"
$cfgAnalyzerProj  = Join-Path $repoRoot "Sharper\Step3_Repair\CFGAnalyzer\CFGAnalyzer.csproj"
$cfgReportDir     = Join-Path $repoRoot "Sharper\Step3_Repair\reports"
$cfgReportLog     = Join-Path $cfgReportDir "cfg_analyzer.log"
$violationsPath   = Join-Path $repoRoot "Sharper\Step2_Constraints\reports\violations.json"

if (-not $SourceDir)     { $SourceDir     = Join-Path $repoRoot "review\fixtures\runtime\RuntimeCode" }
if (-not $DriverProject) { $DriverProject = Join-Path $repoRoot "review\fixtures\runtime\RuntimeCode.Tests\RuntimeCode.Tests.csproj" }
if (-not $Solution)      { $Solution      = Join-Path $repoRoot "review\fixtures\runtime\RuntimeCode\RuntimeCode.csproj" }
if (($ValidationRoot -and -not $ReproducerFilter) -or ($ReproducerFilter -and -not $ValidationRoot) -or
    ($ApplyPatches -and -not $ValidationRoot) -or ($ApplyPatches -and $RestorePatches)) {
    throw "ValidationRoot and ReproducerFilter must be supplied together; ApplyPatches requires validation and cannot be combined with RestorePatches."
}

if (-not (Test-Path $cfgReportDir)) { New-Item -ItemType Directory -Path $cfgReportDir | Out-Null }

Write-Host "=============================================" -ForegroundColor Magenta
Write-Host "   Sharper: Proactive Repair from One Fault"-ForegroundColor Magenta
Write-Host "   Target source : $SourceDir"                 -ForegroundColor Magenta
Write-Host "   Test driver   : $DriverProject"             -ForegroundColor Magenta
Write-Host "   CFG solution  : $Solution"                  -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

#--- STEP 1: Concrete execution - capture trace + state from the test suite
Write-Host ">>> STEP 1: Instrument + Trace <<<" -ForegroundColor Cyan
if ($DriverProject) {
    & $step1Script -TargetSourceDir $SourceDir -TestProject $DriverProject
} else {
    & $step1Script -TargetSourceDir $SourceDir
}
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

$cfgArgs = @("$Solution")
if (Test-Path $violationsPath) {
    Write-Host "Driving CFGAnalyzer from violations: $violationsPath" -ForegroundColor Gray
    $cfgArgs += @("--violations", $violationsPath, "--output-dir", $cfgReportDir)
    if ($ValidationRoot) { $cfgArgs += @("--validation-root", $ValidationRoot, "--test-project", $DriverProject, "--reproducer-filter", $ReproducerFilter) }
    if ($ApplyPatches)   { $cfgArgs += "--apply-patches" }
    if ($RestorePatches) { $cfgArgs += "--restore" }
}

$cfgOutput = & dotnet run --project $cfgAnalyzerProj --no-build -- @cfgArgs 2>&1
$cfgExit = $LASTEXITCODE
$cfgOutput | Tee-Object -FilePath $cfgReportLog
if ($cfgExit -ne 0) { Write-Host "CFGAnalyzer run failed (exit $cfgExit)!" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "   Full Pipeline Complete!"                    -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  Trace (Step 1):       Sharper\Step1_Tracing\trace_logs.jsonl"
Write-Host "  Constraints (Step 2): Sharper\Step2_Constraints\reports\violations.json"
Write-Host "  CFG (Step 3):         Sharper\Step3_Repair\reports\cfg_analyzer.log"
Write-Host "  Repair plan (Step 3): Sharper\Step3_Repair\reports\repair_plan.json"
Write-Host "  Patches (Step 3):     Sharper\Step3_Repair\reports\patches\"

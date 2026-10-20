#Requires -Version 5.1
param(
    [string]$SourceDir = "",
    [string]$DriverProject = "",
    [string]$Solution = "",
    [string]$ValidationRoot = "",
    [string]$ReproducerFilter = "",
    [string]$TestFilter = "",
    [string]$OutputDir = "",
    [switch]$ApplyPatches,
    [switch]$RestorePatches
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$step1Script = Join-Path $PSScriptRoot "Step1_Tracing/generate_trace.ps1"
$restoreScript = Join-Path $PSScriptRoot "Step1_Tracing/restore_project.ps1"
$step2Script = Join-Path $PSScriptRoot "Step2_Constraints/analyze_trace.ps1"
$cfgAnalyzerProj = Join-Path $PSScriptRoot "Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj"

if (-not $SourceDir) { $SourceDir = Join-Path $repoRoot "benchmarks/MicrosoftBugDemo/RuntimeCode" }
if (-not $DriverProject) { $DriverProject = Join-Path $repoRoot "benchmarks/MicrosoftBugDemo/RuntimeCode.Tests/RuntimeCode.Tests.csproj" }
if (-not $Solution) { $Solution = Join-Path $repoRoot "benchmarks/MicrosoftBugDemo/RuntimeCode/RuntimeCode.csproj" }
if (($ValidationRoot -and -not $ReproducerFilter) -or ($ReproducerFilter -and -not $ValidationRoot) -or
    ($ApplyPatches -and -not $ValidationRoot) -or ($ApplyPatches -and $RestorePatches)) {
    throw "ValidationRoot and ReproducerFilter must be supplied together; ApplyPatches requires validation and cannot be combined with RestorePatches."
}

if (-not $OutputDir) {
    $OutputDir = "results/run-{0}-{1}" -f (Get-Date).ToString("yyyyMMdd-HHmmss"), (Split-Path $SourceDir -Leaf)
}
if (-not [IO.Path]::IsPathRooted($OutputDir)) { $OutputDir = Join-Path $repoRoot $OutputDir }
$OutputDir = [IO.Path]::GetFullPath($OutputDir)
$step1Out = Join-Path $OutputDir "step1_traces"
$step2Out = Join-Path $OutputDir "step2_constraints"
$step3Out = Join-Path $OutputDir "step3_repairs"
foreach ($dir in @($step1Out, $step2Out, $step3Out)) {
    if (Test-Path $dir) { throw "Use a fresh output directory; previous step outputs exist at $dir" }
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}
$tracePath = Join-Path $step1Out "trace_logs.jsonl"
$violationsPath = Join-Path $step2Out "violations.json"
$cfgReportLog = Join-Path $step3Out "cfg_analyzer.log"

Write-Host "Sharper target: $SourceDir" -ForegroundColor Magenta
Write-Host "Results: $OutputDir" -ForegroundColor Magenta
try {
    Write-Host ">>> STEP 1: Instrument + Trace <<<" -ForegroundColor Cyan
    & $step1Script -TargetSourceDir $SourceDir -TestProject $DriverProject `
        -TraceLogPath $tracePath -TestResultsDirectory $step1Out -TestFilter $TestFilter
    if ($LASTEXITCODE -ne 0) { throw "Step 1 failed (exit $LASTEXITCODE)." }

    Write-Host ">>> STEP 2: Derive Repair Constraints from Trace <<<" -ForegroundColor Cyan
    & $step2Script -TracePath $tracePath -OutputDir $step2Out
    if ($LASTEXITCODE -ne 0) { throw "Step 2 failed (exit $LASTEXITCODE)." }
} finally {
    & $restoreScript $SourceDir | Out-Null
}

Write-Host ">>> STEP 3: Build CFG and Path Conditions <<<" -ForegroundColor Cyan
dotnet build $cfgAnalyzerProj --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "CFGAnalyzer build failed." }
$cfgArgs = @($Solution, "--violations", $violationsPath, "--output-dir", $step3Out)
if ($ValidationRoot) {
    $cfgArgs += @("--validation-root", $ValidationRoot, "--test-project", $DriverProject, "--reproducer-filter", $ReproducerFilter)
}
if ($ApplyPatches) { $cfgArgs += "--apply-patches" }
if ($RestorePatches) { $cfgArgs += "--restore" }
& dotnet run --project $cfgAnalyzerProj --no-build -- @cfgArgs |
    Tee-Object -FilePath $cfgReportLog
if ($LASTEXITCODE -ne 0) { throw "CFGAnalyzer failed (exit $LASTEXITCODE). See $cfgReportLog" }
if (-not (Test-Path (Join-Path $step3Out "repair_plan.json"))) { throw "CFGAnalyzer produced no repair plan." }
Write-Host "Full pipeline complete. Results: $OutputDir" -ForegroundColor Green
exit 0

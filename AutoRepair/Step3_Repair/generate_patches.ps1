# script parameters for patch generation
param(
    [string]$ViolationsPath = "",    # path to the violations.json from step 2
    [string]$TargetSourceDir = "",   # folder with the source code to patch
    [switch]$Apply                   # if set, actually write patches to files (otherwise dry run)
)

$ErrorActionPreference = "Stop" # stop on any errors

# set up paths
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path           # folder this script lives in
$repairProject = Join-Path $scriptRoot "Repair\Repair.csproj"          # the c# repair/patch generator project
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..\..")               # go up two levels to repo root
$outputDir = Join-Path $scriptRoot "reports"                           # folder for output reports

# if no violations path given, default to the step 2 output
if ([string]::IsNullOrWhiteSpace($ViolationsPath)) {
    $ViolationsPath = Join-Path $scriptRoot "..\Step2_Constraints\reports\violations.json"
}

# if no target source dir given, default to the BrightHrKata checkout project
if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "BrightHrKata\Checkout"
}

# check that the violations file exists (step 2 must run first)
if (-not (Test-Path $ViolationsPath)) {
    Write-Error "Violations file not found at $ViolationsPath. Run Step 2 first."
}

# resolve paths to absolute so nothing gets confused
$ViolationsPath = Resolve-Path $ViolationsPath
$TargetSourceDir = Resolve-Path $TargetSourceDir

# create reports folder if it doesnt exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$reportPath = Join-Path $outputDir "patches.json" # where the patch report will be written

# print what were about to do
Write-Host "=== Step 3: Generating Patches ===" -ForegroundColor Cyan
Write-Host "Violations: $ViolationsPath"
Write-Host "Target:     $TargetSourceDir"
Write-Host "Report:     $reportPath"
Write-Host ""

# build the repair project first
Write-Host "Building Patch Generator..." -ForegroundColor Yellow
dotnet build $repairProject --nologo
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed!"; exit 1 } # bail if build fails

# set up the command line args for the repair tool
$runArgs = @(
    "--violations", "$ViolationsPath",   # where to find the violations
    "--target", "$TargetSourceDir",      # where the source code is
    "--report", "$reportPath"            # where to write the patches report
)

# if -Apply is set, add the apply flag so patches actually get written to source files
if ($Apply) {
    $runArgs += "--apply"
    Write-Host "Mode: APPLY (patches will be written to source files)" -ForegroundColor Red
} else {
    Write-Host "Mode: DRY-RUN (report only; use -Apply to write patches)" -ForegroundColor Yellow
}

Write-Host ""
dotnet run --project $repairProject --no-build -- @runArgs # run the repair tool (already built above)

if ($LASTEXITCODE -ne 0) { # patch generation failed
    Write-Error "Patch generation failed!"
    exit 1
}

Write-Host ""
Write-Host "Patch generation complete. See reports folder for details." -ForegroundColor Green

# if we were in dry run mode, remind the user how to apply
if (-not $Apply) {
    Write-Host ""
    Write-Host "To apply patches to source files, re-run with -Apply:" -ForegroundColor DarkYellow
    Write-Host "  .\AutoRepair\Step3_Repair\generate_patches.ps1 -Apply" -ForegroundColor DarkYellow
}

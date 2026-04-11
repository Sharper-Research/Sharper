param(
    [string]$SourceDir = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot  = Split-Path -Parent $MyInvocation.MyCommand.Path
$repairDir   = Join-Path $scriptRoot "Repair"
$violationsPath = Join-Path $scriptRoot "..\Step2_Constraints\reports\violations.json"
$outputDir   = Join-Path $scriptRoot "reports"
$repoRoot    = Resolve-Path (Join-Path $scriptRoot "..\..")

if (-not (Test-Path $violationsPath)) {
    Write-Error "Violations file not found at $violationsPath. Run Step 2 first."
}
$violationsPath = Resolve-Path $violationsPath

# Auto-detect source directory from the first violation's file path
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    $violations = Get-Content $violationsPath | ConvertFrom-Json
    if ($violations.Count -gt 0) {
        $firstFile = $violations[0].file

        $candidates = @(
            (Join-Path $repoRoot "SharpZipLib\src\ICSharpCode.SharpZipLib"),
            (Join-Path $repoRoot "ThrottleDebounce\ThrottleDebounce"),
            (Join-Path $repoRoot "ThrottleDebounce")
        )

        foreach ($candidate in $candidates) {
            if (Test-Path $candidate) {
                $testPath = Join-Path $candidate $firstFile
                if (Test-Path $testPath) {
                    $SourceDir = $candidate
                    Write-Host "Auto-detected source directory: $SourceDir" -ForegroundColor Cyan
                    break
                }
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($SourceDir)) {
        Write-Error "Cannot auto-detect source directory. Use -SourceDir parameter."
    }
}

$SourceDir = Resolve-Path $SourceDir

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host "=== Building CFG (Step 3 - Task 1) ===" -ForegroundColor Cyan
Write-Host "Violations: $violationsPath"
Write-Host "Source:     $SourceDir"
Write-Host ""

Write-Host "Building Repair project..." -ForegroundColor Yellow
dotnet build $repairDir --nologo -v q

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "Running CFG Builder..." -ForegroundColor Yellow
dotnet run --project $repairDir --no-build -- `
    --violations "$violationsPath" `
    --source "$SourceDir" `
    --report (Join-Path $outputDir "cfg_report.json")

if ($LASTEXITCODE -ne 0) {
    Write-Error "CFG Builder failed!"
    exit 1
}

Write-Host ""
Write-Host "=== CFG Build Complete ===" -ForegroundColor Green
$reportPath = Join-Path $outputDir 'cfg_report.json'
Write-Host "Report: $reportPath"

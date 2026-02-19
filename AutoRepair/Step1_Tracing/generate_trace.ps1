param(
    [string]$TargetSourceDir = "",
    [string]$DriverProject = "",
    [string]$TestFilter = "",
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$logFileTxt = Join-Path $PSScriptRoot "trace_logs.txt"
$logFileJson = Join-Path $PSScriptRoot "trace_logs.jsonl"
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"

if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "BrightHrKata\Checkout"
}
if ([string]::IsNullOrWhiteSpace($DriverProject)) {
    $DriverProject = Join-Path $repoRoot "BrightHrKata\Checkout.Tests\Checkout.Tests.csproj"
}

$TargetSourceDir = Resolve-Path $TargetSourceDir
$DriverProject = Resolve-Path $DriverProject

Write-Host "=== Step 1: Generating Trace Logs (Checkout) ===" -ForegroundColor Cyan
Write-Host "Target source: $TargetSourceDir"
Write-Host "Driver project: $DriverProject"

if (-not $SkipRestore -and (Test-Path $restoreScript)) {
    Write-Host "0. Restoring instrumented files from backups..." -ForegroundColor Yellow
    & $restoreScript $TargetSourceDir | Out-Null
}

# Clean previous logs
if (Test-Path $logFileJson) { Remove-Item $logFileJson }
if (Test-Path $logFileTxt) { Remove-Item $logFileTxt }

# Ensure log files exist even when test execution fails early.
New-Item -ItemType File -Path $logFileJson -Force | Out-Null
New-Item -ItemType File -Path $logFileTxt -Force | Out-Null

Write-Host "1. Running Instrumenter on Directory: $TargetSourceDir" -ForegroundColor Yellow
dotnet run --project $instrumenterProject -- "$TargetSourceDir"
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed!"; exit 1 }

Write-Host "2. Running test driver to execute instrumented Checkout code..." -ForegroundColor Yellow
Write-Host "   JSON Log: $logFileJson"

# Set environment variable for the JSON log path so TraceLogger knows where to write.
$env:TRACE_LOG_JSON_PATH = $logFileJson

$testArgs = @("test", "$DriverProject", "-f", "net10.0", "--verbosity", "minimal")
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $testArgs += @("--filter", $TestFilter)
}

& dotnet @testArgs *> $logFileTxt
$testExitCode = $LASTEXITCODE

if ($testExitCode -ne 0) {
    Write-Host "=== Test driver failed ===" -ForegroundColor Red

    $failedTest = $null
    $failedTestLine = Select-String -Path $logFileTxt -Pattern '^\s*Failed\s+(.+?)\s+\[' | Select-Object -First 1
    if ($failedTestLine) {
        $failedTest = [regex]::Match($failedTestLine.Line, '^\s*Failed\s+(.+?)\s+\[').Groups[1].Value
    }

    $failedLocation = $null
    $failedLocationLine = Select-String -Path $logFileTxt -Pattern ' in .+?:line \d+' | Select-Object -First 1
    if ($failedLocationLine) {
        $failedLocationMatch = [regex]::Match($failedLocationLine.Line, ' in (.+?):line (\d+)')
        if ($failedLocationMatch.Success) {
            $failedLocation = "$($failedLocationMatch.Groups[1].Value):$($failedLocationMatch.Groups[2].Value)"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($failedTest)) {
        Write-Host "First failed test: $failedTest" -ForegroundColor Yellow
    }
    if (-not [string]::IsNullOrWhiteSpace($failedLocation)) {
        Write-Host "Failure location: $failedLocation" -ForegroundColor Yellow
    }

    if (-not (Test-Path $logFileJson)) {
        New-Item -ItemType File -Path $logFileJson -Force | Out-Null
    }

    Write-Host "Trace logs available:" -ForegroundColor Yellow
    Write-Host "  $logFileTxt"
    Write-Host "  $logFileJson"
    exit 1
}

Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Trace logs available:"
Write-Host "  $logFileTxt"
Write-Host "  $logFileJson"

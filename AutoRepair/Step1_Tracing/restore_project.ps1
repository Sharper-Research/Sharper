param(
    [string]$targetDir,
    [switch]$ScanRoot
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($targetDir)) {
    # Default to Checkout source folder; fallback to NodaTime, then TargetApp.
    $checkoutDefault = Join-Path $PSScriptRoot "..\..\BrightHrKata\Checkout"
    $nodaDefault = Join-Path $PSScriptRoot "..\..\nodatime\src\NodaTime"
    $targetAppDefault = Join-Path $PSScriptRoot "..\..\TargetApp"
    if (Test-Path $checkoutDefault) {
        $targetDir = $checkoutDefault
    }
    elseif (Test-Path $nodaDefault) {
        $targetDir = $nodaDefault
    }
    else {
        $targetDir = $targetAppDefault
    }
}

if (!(Test-Path $targetDir)) {
    Write-Error "Target directory not found: $targetDir"
    exit 1
}

$targetDir = Resolve-Path $targetDir

# In --ScanRoot mode, restore each library's src/ subdirectory
if ($ScanRoot) {
    Write-Host "Restoring all library projects under: $targetDir" -ForegroundColor Yellow
    $libDirs = Get-ChildItem $targetDir -Directory
    $totalRestored = 0
    foreach ($lib in $libDirs) {
        $srcDir = Join-Path $lib.FullName "src"
        if (!(Test-Path $srcDir)) { continue }
        # Check for .bak files (both .cs.bak and .csproj.bak)
        $bakFiles = Get-ChildItem -Path $srcDir -Filter "*.bak" -Recurse -ErrorAction SilentlyContinue
        $loggerPath = Join-Path $srcDir "TraceLogger.cs"
        $hasLogger = Test-Path $loggerPath
        if ($bakFiles.Count -eq 0 -and -not $hasLogger) { continue }

        Write-Host "  Restoring: $($lib.Name)" -ForegroundColor Gray
        if ($hasLogger) {
            Remove-Item $loggerPath -Force
        }
        foreach ($bak in $bakFiles) {
            $originalPath = $bak.FullName.Substring(0, $bak.FullName.Length - 4)
            Move-Item -Path $bak.FullName -Destination $originalPath -Force
            $totalRestored++
        }
    }
    Write-Host "=== Scan-Root Restoration Complete ($totalRestored files restored) ===" -ForegroundColor Green
    return
}

Write-Host "Restoring project at: $targetDir" -ForegroundColor Yellow

# 1. Remove generated TraceLogger.cs
$logger = Join-Path $targetDir "TraceLogger.cs"
if (Test-Path $logger) {
    Remove-Item $logger -Force
    Write-Host "[-] Removed TraceLogger.cs" -ForegroundColor Gray
}

# 2. Restore backup files (*.cs.bak -> *.cs)
$bakFiles = Get-ChildItem -Path $targetDir -Filter "*.bak" -Recurse

if ($bakFiles.Count -eq 0) {
    Write-Host "No backup files (*.bak) found. Project might be clean already." -ForegroundColor Cyan
}

foreach ($bak in $bakFiles) {
    $originalPath = $bak.FullName.Substring(0, $bak.FullName.Length - 4)
    
    Move-Item -Path $bak.FullName -Destination $originalPath -Force
    Write-Host "[*] Restored $(Split-Path $originalPath -Leaf)" -ForegroundColor Gray
}

Write-Host "=== Restoration Complete ===" -ForegroundColor Green

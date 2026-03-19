param([string]$targetDir)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($targetDir)) {
    # Default to MicrosoftBugDemo source folder; fallback to TargetApp.
    $microsoftBugDemoDefault = Join-Path $PSScriptRoot "..\..\MicrosoftBugDemo\RuntimeCode"
    $targetAppDefault = Join-Path $PSScriptRoot "..\..\TargetApp"
    if (Test-Path $microsoftBugDemoDefault) {
        $targetDir = $microsoftBugDemoDefault
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

param([string]$targetDir)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($targetDir)) {
    $targetDir = Join-Path $PSScriptRoot "..\..\review\fixtures\runtime\RuntimeCode"
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
$bakFiles = @(Get-ChildItem -Path $targetDir -Filter "*.cs.bak" -File -Recurse)

if ($bakFiles.Count -eq 0) {
    Write-Host "No instrumentation backups (*.cs.bak) found. Project might be clean already." -ForegroundColor Cyan
}

foreach ($bak in $bakFiles) {
    $originalPath = $bak.FullName.Substring(0, $bak.FullName.Length - 4)
    
    Move-Item -Path $bak.FullName -Destination $originalPath -Force
    Write-Host "[*] Restored $(Split-Path $originalPath -Leaf)" -ForegroundColor Gray
}

Write-Host "=== Restoration Complete ===" -ForegroundColor Green

$ErrorActionPreference = "Stop"

$repoSrc = Resolve-Path (Join-Path $PSScriptRoot "..\..\command-line-api\src")

Write-Host "=== Restoring command-line-api repository ===" -ForegroundColor Cyan
Write-Host "Root: $repoSrc"

# 1. Remove every generated TraceLogger.cs
$loggers = Get-ChildItem -Path $repoSrc -Filter "TraceLogger.cs" -Recurse
foreach ($f in $loggers) {
    Remove-Item $f.FullName -Force
    Write-Host "[-] Removed $($f.FullName)" -ForegroundColor Gray
}

# 2. Restore all *.bak -> *.cs (or original extension)
$bakFiles = Get-ChildItem -Path $repoSrc -Filter "*.bak" -Recurse

if ($bakFiles.Count -eq 0) {
    Write-Host "No backup files found - repository is already clean." -ForegroundColor Green
} else {
    foreach ($bak in $bakFiles) {
        $originalPath = $bak.FullName.Substring(0, $bak.FullName.Length - 4)
        Move-Item -Path $bak.FullName -Destination $originalPath -Force
        Write-Host "[*] Restored $(Split-Path $originalPath -Leaf)" -ForegroundColor Gray
    }
    Write-Host "Restored $($bakFiles.Count) file(s)." -ForegroundColor Green
}

Write-Host "=== Restoration Complete ===" -ForegroundColor Green

# takes a target directory path, undoes instrumentation by restoring .bak files
param([string]$targetDir)

$ErrorActionPreference = "Stop" # stop on errors

# if no target dir provided, try to find a reasonable default
if ([string]::IsNullOrEmpty($targetDir)) {
    # check these projects in order: Checkout, NodaTime, TargetApp
    $checkoutDefault = Join-Path $PSScriptRoot "..\..\BrightHrKata\Checkout"
    $nodaDefault = Join-Path $PSScriptRoot "..\..\nodatime\src\NodaTime"
    $targetAppDefault = Join-Path $PSScriptRoot "..\..\TargetApp"
    if (Test-Path $checkoutDefault) {          # try checkout first
        $targetDir = $checkoutDefault
    }
    elseif (Test-Path $nodaDefault) {          # then nodatime
        $targetDir = $nodaDefault
    }
    else {                                      # fall back to targetapp
        $targetDir = $targetAppDefault
    }
}

# make sure the directory actually exists
if (!(Test-Path $targetDir)) {
    Write-Error "Target directory not found: $targetDir"
    exit 1
}

$targetDir = Resolve-Path $targetDir # resolve to full path
Write-Host "Restoring project at: $targetDir" -ForegroundColor Yellow

# step 1: delete the TraceLogger.cs file that the instrumenter injected
$logger = Join-Path $targetDir "TraceLogger.cs"
if (Test-Path $logger) {
    Remove-Item $logger -Force                    # delete it
    Write-Host "[-] Removed TraceLogger.cs" -ForegroundColor Gray
}

# step 2: find all .bak files and restore the originals
# the instrumenter makes backups like Foo.cs.bak before modifying Foo.cs
$bakFiles = Get-ChildItem -Path $targetDir -Filter "*.bak" -Recurse # find all .bak files recursively

if ($bakFiles.Count -eq 0) {  # no backups means nothing was instrumented (or already restored)
    Write-Host "No backup files (*.bak) found. Project might be clean already." -ForegroundColor Cyan
}

# loop through each backup and move it back to the original filename
foreach ($bak in $bakFiles) {
    # strip the .bak extension to get the original path (e.g. Foo.cs.bak -> Foo.cs)
    $originalPath = $bak.FullName.Substring(0, $bak.FullName.Length - 4)
    
    Move-Item -Path $bak.FullName -Destination $originalPath -Force # overwrite the instrumented version
    Write-Host "[*] Restored $(Split-Path $originalPath -Leaf)" -ForegroundColor Gray
}

Write-Host "=== Restoration Complete ===" -ForegroundColor Green

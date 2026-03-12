<#
.SYNOPSIS
    Restores all instrumented runtime source files back to their original state.

.DESCRIPTION
    Undoes everything done by generate_trace_runtime.ps1:
      - Restores all .cs.bak files back to .cs in the Serialization folder
      - Removes generated TraceLogger.cs from the Serialization folder
      - Removes TraceLogger.cs reference from System.Private.Xml.csproj

.EXAMPLE
    .\restore_runtime.ps1
#>

$ErrorActionPreference = "Stop"

$scriptDir        = $PSScriptRoot
$repoRoot         = Resolve-Path (Join-Path $scriptDir "..\..")
$runtimeRoot      = Join-Path $repoRoot "runtime"
$serializationSrc = Join-Path $runtimeRoot "src\libraries\System.Private.Xml\src\System\Xml\Serialization"
$csprojPath       = Join-Path $runtimeRoot "src\libraries\System.Private.Xml\src\System.Private.Xml.csproj"
$restoreScript    = Join-Path $scriptDir "restore_project.ps1"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  AutoRepair - Restore Runtime Source Files" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host ""

# Step 1: Restore ALL .bak files + remove TraceLogger.cs from the Serialization folder
Write-Host "[1] Restoring all instrumented source files in Serialization folder..." -ForegroundColor Yellow
& $restoreScript $serializationSrc

# Step 2: Remove TraceLogger.cs entry from csproj
Write-Host "[2] Cleaning up System.Private.Xml.csproj..." -ForegroundColor Yellow
$content = Get-Content $csprojPath -Raw
if ($content -match 'TraceLogger\.cs') {
    $content = $content -replace '(\r?\n)\s*<Compile Include="System\\Xml\\Serialization\\TraceLogger\.cs" />', ''
    Set-Content -Path $csprojPath -Value $content -NoNewline
    Write-Host "    Removed TraceLogger.cs reference from csproj." -ForegroundColor Green
} else {
    Write-Host "    csproj already clean - no TraceLogger.cs reference found." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  Restore complete. Source is back to original." -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""

<#
.SYNOPSIS
    Instruments all C# source files in the System.Xml.Serialization folder
    with AutoRepair tracing — targeting GitHub issue #120561.

.EXAMPLE
    .\generate_trace_runtime.ps1
#>

$ErrorActionPreference = "Stop"

# Paths
$scriptDir        = $PSScriptRoot
$repoRoot         = Resolve-Path (Join-Path $scriptDir "..\..")
$runtimeRoot      = Join-Path $repoRoot "runtime"
$serializationSrc = Join-Path $runtimeRoot "src\libraries\System.Private.Xml\src\System\Xml\Serialization"
$csprojPath       = Join-Path $runtimeRoot "src\libraries\System.Private.Xml\src\System.Private.Xml.csproj"
$instrumenterProj = Join-Path $scriptDir "Instrumenter\Instrumenter.csproj"
$restoreScript    = Join-Path $scriptDir "restore_project.ps1"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  AutoRepair Step-1 - Instrument Runtime Serialization" -ForegroundColor Cyan
Write-Host "  Target : System\Xml\Serialization folder" -ForegroundColor Cyan
Write-Host "  Bug    : GitHub issue #120561" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Step 0: Clean up any previous instrumentation
Write-Host "[0] Cleaning up any previous instrumentation..." -ForegroundColor Yellow
& $restoreScript $serializationSrc | Out-Null

# Also remove TraceLogger.cs csproj entry if present
$content = Get-Content $csprojPath -Raw
if ($content -match 'TraceLogger\.cs') {
    $removePattern = '(\r?\n)\s*<Compile Include="System\\Xml\\Serialization\\TraceLogger\.cs" />'
    $content = $content -replace $removePattern, ''
    Set-Content -Path $csprojPath -Value $content -NoNewline
    Write-Host "    Removed old TraceLogger.cs entry from csproj." -ForegroundColor DarkGray
}
Write-Host "    Done." -ForegroundColor DarkGray

# Step 1: Instrument ALL .cs files in the Serialization folder
Write-Host ""
Write-Host "[1] Instrumenting all files in:" -ForegroundColor Yellow
Write-Host "    $serializationSrc" -ForegroundColor White

dotnet run --project $instrumenterProj -- "$serializationSrc"
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed. Aborting."; exit 1 }

# Step 2: Register TraceLogger.cs in System.Private.Xml.csproj
Write-Host ""
Write-Host "[2] Adding TraceLogger.cs to System.Private.Xml.csproj..." -ForegroundColor Yellow
$content = Get-Content $csprojPath -Raw
if ($content -notmatch 'TraceLogger\.cs') {
    $pattern     = '<Compile Include="System\\Xml\\Serialization\\ReflectionXmlSerializationWriter\.cs" />'
    $replacement = '<Compile Include="System\Xml\Serialization\ReflectionXmlSerializationWriter.cs" />' + "`r`n    " + '<Compile Include="System\Xml\Serialization\TraceLogger.cs" />'
    $content = $content -replace $pattern, $replacement
    Set-Content -Path $csprojPath -Value $content -NoNewline
    Write-Host "    Added TraceLogger.cs to csproj." -ForegroundColor Green
} else {
    Write-Host "    Already present - skipped." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Instrumentation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor White
Write-Host "  1. Rebuild:  cd runtime && .\build.cmd -s libs.sfx -c Debug" -ForegroundColor Yellow
Write-Host "  2. Run test: .\build.cmd -s libs.tests -test -testnobuild -c Debug /p:Projects=""...\ReflectionOnly\...""" -ForegroundColor Yellow
Write-Host "  3. Restore:  .\AutoRepair\Step1_Tracing\restore_runtime.ps1" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

#Requires -Version 5.1
<#
.SYNOPSIS
    One-command setup for Sharper on Windows: installs what is missing, builds, then calls run_quick_test.ps1.

.DESCRIPTION
    1. Checks Windows x64 and PowerShell 5.1+.
    2. Installs the .NET 10 SDK for the current user (no admin rights) if no 10.0 SDK is found.
    3. Installs the Visual C++ 2015-2022 x64 runtime (needed by Z3's native DLL) if it is missing.
       This is the only step that asks for administrator rights (UAC prompt).
    4. Restores NuGet packages and builds Sharper.sln.
    5. Calls run_quick_test.ps1 and exits with its exit code (0 = QUICK TEST PASSED).

    Steps that are already satisfied are skipped, so the script is safe to run again.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\setup.ps1
#>
param(
    # Folder for a per-user .NET SDK install (used only when no .NET 10 SDK is found).
    [string]$DotnetInstallDir = (Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"),
    # Install the SDK into -DotnetInstallDir even if a .NET 10 SDK is already available.
    [switch]$ForceDotnetInstall,
    # Do not add a per-user SDK install to the user's PATH permanently (current session only).
    [switch]$NoPersistPath
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"   # Invoke-WebRequest is much faster without the progress bar
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$root = $PSScriptRoot
$downloads = Join-Path $root ".setup-downloads"

function Write-Step([string]$text) { Write-Host ""; Write-Host ">>> $text" -ForegroundColor Cyan }
function Write-Ok([string]$text) { Write-Host "    [ok] $text" -ForegroundColor Green }

function Get-Dotnet10Sdk {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) { return $null }
    $sdk = @(& $dotnet.Source --list-sdks 2>$null | Where-Object { $_ -match '^10\.0\.' }) | Select-Object -Last 1
    if ($sdk) { return [pscustomobject]@{ Path = $dotnet.Source; Sdk = ($sdk -split ' ')[0] } }
    return $null
}

# ---- 1. Platform ------------------------------------------------------------------------
Write-Step "Checking platform"
if ($env:OS -ne "Windows_NT") { throw "setup.ps1 is for Windows. On Linux or macOS, use Docker (see README)." }
if (-not [Environment]::Is64BitOperatingSystem -or $env:PROCESSOR_ARCHITECTURE -eq "ARM64") {
    throw "Sharper's native route needs Windows x64 (Z3's native library is x64 only)."
}
Write-Ok ("Windows x64, PowerShell {0}" -f $PSVersionTable.PSVersion)

# ---- 2. .NET 10 SDK ---------------------------------------------------------------------
Write-Step "Checking .NET 10 SDK"
$found = if ($ForceDotnetInstall) { $null } else { Get-Dotnet10Sdk }
if ($found) {
    Write-Ok ".NET SDK $($found.Sdk) at $($found.Path)"
} else {
    Write-Host "    Installing the latest .NET 10.0 SDK into $DotnetInstallDir (per user, no admin rights)"
    New-Item -ItemType Directory -Path $downloads -Force | Out-Null
    $installer = Join-Path $downloads "dotnet-install.ps1"
    Invoke-WebRequest -UseBasicParsing https://dot.net/v1/dotnet-install.ps1 -OutFile $installer
    & $installer -Channel 10.0 -InstallDir $DotnetInstallDir -NoPath
    if (-not (Test-Path (Join-Path $DotnetInstallDir "dotnet.exe"))) { throw ".NET SDK installation failed" }

    # Use the new SDK in this session (and in the processes this script starts).
    $env:DOTNET_ROOT = $DotnetInstallDir
    $env:DOTNET_MULTILEVEL_LOOKUP = "0"
    $env:PATH = "$DotnetInstallDir;$env:PATH"
    if (-not $NoPersistPath) {
        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if (($userPath -split ';') -notcontains $DotnetInstallDir) {
            [Environment]::SetEnvironmentVariable("Path", (@($DotnetInstallDir, $userPath) | Where-Object { $_ }) -join ';', "User")
        }
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $DotnetInstallDir, "User")
        Write-Host "    Added $DotnetInstallDir to your user PATH (open a new terminal for other windows to see it)."
    }
    $found = Get-Dotnet10Sdk
    if (-not $found) { throw ".NET 10 SDK still not found after installation" }
    Write-Ok ".NET SDK $($found.Sdk) installed at $($found.Path)"
}

# ---- 3. Visual C++ runtime (for Z3) -----------------------------------------------------
Write-Step "Checking Visual C++ 2015-2022 x64 runtime (needed by Z3)"
$system32 = Join-Path $env:SystemRoot "System32"
$vcDlls = @("vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll")
$missing = @($vcDlls | Where-Object { -not (Test-Path (Join-Path $system32 $_)) })
if ($missing.Count -eq 0) {
    Write-Ok "Installed ($($vcDlls -join ', '))"
} else {
    Write-Host "    Missing: $($missing -join ', '). Installing the Microsoft Visual C++ Redistributable."
    Write-Host "    Windows will ask for administrator permission." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $downloads -Force | Out-Null
    $redist = Join-Path $downloads "vc_redist.x64.exe"
    Invoke-WebRequest -UseBasicParsing https://aka.ms/vs/17/release/vc_redist.x64.exe -OutFile $redist
    $process = Start-Process $redist -ArgumentList "/install", "/quiet", "/norestart" -Verb RunAs -Wait -PassThru
    # 0 = installed, 3010 = installed (restart pending), 1638 = a newer version is already installed
    if ($process.ExitCode -notin 0, 3010, 1638) { throw "Visual C++ Redistributable install failed (exit $($process.ExitCode))" }
    Write-Ok "Installed (exit code $($process.ExitCode))"
}

# ---- 4. Restore and build ---------------------------------------------------------------
Write-Step "Restoring NuGet packages and building Sharper.sln (first run downloads packages)"
& dotnet restore (Join-Path $root "Sharper.sln") --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
& dotnet build (Join-Path $root "Sharper.sln") --no-restore --verbosity quiet --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
Write-Ok "Sharper.sln built"

if (Test-Path $downloads) { Remove-Item $downloads -Recurse -Force }

Write-Host ""
Write-Host "Environment ready." -ForegroundColor Green

# ---- 5. Quick test ----------------------------------------------------------------------
Write-Step "Running run_quick_test.ps1"
& (Join-Path $root "run_quick_test.ps1")
exit $LASTEXITCODE

param(
    [string]$TargetSourceDir = "",
    [string]$DriverProject = "",
    [string]$TestFilter = "",
    [switch]$SkipRestore,
    [switch]$UseProjectGraph,
    [bool]$StopOnFirstFailure = $true,
    [string]$LogFileTxtName = "trace_logs.txt",
    [string]$LogFileJsonName = "trace_logs.jsonl"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$logFileTxt = Join-Path $PSScriptRoot $LogFileTxtName
$logFileJson = Join-Path $PSScriptRoot $LogFileJsonName
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"

if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "MicrosoftBugDemo\RuntimeCode"
}
if ([string]::IsNullOrWhiteSpace($DriverProject)) {
    $DriverProject = Join-Path $repoRoot "MicrosoftBugDemo\RuntimeCode.Tests\RuntimeCode.Tests.csproj"
}

$TargetSourceDir = Resolve-Path $TargetSourceDir
$DriverProject = Resolve-Path $DriverProject

Write-Host "=== Step 1: Generating Trace Logs ===" -ForegroundColor Cyan
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

# Choose instrumentation mode. --UseProjectGraph walks <ProjectReference> entries
# from the test .csproj so you don't need to point at a specific subfolder.
if ($UseProjectGraph) {
    Write-Host "1. Running Instrumenter in project-graph mode from: $DriverProject" -ForegroundColor Yellow
    dotnet run --project $instrumenterProject -- --test-project "$DriverProject"
}
else {
    Write-Host "1. Running Instrumenter on Directory: $TargetSourceDir" -ForegroundColor Yellow
    dotnet run --project $instrumenterProject -- "$TargetSourceDir"
}
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed!"; exit 1 }

Write-Host "2. Running test driver to execute instrumented Checkout code..." -ForegroundColor Yellow
Write-Host "   JSON Log: $logFileJson"

# Set environment variable for the JSON log path so TraceLogger knows where to write.
$env:TRACE_LOG_JSON_PATH = $logFileJson

function Write-NewTraceEvents {
    param(
        [string]$JsonPath,
        [ref]$LastLineIndex
    )

    if (-not (Test-Path $JsonPath)) {
        return
    }

    $allLines = @(Get-Content -Path $JsonPath)
    if ($allLines.Count -le $LastLineIndex.Value) {
        return
    }

    for ($i = $LastLineIndex.Value; $i -lt $allLines.Count; $i++) {
        $rawLine = $allLines[$i]
        if ([string]::IsNullOrWhiteSpace($rawLine)) {
            continue
        }

        try {
            $evt = $rawLine | ConvertFrom-Json -ErrorAction Stop
            $traceText = "[TRACE] {0}:{1} | {2}" -f $evt.file, $evt.line, $evt.msg
            if ($null -ne $evt.var -and -not [string]::IsNullOrWhiteSpace($evt.var.name)) {
                $traceText += " | {0}={1}" -f $evt.var.name, $evt.var.val
            }
            Write-Host $traceText -ForegroundColor DarkCyan
        }
        catch {
            Write-Host "[TRACE-RAW] $rawLine" -ForegroundColor DarkCyan
        }
    }

    $LastLineIndex.Value = $allLines.Count
}

function Convert-ToCmdArg {
    param([string]$Arg)

    if ($Arg -match '[\s"]') {
        return '"' + ($Arg -replace '"', '\"') + '"'
    }

    return $Arg
}

function Invoke-DotnetWithStreaming {
    param(
        [string[]]$DotnetArgs,
        [string]$JsonPath,
        [string]$TestLogPath,
        [ref]$TraceLineIndex
    )

    $escapedArgs = $DotnetArgs | ForEach-Object { Convert-ToCmdArg $_ }
    $commandLine = "dotnet " + ($escapedArgs -join " ") + " 2>&1"

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "cmd.exe"
    $startInfo.Arguments = "/d /c $commandLine"
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $null = $process.Start()

    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line
        Write-Host $line
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    $process.WaitForExit()

    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line
        Write-Host $line
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    return $process.ExitCode
}

Write-Host "   Streaming dotnet test output + trace JSON events..." -ForegroundColor DarkGray
$traceLineIndex = 0
$testExitCode = 0

if ($StopOnFirstFailure) {
    Write-Host "   Stop-on-first-failure mode is ON (tests run one-by-one)." -ForegroundColor DarkGray

    $listArgs = @("test", "$DriverProject", "-f", "net10.0", "--list-tests", "--verbosity", "quiet")
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $listArgs += @("--filter", $TestFilter)
    }

    $listOutput = & dotnet @listArgs 2>&1
    foreach ($line in $listOutput) {
        Add-Content -Path $logFileTxt -Value $line
        Write-Host $line
    }

    $testNames = @(
        $listOutput |
        Where-Object { $_ -match '^\s{4,}\S' } |
        ForEach-Object { $_.Trim() }
    )

    if ($testNames.Count -eq 0) {
        Write-Error "No tests discovered for fail-fast run."
        exit 1
    }

    $buildArgs = @("build", "$DriverProject", "-f", "net10.0", "--verbosity", "quiet")
    $buildExit = Invoke-DotnetWithStreaming -DotnetArgs $buildArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
    if ($buildExit -ne 0) {
        $testExitCode = $buildExit
    }
    else {
        foreach ($testName in $testNames) {
            Write-Host "   Running test: $testName" -ForegroundColor DarkGray
            $singleRunArgs = @(
                "test", "$DriverProject",
                "-f", "net10.0",
                "--no-build",
                "--verbosity", "minimal",
                "--filter", "Name=$testName"
            )

            $singleExit = Invoke-DotnetWithStreaming -DotnetArgs $singleRunArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
            if ($singleExit -ne 0) {
                Write-Host "   First failing test detected. Terminating test run..." -ForegroundColor Yellow
                $testExitCode = $singleExit
                break
            }
        }
    }
}
else {
    $testArgs = @("test", "$DriverProject", "-f", "net10.0", "--verbosity", "minimal")
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArgs += @("--filter", $TestFilter)
    }

    $testExitCode = Invoke-DotnetWithStreaming -DotnetArgs $testArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
}

Write-NewTraceEvents -JsonPath $logFileJson -LastLineIndex ([ref]$traceLineIndex)
Write-Host "   Trace events streamed: $traceLineIndex" -ForegroundColor DarkGray

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

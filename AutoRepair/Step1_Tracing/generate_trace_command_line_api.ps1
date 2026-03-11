param(
    [string]$DriverProject = "",
    [string]$TestFilter = "",
    [switch]$SkipRestore,
    [bool]$StopOnFirstFailure = $true,
    [string]$LogFileTxtName = "trace_logs_command_line_api.txt",
    [string]$LogFileJsonName = "trace_logs_command_line_api.jsonl"
)

$ErrorActionPreference = "Stop"

$repoRoot            = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj"
$logFileTxt          = Join-Path $PSScriptRoot $LogFileTxtName
$logFileJson         = Join-Path $PSScriptRoot $LogFileJsonName
$restoreScript       = Join-Path $PSScriptRoot "restore_project.ps1"

# Instrument the full source codebase — both libraries, no test folders.
$sourceDirs = @(
    (Join-Path $repoRoot "command-line-api\src\System.CommandLine"),
    (Join-Path $repoRoot "command-line-api\src\System.CommandLine.Suggest")
)

if ([string]::IsNullOrWhiteSpace($DriverProject)) {
    $DriverProject = Join-Path $repoRoot "command-line-api\src\System.CommandLine.Suggest.Tests\dotnet-suggest.Tests.csproj"
}
$DriverProject = Resolve-Path $DriverProject

Write-Host "=== Step 1: Generating Trace Logs (command-line-api) ===" -ForegroundColor Cyan
Write-Host "Source dirs   : $($sourceDirs -join ', ')"
Write-Host "Driver project: $DriverProject"

# ── Restore previous instrumentation if any ──────────────────────────────────
if (-not $SkipRestore -and (Test-Path $restoreScript)) {
    Write-Host "0. Restoring instrumented files from backups..." -ForegroundColor Yellow
    foreach ($dir in $sourceDirs) {
        if (Test-Path $dir) { & $restoreScript $dir | Out-Null }
    }
}

# ── Clean / create log files ─────────────────────────────────────────────────
if (Test-Path $logFileJson) { Remove-Item $logFileJson }
if (Test-Path $logFileTxt)  { Remove-Item $logFileTxt  }
New-Item -ItemType File -Path $logFileJson -Force | Out-Null
New-Item -ItemType File -Path $logFileTxt  -Force | Out-Null

# ── Instrument each source directory ─────────────────────────────────────────
Write-Host "1. Running Instrumenter on full codebase..." -ForegroundColor Yellow
foreach ($dir in $sourceDirs) {
    Write-Host "   Instrumenting: $dir" -ForegroundColor DarkGray
    dotnet run --project $instrumenterProject -- "$dir"
    if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed on: $dir"; exit 1 }
}

# ── Helpers ──────────────────────────────────────────────────────────────────
Write-Host "2. Running tests against instrumented code..." -ForegroundColor Yellow
Write-Host "   JSON log: $logFileJson"

$env:TRACE_LOG_JSON_PATH = $logFileJson

function Write-NewTraceEvents {
    param([string]$JsonPath, [ref]$LastLineIndex)
    if (-not (Test-Path $JsonPath)) { return }
    $allLines = @(Get-Content -Path $JsonPath)
    if ($allLines.Count -le $LastLineIndex.Value) { return }
    for ($i = $LastLineIndex.Value; $i -lt $allLines.Count; $i++) {
        $raw = $allLines[$i]
        if ([string]::IsNullOrWhiteSpace($raw)) { continue }
        try {
            $evt = $raw | ConvertFrom-Json -ErrorAction Stop
            $txt = "[TRACE] {0}:{1} | {2}" -f $evt.file, $evt.line, $evt.msg
            if ($null -ne $evt.var -and -not [string]::IsNullOrWhiteSpace($evt.var.name)) {
                $txt += " | {0}={1}" -f $evt.var.name, $evt.var.val
            }
            Write-Host $txt -ForegroundColor DarkCyan
        }
        catch { Write-Host "[TRACE-RAW] $raw" -ForegroundColor DarkCyan }
    }
    $LastLineIndex.Value = $allLines.Count
}

function Convert-ToCmdArg {
    param([string]$Arg)
    if ($Arg -match '[\s"]') { return '"' + ($Arg -replace '"', '\"') + '"' }
    return $Arg
}

function Invoke-DotnetWithStreaming {
    param(
        [string[]]$DotnetArgs,
        [string]$JsonPath,
        [string]$TestLogPath,
        [ref]$TraceLineIndex
    )
    $escaped     = $DotnetArgs | ForEach-Object { Convert-ToCmdArg $_ }
    $commandLine = "dotnet " + ($escaped -join " ") + " 2>&1"

    $si = New-Object System.Diagnostics.ProcessStartInfo
    $si.FileName              = "cmd.exe"
    $si.Arguments             = "/d /c $commandLine"
    $si.UseShellExecute       = $false
    $si.RedirectStandardOutput = $true
    $si.CreateNoWindow        = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $si
    $null = $proc.Start()

    while ($null -ne ($line = $proc.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line
        Write-Host $line
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    $proc.WaitForExit()

    # Drain any remaining output
    while ($null -ne ($line = $proc.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line
        Write-Host $line
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    return $proc.ExitCode
}

# ── Run tests ────────────────────────────────────────────────────────────────
$traceLineIndex = 0
$testExitCode   = 0

if ($StopOnFirstFailure) {
    Write-Host "   Stop-on-first-failure mode ON (tests run one-by-one)." -ForegroundColor DarkGray

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
        Write-Error "No tests discovered."
        exit 1
    }

    # Build once, then run each test individually
    $buildArgs  = @("build", "$DriverProject", "-f", "net10.0", "--verbosity", "quiet")
    $buildExit  = Invoke-DotnetWithStreaming -DotnetArgs $buildArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
    if ($buildExit -ne 0) {
        $testExitCode = $buildExit
    }
    else {
        foreach ($testName in $testNames) {
            Write-Host "   Running: $testName" -ForegroundColor DarkGray

            # --list-tests returns fully qualified names like:
            #   Namespace.Class.MethodName(param: "value with spaces")
            # The MSBuild "Name=" filter breaks on special chars in Theory params.
            # Strategy: strip parameters -> keep last two segments (Class.Method) ->
            # use FullyQualifiedName~ (contains) so the filter is safe and specific.
            $stripped    = $testName -replace '\(.*\)$', ''           # drop (params...)
            $segments    = $stripped -split '\.'
            $filterValue = if ($segments.Count -ge 2) {
                               "$($segments[-2]).$($segments[-1])"    # Class.Method
                           } else { $stripped }

            $runArgs = @(
                "test", "$DriverProject",
                "-f", "net10.0",
                "--no-build",
                "--verbosity", "minimal",
                "--filter", "FullyQualifiedName~$filterValue"
            )
            $singleExit = Invoke-DotnetWithStreaming -DotnetArgs $runArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
            if ($singleExit -ne 0) {
                Write-Host "   First failing test detected. Stopping." -ForegroundColor Yellow
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

# ── Report ───────────────────────────────────────────────────────────────────
if ($testExitCode -ne 0) {
    Write-Host "=== Test driver failed ===" -ForegroundColor Red

    $failedTestLine = Select-String -Path $logFileTxt -Pattern '^\s*Failed\s+(.+?)\s+\[' | Select-Object -First 1
    if ($failedTestLine) {
        $failedTest = [regex]::Match($failedTestLine.Line, '^\s*Failed\s+(.+?)\s+\[').Groups[1].Value
        Write-Host "First failed test: $failedTest" -ForegroundColor Yellow
    }

    $failedLocationLine = Select-String -Path $logFileTxt -Pattern ' in .+?:line \d+' | Select-Object -First 1
    if ($failedLocationLine) {
        $m = [regex]::Match($failedLocationLine.Line, ' in (.+?):line (\d+)')
        if ($m.Success) {
            Write-Host "Failure location : $($m.Groups[1].Value):$($m.Groups[2].Value)" -ForegroundColor Yellow
        }
    }

    Write-Host "Trace logs:"
    Write-Host "  $logFileTxt"
    Write-Host "  $logFileJson"
    exit 1
}

Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Trace logs:"
Write-Host "  $logFileTxt"
Write-Host "  $logFileJson"

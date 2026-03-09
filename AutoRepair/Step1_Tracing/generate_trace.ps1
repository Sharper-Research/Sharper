# script parameters - these control how tracing and test execution works
param(
    [string]$TargetSourceDir = "",            # folder containing the source code to instrument
    [string]$DriverProject = "",              # path to the test .csproj that exercises the source
    [string]$TestFilter = "",                 # optional filter to run only specific tests
    [switch]$SkipRestore,                     # if set, skip restoring backups before instrumenting
    [bool]$StopOnFirstFailure = $true,        # if true, stop after first failing test (fail-fast mode)
    [string]$LogFileTxtName = "trace_logs.txt",   # filename for the human-readable log
    [string]$LogFileJsonName = "trace_logs.jsonl"  # filename for the machine-readable json log
)

$ErrorActionPreference = "Stop" # make the script stop on any error instead of continuing

# set up all the paths we need
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")                     # go up two levels to get the repo root
$instrumenterProject = Join-Path $PSScriptRoot "Instrumenter\Instrumenter.csproj" # the instrumenter c# project
$logFileTxt = Join-Path $PSScriptRoot $LogFileTxtName                          # full path for the text log
$logFileJson = Join-Path $PSScriptRoot $LogFileJsonName                        # full path for the json log
$restoreScript = Join-Path $PSScriptRoot "restore_project.ps1"                 # script to restore backups

# if no source dir was given, default to the BrightHrKata checkout project
if ([string]::IsNullOrWhiteSpace($TargetSourceDir)) {
    $TargetSourceDir = Join-Path $repoRoot "BrightHrKata\Checkout"
}
# if no test project was given, try the default one
if ([string]::IsNullOrWhiteSpace($DriverProject)) {
    # the gui should always pass this in, this fallback is just for standalone usage
    $fallback = Join-Path $repoRoot "BrightHrKata\Checkout.Tests\Checkout.Tests.csproj"
    if (Test-Path $fallback) {
        $DriverProject = $fallback
        Write-Host "NOTE: No -DriverProject specified; defaulting to $DriverProject" -ForegroundColor Yellow
    } else {
        Write-Error "No -DriverProject specified and no default test project found. Please provide a test project."
        exit 1 # cant do anything without a test project
    }
}

# resolve to full absolute paths so nothing gets confused later
$TargetSourceDir = Resolve-Path $TargetSourceDir
$DriverProject = Resolve-Path $DriverProject

# print out what were about to do
Write-Host "=== Step 1: Generating Trace Logs (Checkout) ===" -ForegroundColor Cyan
Write-Host "Target source: $TargetSourceDir"
Write-Host "Driver project: $DriverProject"

# restore any instrumented files from previous runs (unless told to skip)
if (-not $SkipRestore -and (Test-Path $restoreScript)) {
    Write-Host "0. Restoring instrumented files from backups..." -ForegroundColor Yellow
    & $restoreScript $TargetSourceDir | Out-Null # run the restore script silently
}

# delete old log files so we start fresh
if (Test-Path $logFileJson) { Remove-Item $logFileJson }
if (Test-Path $logFileTxt) { Remove-Item $logFileTxt }

# create empty log files even if tests fail early, so other scripts dont crash looking for them
New-Item -ItemType File -Path $logFileJson -Force | Out-Null
New-Item -ItemType File -Path $logFileTxt -Force | Out-Null

# run the instrumenter to inject trace logging into the source files
Write-Host "1. Running Instrumenter on Directory: $TargetSourceDir" -ForegroundColor Yellow
dotnet run --project $instrumenterProject -- "$TargetSourceDir"
if ($LASTEXITCODE -ne 0) { Write-Error "Instrumenter failed!"; exit 1 } # bail if it failed

# run the test project which will execute the now-instrumented source code
Write-Host "2. Running test driver to execute instrumented Checkout code..." -ForegroundColor Yellow
Write-Host "   JSON Log: $logFileJson"

# tell TraceLogger where to write the json events via environment variable
$env:TRACE_LOG_JSON_PATH = $logFileJson

# reads new trace events from the json log file and prints them to the console
# keeps track of where it left off so it only prints new events each time
function Write-NewTraceEvents {
    param(
        [string]$JsonPath,       # path to the jsonl trace file
        [ref]$LastLineIndex      # reference to the line counter so we can update it
    )

    if (-not (Test-Path $JsonPath)) { # file doesnt exist yet, nothing to read
        return
    }

    $allLines = @(Get-Content -Path $JsonPath)          # read all lines from the file
    if ($allLines.Count -le $LastLineIndex.Value) {     # no new lines since last check
        return
    }

    # loop through only the new lines we havent seen yet
    for ($i = $LastLineIndex.Value; $i -lt $allLines.Count; $i++) {
        $rawLine = $allLines[$i]
        if ([string]::IsNullOrWhiteSpace($rawLine)) { # skip blank lines
            continue
        }

        try {
            $evt = $rawLine | ConvertFrom-Json -ErrorAction Stop # parse the json event
            # format the trace event nicely: file:line | message
            $traceText = "[TRACE] {0}:{1} | {2}" -f $evt.file, $evt.line, $evt.msg
            # if theres a variable snapshot, append it too
            if ($null -ne $evt.var -and -not [string]::IsNullOrWhiteSpace($evt.var.name)) {
                $traceText += " | {0}={1}" -f $evt.var.name, $evt.var.val
            }
            Write-Host $traceText -ForegroundColor DarkCyan # print in dark cyan so its distinct
        }
        catch {
            Write-Host "[TRACE-RAW] $rawLine" -ForegroundColor DarkCyan # cant parse it, just print raw
        }
    }

    $LastLineIndex.Value = $allLines.Count # update the counter to where we left off
}

# escapes a command line argument if it contains spaces or quotes
# needed for passing arguments to cmd.exe safely
function Convert-ToCmdArg {
    param([string]$Arg)

    if ($Arg -match '[\s"]') { # check if it has whitespace or double quotes
        return '"' + ($Arg -replace '"', '\"') + '"' # wrap in quotes and escape inner quotes
    }

    return $Arg # no escaping needed
}

# runs dotnet with the given arguments and streams output in real time
# also polls for new trace events while the process runs
function Invoke-DotnetWithStreaming {
    param(
        [string[]]$DotnetArgs,    # the arguments to pass to dotnet
        [string]$JsonPath,        # path to the trace json file (for polling new events)
        [string]$TestLogPath,     # path to write a copy of the test output
        [ref]$TraceLineIndex      # reference to the trace event line counter
    )

    # escape all the args and build the full command line
    $escapedArgs = $DotnetArgs | ForEach-Object { Convert-ToCmdArg $_ }
    $commandLine = "dotnet " + ($escapedArgs -join " ") + " 2>&1" # redirect stderr to stdout

    # set up a new process to run the command through cmd.exe
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "cmd.exe"                    # run through cmd
    $startInfo.Arguments = "/d /c $commandLine"        # /d disables autorun, /c runs the command
    $startInfo.UseShellExecute = $false                # need this false to redirect output
    $startInfo.RedirectStandardOutput = $true          # capture stdout
    $startInfo.CreateNoWindow = $true                  # dont pop up a console window

    $process = New-Object System.Diagnostics.Process   # create the process object
    $process.StartInfo = $startInfo                    # assign the start info
    $null = $process.Start()                           # start it

    # read stdout line by line as it comes in
    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line    # save to test log file
        Write-Host $line                               # print to console
        # check for any new trace events that were written while we were reading
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    $process.WaitForExit() # wait for the process to fully finish

    # read any remaining output after the process exits (there might be buffered lines)
    while ($null -ne ($line = $process.StandardOutput.ReadLine())) {
        Add-Content -Path $TestLogPath -Value $line
        Write-Host $line
        Write-NewTraceEvents -JsonPath $JsonPath -LastLineIndex $TraceLineIndex
    }

    return $process.ExitCode # return the exit code so caller knows if it passed
}

Write-Host "   Streaming dotnet test output + trace JSON events..." -ForegroundColor DarkGray
$traceLineIndex = 0   # counter for how many trace lines weve read so far
$testExitCode = 0     # will be set to non-zero if any test fails

if ($StopOnFirstFailure) {
    # fail-fast mode: run tests one at a time and stop at the first failure
    Write-Host "   Stop-on-first-failure mode is ON (tests run one-by-one)." -ForegroundColor DarkGray

    # first we need to list all the available tests
    $listArgs = @("test", "$DriverProject", "-f", "net10.0", "--list-tests", "--verbosity", "quiet")
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { # add filter if one was provided
        $listArgs += @("--filter", $TestFilter)
    }

    $listOutput = & dotnet @listArgs 2>&1 # run dotnet test --list-tests to get test names
    foreach ($line in $listOutput) {      # log the list output
        Add-Content -Path $logFileTxt -Value $line
        Write-Host $line
    }

    # parse out the test names from the output
    # test names are indented with 4+ spaces in the dotnet output
    $testNames = @(
        $listOutput |
        Where-Object { $_ -match '^\s{4,}\S' } | # lines with 4+ leading spaces
        ForEach-Object { $_.Trim() }              # trim the whitespace
    )

    if ($testNames.Count -eq 0) { # no tests found, something is wrong
        Write-Error "No tests discovered for fail-fast run."
        exit 1
    }

    # build the project first so we dont rebuild for every individual test
    $buildArgs = @("build", "$DriverProject", "-f", "net10.0", "--verbosity", "quiet")
    $buildExit = Invoke-DotnetWithStreaming -DotnetArgs $buildArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
    if ($buildExit -ne 0) { # build failed
        $testExitCode = $buildExit
    }
    else {
        # now run each test individually
        foreach ($testName in $testNames) {
            Write-Host "   Running test: $testName" -ForegroundColor DarkGray
            # run just this one test with --no-build since we already built above
            $singleRunArgs = @(
                "test", "$DriverProject",
                "-f", "net10.0",
                "--no-build",                     # skip building, already done
                "--verbosity", "minimal",
                "--filter", "Name=$testName"       # run only this specific test
            )

            $singleExit = Invoke-DotnetWithStreaming -DotnetArgs $singleRunArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
            if ($singleExit -ne 0) { # this test failed, stop running more tests
                Write-Host "   First failing test detected. Terminating test run..." -ForegroundColor Yellow
                $testExitCode = $singleExit
                break # exit the loop
            }
        }
    }
}
else {
    # normal mode: run all tests at once
    $testArgs = @("test", "$DriverProject", "-f", "net10.0", "--verbosity", "minimal")
    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { # add filter if provided
        $testArgs += @("--filter", $TestFilter)
    }

    # run all tests in one go
    $testExitCode = Invoke-DotnetWithStreaming -DotnetArgs $testArgs -JsonPath $logFileJson -TestLogPath $logFileTxt -TraceLineIndex ([ref]$traceLineIndex)
}

# flush any remaining trace events that havent been printed yet
Write-NewTraceEvents -JsonPath $logFileJson -LastLineIndex ([ref]$traceLineIndex)
Write-Host "   Trace events streamed: $traceLineIndex" -ForegroundColor DarkGray

if ($testExitCode -ne 0) {
    # tests failed, which is expected (thats why were running the auto repair tool)
    Write-Host "=== Test driver failed ===" -ForegroundColor Red

    # try to find the name of the first failed test from the log
    $failedTest = $null
    $failedTestLine = Select-String -Path $logFileTxt -Pattern '^\s*Failed\s+(.+?)\s+\[' | Select-Object -First 1
    if ($failedTestLine) {
        $failedTest = [regex]::Match($failedTestLine.Line, '^\s*Failed\s+(.+?)\s+\[').Groups[1].Value
    }

    # try to find the file and line number where the failure happened
    $failedLocation = $null
    $failedLocationLine = Select-String -Path $logFileTxt -Pattern ' in .+?:line \d+' | Select-Object -First 1
    if ($failedLocationLine) {
        $failedLocationMatch = [regex]::Match($failedLocationLine.Line, ' in (.+?):line (\d+)')
        if ($failedLocationMatch.Success) {
            $failedLocation = "$($failedLocationMatch.Groups[1].Value):$($failedLocationMatch.Groups[2].Value)"
        }
    }

    # print the failure info if we found it
    if (-not [string]::IsNullOrWhiteSpace($failedTest)) {
        Write-Host "First failed test: $failedTest" -ForegroundColor Yellow
    }
    if (-not [string]::IsNullOrWhiteSpace($failedLocation)) {
        Write-Host "Failure location: $failedLocation" -ForegroundColor Yellow
    }

    # make sure the json log file exists even if no events were captured
    if (-not (Test-Path $logFileJson)) {
        New-Item -ItemType File -Path $logFileJson -Force | Out-Null
    }

    Write-Host "Trace logs available:" -ForegroundColor Yellow
    Write-Host "  $logFileTxt"
    Write-Host "  $logFileJson"
    exit 1 # exit with error code so the caller knows tests failed
}

# if we get here, all tests passed
Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Trace logs available:"
Write-Host "  $logFileTxt"
Write-Host "  $logFileJson"

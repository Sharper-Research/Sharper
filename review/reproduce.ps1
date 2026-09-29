param([string]$Output = "review/rerun")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$elapsed = [Diagnostics.Stopwatch]::StartNew()
Push-Location $root
try {
    if (![IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }
    $Output = [IO.Path]::GetFullPath($Output)
    if (Test-Path $Output) { throw "Use a fresh output directory: $Output" }
    New-Item -ItemType Directory -Path $Output | Out-Null
    $analyzer = "Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj"
    $tests = "Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/CFGAnalyzer.Tests.csproj"
    $fixture = "review/fixtures/bounds"
    $originals = @("$fixture/Fixture.cs", "review/fixtures/runtime/RuntimeCode/MemberMethodResolver.cs", "review/fixtures/listing/Listing.cs")
    $hashes = @{}
    foreach ($source in $originals) { $hashes[$source] = (Get-FileHash $source).Hash }
    function Invoke-Dotnet([string[]]$Arguments, [int]$Expected = 0) {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne $Expected) { throw "dotnet exited $LASTEXITCODE; expected $Expected" }
    }
    Invoke-Dotnet -Arguments @("build", "Sharper.sln", "--verbosity", "minimal")
    Invoke-Dotnet -Arguments @("test", $tests, "--logger", "trx;LogFileName=analyzer-suite.trx", "--results-directory", $Output, "--verbosity", "minimal")
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", $analyzer, "--", "--verify-model", "review/models/loop-and-call.json", "--output", "$Output/loop-and-call.json")
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", $analyzer, "--", "--check-activation", "review/models/activation.json", "--output", "$Output/activation.json")
    $runtime = (Resolve-Path "review/fixtures/runtime").Path
    $sandbox = Join-Path ([IO.Path]::GetTempPath()) ("Sharper-trace-review-" + [guid]::NewGuid().ToString("N"))
    $traceDirectory = Join-Path $Output "runtime-trace"
    New-Item -ItemType Directory -Path $traceDirectory | Out-Null
    $previousTracePath = $env:TRACE_LOG_JSON_PATH
    try {
        foreach ($file in (Get-ChildItem -LiteralPath $runtime -File -Recurse | Where-Object {
            $_.Extension -in @(".cs", ".csproj") -and $_.FullName -notmatch '[\\/](bin|obj|TestResults)[\\/]'
        })) {
            $destination = Join-Path $sandbox $file.FullName.Substring($runtime.Length + 1)
            New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination
        }
        $env:TRACE_LOG_JSON_PATH = Join-Path $traceDirectory "trace_logs.jsonl"
        Invoke-Dotnet -Arguments @("run", "--no-build", "--project", "Sharper/Step1_Tracing/Instrumenter/Instrumenter.csproj", "--", "$sandbox/RuntimeCode")
        Invoke-Dotnet -Expected 1 -Arguments @("test", "$sandbox/RuntimeCode.Tests/RuntimeCode.Tests.csproj", "--logger", "trx;LogFileName=instrumented.trx", "--results-directory", $traceDirectory, "--verbosity", "minimal")
        if (!(Test-Path $env:TRACE_LOG_JSON_PATH)) { throw "Instrumented execution produced no trace" }
    } finally {
        $env:TRACE_LOG_JSON_PATH = $previousTracePath
        if (Test-Path $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
    }
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", "Sharper/Step2_Constraints/Constraints/Constraints.csproj", "--", "--trace", "$traceDirectory/trace_logs.jsonl", "--report", "$traceDirectory/violations.json")
    [xml]$instrumented = Get-Content "$traceDirectory/instrumented.trx" -Raw
    $instrumentedCounters = $instrumented.SelectSingleNode("//*[local-name()='Counters']")
    if ([int]$instrumentedCounters.total -ne 5 -or [int]$instrumentedCounters.passed -ne 3 -or [int]$instrumentedCounters.failed -ne 2) {
        throw "Instrumented fixture changed its expected test outcomes"
    }
    $detected = Get-Content "$traceDirectory/violations.json" -Raw | ConvertFrom-Json
    if ($detected.Count -ne 2 -or @($detected | Where-Object { $_.file -ne "MemberMethodResolver.cs" -or $_.line -ne 13 -or $_.predicate -notlike "NotNull*" }).Count) {
        throw "Unexpected runtime detection results"
    }
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", $analyzer, "--", "$fixture/Fixture.csproj", "--violations", "$fixture/violations.json", "--output-dir", "$Output/acceptance", "--validation-root", $fixture, "--test-project", "$fixture/Fixture.Tests/Fixture.Tests.csproj", "--reproducer-filter", "FullyQualifiedName~InvalidReadsReturnDefault")
    Invoke-Dotnet -Expected 5 -Arguments @("run", "--no-build", "--project", $analyzer, "--", "review/fixtures/runtime/RuntimeCode/RuntimeCode.csproj", "--violations", "$traceDirectory/violations.json", "--output-dir", "$Output/runtime-rejection", "--validation-root", "review/fixtures/runtime", "--test-project", "review/fixtures/runtime/RuntimeCode.Tests/RuntimeCode.Tests.csproj", "--reproducer-filter", "FullyQualifiedName~MemberMethodResolverTests")
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", $analyzer, "--", "review/fixtures/listing/Listing.csproj", "--violations", "review/fixtures/listing/violations.json", "--output-dir", "$Output/listing", "--validation-root", "review/fixtures/listing", "--test-project", "review/fixtures/listing/Listing.Tests/Listing.Tests.csproj", "--reproducer-filter", "FullyQualifiedName~PaperAssumptionReturnsNormally")
    $listing = Get-Content "$Output/listing/repair_plan.json" -Raw | ConvertFrom-Json
    if ($listing.Items.Count -ne 1 -or $listing.Items[0].CandidateModelCheck.Status -ne "VerifiedExploredModel" -or $listing.Items[0].Validation.Status -ne "AcceptedByTests") {
        throw "Listing candidate did not pass model and test validation"
    }
    if (($listing.Items[0].CandidateRefinementChecks.Status -join ',') -ne 'Counterexample,Counterexample,VerifiedExploredModel') {
        throw "Listing refinement did not expose both unsafe stages"
    }
    foreach ($source in $originals) {
        if ((Get-FileHash $source).Hash -ne $hashes[$source]) { throw "Original source changed: $source" }
    }
    $positive = Get-Content "$Output/acceptance/repair_plan.json" -Raw | ConvertFrom-Json
    $negative = Get-Content "$Output/runtime-rejection/repair_plan.json" -Raw | ConvertFrom-Json
    if ($positive.Items[0].Validation.Status -ne "AcceptedByTests") { throw "Positive gate did not accept" }
    if (@($negative.Items | Where-Object { $_.Validation.Status -ne "Rejected" }).Count) { throw "Negative gate did not reject" }
    $hashes | ConvertTo-Json | Set-Content "$Output/original-source-sha256.json" -Encoding UTF8
    $elapsed.Stop()
    $sdk = (& dotnet --version | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Cannot record .NET SDK version" }
    [ordered]@{
        captured_utc = [DateTime]::UtcNow.ToString("o")
        os = [Environment]::OSVersion.VersionString
        architecture = $env:PROCESSOR_ARCHITECTURE
        dotnet_sdk = $sdk
        powershell = $PSVersionTable.PSVersion.ToString()
        elapsed_seconds = [Math]::Round($elapsed.Elapsed.TotalSeconds, 3)
        timing_scope = "Single local reproduction including build, tests, tracing, solver checks and acceptance gates; not a benchmark"
    } | ConvertTo-Json | Set-Content "$Output/environment.json" -Encoding UTF8
    Write-Output "Review checks passed; originals unchanged. Evidence: $Output"
    $global:LASTEXITCODE = 0
} finally { Pop-Location }
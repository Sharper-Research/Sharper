param([string]$Output = "review/rerun")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    if (![IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }
    $Output = [IO.Path]::GetFullPath($Output)
    if (Test-Path $Output) { throw "Use a fresh output directory: $Output" }
    New-Item -ItemType Directory -Path $Output | Out-Null
    $analyzer = "Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj"
    $tests = "Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/CFGAnalyzer.Tests.csproj"
    $fixture = "review/fixtures/bounds"
    $originals = @("$fixture/Fixture.cs", "review/fixtures/runtime/RuntimeCode/MemberMethodResolver.cs")
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
    Invoke-Dotnet -Arguments @("run", "--no-build", "--project", $analyzer, "--", "$fixture/Fixture.csproj", "--violations", "$fixture/violations.json", "--output-dir", "$Output/acceptance", "--validation-root", $fixture, "--test-project", "$fixture/Fixture.Tests/Fixture.Tests.csproj", "--reproducer-filter", "FullyQualifiedName~InvalidReadsReturnDefault")
    Invoke-Dotnet -Expected 5 -Arguments @("run", "--no-build", "--project", $analyzer, "--", "review/fixtures/runtime/RuntimeCode/RuntimeCode.csproj", "--violations", "review/fixtures/runtime/evidence/violations.json", "--output-dir", "$Output/runtime-rejection", "--validation-root", "review/fixtures/runtime", "--test-project", "review/fixtures/runtime/RuntimeCode.Tests/RuntimeCode.Tests.csproj", "--reproducer-filter", "FullyQualifiedName~MemberMethodResolverTests")
    foreach ($source in $originals) {
        if ((Get-FileHash $source).Hash -ne $hashes[$source]) { throw "Original source changed: $source" }
    }
    $positive = Get-Content "$Output/acceptance/repair_plan.json" -Raw | ConvertFrom-Json
    $negative = Get-Content "$Output/runtime-rejection/repair_plan.json" -Raw | ConvertFrom-Json
    if ($positive.Items[0].Validation.Status -ne "AcceptedByTests") { throw "Positive gate did not accept" }
    if (@($negative.Items | Where-Object { $_.Validation.Status -ne "Rejected" }).Count) { throw "Negative gate did not reject" }
    $hashes | ConvertTo-Json | Set-Content "$Output/original-source-sha256.json" -Encoding UTF8
    Write-Output "Review checks passed; originals unchanged. Evidence: $Output"
    $global:LASTEXITCODE = 0
} finally { Pop-Location }
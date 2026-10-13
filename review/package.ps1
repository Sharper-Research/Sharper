param(
    [string]$Output = "review/Sharper-tool-artifact.zip",
    [string]$Evidence = "review/verified-run"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
$staging = Join-Path ([IO.Path]::GetTempPath()) ("Sharper-artifact-" + [guid]::NewGuid().ToString("N"))
try {
    if (![IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }
    $Output = [IO.Path]::GetFullPath($Output)
    if (Test-Path $Output) { throw "Refusing to replace an existing package: $Output" }
    $evidencePath = (Resolve-Path -LiteralPath $Evidence).Path
    if (!$evidencePath.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Evidence must be within the repository"
    }
    [xml]$trx = Get-Content (Join-Path $evidencePath "analyzer-suite.trx") -Raw
    $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
    if (!$counters -or [int]$counters.failed -ne 0 -or [int]$counters.passed -le 0) { throw "Missing passing analyzer evidence" }
    $positive = Get-Content (Join-Path $evidencePath "acceptance/repair_plan.json") -Raw | ConvertFrom-Json
    $negative = Get-Content (Join-Path $evidencePath "runtime-rejection/repair_plan.json") -Raw | ConvertFrom-Json
    if ($positive.Items[0].Validation.Status -ne "AcceptedByTests") { throw "Missing accepted synthetic case" }
    if (@($negative.Items | Where-Object { $_.Validation.Status -ne "Rejected" }).Count) { throw "Missing rejected runtime case" }
    $listing = Get-Content (Join-Path $evidencePath "listing/repair_plan.json") -Raw | ConvertFrom-Json
    if ($listing.Items.Count -ne 1 -or $listing.Items[0].Validation.Status -ne "AcceptedByTests" -or $listing.Items[0].CandidateModelCheck.Status -ne "VerifiedExploredModel") {
        throw "Missing model-checked and test-accepted listing candidate"
    }
    if ((Get-FileHash (Join-Path $evidencePath "listing/patches/Listing.L9.patched.cs")).Hash -ne $listing.Items[0].CandidateModelSha256) {
        throw "Listing candidate bytes no longer match the model check"
    }
    $directories = @(
        "Sharper/Step1_Tracing", "Sharper/Step2_Constraints", "Sharper/Step3_Repair/CFGAnalyzer",
        "review/fixtures", "review/models", $Evidence
    )
    if (Test-Path "review/jsonconverter-run") { $directories += "review/jsonconverter-run" }
    $files = @($directories | ForEach-Object { Get-ChildItem -LiteralPath $_ -File -Recurse })
    $files += @(Get-Item "README.md", "Sharper.sln", ".gitignore", ".gitattributes", "Sharper/run_all.ps1",
        "review/README.md", "review/reproduce.ps1", "review/package.ps1", "review/verify-jsonconverter.ps1")
    New-Item -ItemType Directory -Path $staging | Out-Null
    $hashes = [ordered]@{}
    foreach ($file in ($files | Sort-Object FullName -Unique)) {
        $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
        if ($relative -match '(^|/)(bin|obj|\.git|\.idea|\.vs|reports|TestResults|__pycache__)/' -or
            $relative.StartsWith("review/fixtures/bounds/results/") -or
            $file.Name -in @("AGENTS.md", ".env") -or $file.Name -like "*.bak" -or
            $file.Name -like '~$*' -or $file.Extension -in @(".zip", ".pdb", ".dll")) { continue }
        $destination = Join-Path $staging $relative
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        $hashes[$relative] = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    }
    $branch = "export"
    $commit = $null
    $dirty = $true
    if (Test-Path .git) {
        $branch = (& git branch --show-current | Out-String).Trim()
        $commit = (& git rev-parse HEAD | Out-String).Trim()
        $dirty = [bool]((& git status --porcelain | Out-String).Trim())
    }
    [ordered]@{
        created_utc = [DateTime]::UtcNow.ToString("o")
        branch = $branch
        base_commit = $commit
        includes_uncommitted_working_tree = $dirty
        analyzer_tests_passed = [int]$counters.passed
        evidence = $Evidence
        files = $hashes
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $staging "ARTIFACT_MANIFEST.json") -Encoding UTF8
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Path (Split-Path $Output -Parent) -Force | Out-Null
    [IO.Compression.ZipFile]::CreateFromDirectory($staging, $Output)
    Write-Output "Created $Output ($($hashes.Count) files plus SHA-256 manifest)."
} finally {
    if (Test-Path $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    Pop-Location
}

# PRHunter auto-commit watcher.
# Polls every 60s for the python.exe running exception_hunter.py.
# When it exits, commits any changed output files and pushes.
# Self-deletes its lock file when done.

$ErrorActionPreference = 'Continue'
$repo = 'C:\Users\manis\Desktop\Patching'
$lock = Join-Path $repo 'PRHunter\output\exception_hunter\.watcher.lock'
$watcherLog = Join-Path $repo 'PRHunter\output\exception_hunter\watcher.log'

function Log($m) { "$([DateTime]::Now.ToString('s'))  $m" | Out-File -FilePath $watcherLog -Append -Encoding utf8 }

Set-Content -Path $lock -Value $PID -Force
Log "watcher started pid=$PID"

# Wait until no python.exe is running exception_hunter.py
while ($true) {
    $procs = Get-CimInstance Win32_Process -Filter "Name='python.exe'" -ErrorAction SilentlyContinue |
             Where-Object { $_.CommandLine -and $_.CommandLine -match 'exception_hunter\.py' }
    if (-not $procs) {
        Log "python no longer running, proceeding to commit"
        break
    }
    Start-Sleep -Seconds 60
}

# Give the script a few seconds to flush final writes
Start-Sleep -Seconds 5

Set-Location $repo
$out = 'PRHunter/output/exception_hunter'
git add -f "$out/rejected_prs.csv" "$out/reverted_prs.csv" "$out/open_issues.csv" `
    "$out/done_keywords.txt" "$out/resolved_prs.txt" "$out/run.log" "$out/live_run.log" 2>&1 | Out-File -FilePath $watcherLog -Append

# Count rows for the commit message
function CountLines($p) {
    if (Test-Path $p) { (Get-Content $p | Measure-Object -Line).Lines - 1 } else { 0 }
}
$nRej  = CountLines "$out/rejected_prs.csv"
$nRev  = CountLines "$out/reverted_prs.csv"
$nIss  = CountLines "$out/open_issues.csv"

$msg = "PRHunter: final overnight run complete - $nRej rejected, $nRev reverted, $nIss open issues"
Log "committing: $msg"

git commit -m $msg 2>&1 | Out-File -FilePath $watcherLog -Append
git push origin Dimitrios_Manish 2>&1 | Out-File -FilePath $watcherLog -Append

Log "done"
Remove-Item $lock -Force -ErrorAction SilentlyContinue

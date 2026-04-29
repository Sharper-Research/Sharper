# PRHunter

Two scripts that mine GitHub for C# bug-fix candidates feeding the AutoRepair
pipeline. Output lands in `PRHunter/output/` (gitignored).

| Script | What it does |
|---|---|
| `pr_hunter.py` | Per-repo: reverted + rejected PRs on a small fixed list of dotnet repos. |
| `exception_hunter.py` | Global: PRs mentioning one of 35 exception types, then their linked **open** issues. |

## Run

```powershell
$env:GITHUB_TOKEN = "ghp_xxx"          # classic token, public_repo scope
pip install -r PRHunter\requirements.txt
python PRHunter\pr_hunter.py           # or:
python PRHunter\exception_hunter.py    # resumable, Ctrl-C safe
```

## Definitions

- **Rejected**: PR closed without merge (`state=closed`, `merged_at=null`).
- **Reverted**: PR was merged, then a later PR with `Revert "... (#N)"` or
  body `This reverts commit <sha>` undid it.
- **Linked issue**: only counted when preceded by `closes`/`fixes`/`resolves`.

## Output

`PRHunter/output/exception_hunter/`:
- `rejected_prs.csv`, `reverted_prs.csv` — candidate PRs with evidence.
- `open_issues.csv` — open issues those PRs claimed to fix (the main answer).
- `done_keywords.txt`, `resolved_prs.txt` — resume checkpoints.

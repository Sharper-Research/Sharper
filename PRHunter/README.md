# PRHunter

Mines GitHub for failed C# bug-fix attempts and feeds them through the
AutoRepair pipeline end-to-end. Output lands in `PRHunter/output/` and
`PRHunter/e2e/` (gitignored except for what we explicitly commit).

## Pipeline

```
exception_hunter.py                        e2e_demo.py
  35 .NET exception keywords                  open_issues.csv
       │                                          │
       ▼                                          ▼
  Phase 1: GitHub search                    rank candidates
   - rejected PRs (closed unmerged)          (small repos, has Tests.csproj)
   - reverted PRs (merged then undone)             │
       │                                          ▼
       ▼                                    per case:
  Phase 2: resolve each PR                   - clone @ buggy base SHA
   - fetch body + comments                   - detect test project
   - keep links to OPEN issues               - run AutoRepair (Step1+Step2)
       │                                     - parse violations.json
       ▼                                          │
  open_issues.csv  ──────────────────────────────►▼
  (candidate bug pool)                       e2e/REPORT.md
                                             "AutoRepair caught it,
                                              SonarQube would not"
```

## Run

```powershell
$env:GITHUB_TOKEN = "ghp_xxx"          # classic token, public_repo scope
pip install -r PRHunter\requirements.txt

# (one-time, takes hours, resumable) mine the candidate pool
python PRHunter\exception_hunter.py

# rank candidates, then pick which to demo
python PRHunter\e2e_demo.py --rank
python PRHunter\e2e_demo.py --case 0
python PRHunter\e2e_demo.py --all --max 3
```

## Definitions

- **Rejected PR**: closed without merge (`state=closed`, `merged_at=null`).
- **Reverted PR**: merged, then a later PR with `Revert "... (#N)"` undid it.
- **Linked issue**: only counted when preceded by `closes`/`fixes`/`resolves`.

## Status

PRHunter is auxiliary tooling for **external validation** of AutoRepair, not
part of the core repair loop. The mining script and the e2e orchestrator
both work. The Instrumenter is now compatible with net6/net8/net10 repos.

| Case | Repo | Exception | Result |
|---|---|---|---|
| 01 | nodirmcsd/WordNinjaSharp#3 | ArgumentOutOfRangeException | **Patch generated** - `WordNinja.L172.diff` |
| 02 | AngleSharp/AngleSharp.Css#183 | InvalidOperationException | 934 ValidCast violations detected; patch pending |

See `PRHunter/e2e/cases/` for per-case output (log, violations.json, patches/, report.md).

## Output

`PRHunter/output/exception_hunter/`:
- `rejected_prs.csv`, `reverted_prs.csv`: candidate PRs.
- `open_issues.csv`: open issues those failed PRs claimed to fix.
- `done_keywords.txt`, `resolved_prs.txt`, `run.log`: resume + audit.

`PRHunter/e2e/`:
- `candidates.json`: ranked demo candidates.
- `cases/<n>_<owner>_<repo>_issue<N>/`: per-case workspace
  (manifest, cloned repo, AutoRepair log, `violations.json`, `report.md`).
- `REPORT.md`: index across all cases.

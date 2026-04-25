# PRHunter — Reverted & Rejected PR Search (Manish's half)

Python tool that searches GitHub for **reverted** and **rejected** pull requests
linked to issues on selected C# repositories. The output is a CSV that
Dimitrios's findings can be merged with later.

## What "reverted" and "rejected" mean here

| Category | Definition (GitHub) |
|---|---|
| **Reverted** | A PR was merged, and a later commit/PR with title starting with `Revert ` (or body containing `This reverts commit <sha>`) undid it. |
| **Rejected** | A PR was closed **without being merged** (`state=closed`, `merged_at=null`). |

For each match we also follow back to the **issue(s) it claimed to close**
(via `Closes #N`, `Fixes #N`, `Resolves #N` in the PR body) so the merge with
Dimitrios's data is straightforward.

## Default scope

These C#-heavy repos are hit by default (override with `--repo`):

- `dotnet/runtime`
- `dotnet/roslyn`
- `dotnet/sdk`
- `dotnet/csharplang`
- `dotnet/aspnetcore`
- `dotnet/efcore`

## Quick start

```powershell
# 1. Install deps
pip install -r PRHunter\requirements.txt

# 2. (Recommended) set a GitHub token to lift the 60 req/h anonymous limit
$env:GITHUB_TOKEN = "ghp_xxxxxxxxxxxxxxxxxxxx"

# 3. Run with defaults (smoke test: 50 reverts + 50 rejections per repo)
python PRHunter\pr_hunter.py

# 4. Or scope to a single repo with a bigger budget
python PRHunter\pr_hunter.py --repo dotnet/roslyn --max 200
```

## Output

Two CSVs in `PRHunter/output/`:

- `reverted_prs.csv` — merged-then-reverted PRs
- `rejected_prs.csv` — closed-without-merge PRs

Both share the columns:

```
repo, pr_number, pr_title, pr_url, pr_state, merged_at, closed_at,
author, linked_issues, category, evidence
```

`linked_issues` is a `;`-separated list of issue numbers extracted from the PR
body; `evidence` cites the revert commit / closing reason.

## Next steps (project plan)

1. **Now (this script):** collect Manish's half of the corpus.
2. Dimitrios runs his equivalent search, exports the same CSV schema.
3. Merge both CSVs into a single deduped corpus.
4. Apply the AutoRepair tool to the linked issues, manually verify, open draft PRs.
</content>
</invoke>
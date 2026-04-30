# PRHunter end-to-end demo report

Each row is a real open issue mined from GitHub whose fix attempt was
either rejected (closed unmerged) or reverted (merged then undone).
AutoRepair was run against the buggy base SHA of each failed PR.

| # | Repo / Issue | Exception | Status | Report |
|---|---|---|---|---|
| 0 | [tghamm/Anthropic.SDK#197](https://github.com/tghamm/Anthropic.SDK/issues/197) | MissingMethodException | ok | [case](cases/00_tghamm_Anthropic.SDK_issue197/report.md) |

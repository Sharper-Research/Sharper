# PRHunter end-to-end demo report

Each row is a real open issue mined from GitHub whose fix attempt was
either rejected (closed unmerged) or reverted (merged then undone).
AutoRepair was run against the buggy base SHA of each failed PR.

| # | Repo / Issue | Exception | Status | Report |
|---|---|---|---|---|
| 0 | [nodirmcsd/WordNinjaSharp#3](https://github.com/nodirmcsd/WordNinjaSharp/issues/3) | ArgumentOutOfRangeException | ok | [case](cases/01_nodirmcsd_WordNinjaSharp_issue3/report.md) |

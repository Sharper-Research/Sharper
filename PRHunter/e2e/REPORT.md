# PRHunter end-to-end demo report

Each row is a real open issue mined from GitHub whose fix attempt was
either rejected (closed unmerged) or reverted (merged then undone).
AutoRepair was run against the buggy base SHA of each failed PR.

| # | Repo / Issue | Exception | Status | Report |
|---|---|---|---|---|
| 0 | [AngleSharp/AngleSharp.Css#183](https://github.com/AngleSharp/AngleSharp.Css/issues/183) | InvalidOperationException | ok | [case](cases/02_AngleSharp_AngleSharp.Css_issue183/report.md) |

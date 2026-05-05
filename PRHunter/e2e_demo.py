"""
PRHunter end-to-end demo orchestrator.

Pipeline (per candidate open issue):

  open_issues.csv row
        |
        v
  GitHub API: PR metadata
   - base SHA (buggy state, before the failed fix)
   - changed-files list
        |
        v
  shallow git clone @ base SHA  ->  cases/<repo>_<issue>/repo/
        |
        v
  detect test project (*Test*.csproj / Tests/**/*.csproj)
        |
        v
  AutoRepair/run_all.ps1
   -SourceDir   = folder containing the changed .cs files
   -DriverProject = detected test project
        |
        v
  parse violations.json -> markdown report
   "AutoRepair caught X (runtime trace), SonarQube would not — it is a
    static linter and never observes the failing execution path."

Usage:
    python e2e_demo.py --rank          # rank cases, write candidates.json
    python e2e_demo.py --case 0        # run case #0 from candidates.json
    python e2e_demo.py --all --max 3   # run top-N
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path
from typing import Optional

import requests

ROOT = Path(__file__).resolve().parent.parent
PRH = ROOT / "PRHunter"
OPEN_ISSUES_CSV = PRH / "output" / "exception_hunter" / "open_issues.csv"
CASES_DIR = PRH / "e2e" / "cases"
CANDIDATES_JSON = PRH / "e2e" / "candidates.json"
INDEX_MD = PRH / "e2e" / "REPORT.md"
RUN_ALL = ROOT / "AutoRepair" / "run_all.ps1"

GH_API = "https://api.github.com"
LINK_RE = re.compile(r"([\w.\-]+/[\w.\-]+)#(\d+)\s*\((rejected|reverted)\)")


# --------------------------------------------------------------------------
# GitHub helpers
# --------------------------------------------------------------------------

class GH:
    def __init__(self, token: str):
        self.s = requests.Session()
        self.s.headers.update({
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "User-Agent": "PRHunter-e2e",
        })

    def get(self, path: str, **params):
        url = path if path.startswith("http") else f"{GH_API}{path}"
        for _ in range(5):
            r = self.s.get(url, params=params)
            if r.status_code == 403 and "rate limit" in r.text.lower():
                reset = int(r.headers.get("X-RateLimit-Reset", time.time() + 60))
                wait = max(5, reset - int(time.time()) + 1)
                print(f"  [rate-limit] sleeping {wait}s")
                time.sleep(wait)
                continue
            if r.status_code == 404:
                return None
            r.raise_for_status()
            return r.json()
        return None

    def pr(self, owner: str, repo: str, number: int):
        return self.get(f"/repos/{owner}/{repo}/pulls/{number}")

    def pr_files(self, owner: str, repo: str, number: int):
        return self.get(f"/repos/{owner}/{repo}/pulls/{number}/files", per_page=100) or []

    def repo_meta(self, owner: str, repo: str):
        return self.get(f"/repos/{owner}/{repo}")


# --------------------------------------------------------------------------
# Ranker: pick demo-feasible cases out of ~60 open issues
# --------------------------------------------------------------------------

def parse_linked(linked: str) -> list[tuple[str, int, str]]:
    out = []
    for m in LINK_RE.finditer(linked or ""):
        owner_repo, num, kind = m.group(1), int(m.group(2)), m.group(3)
        out.append((owner_repo, num, kind))
    return out


def rank(gh: GH) -> list[dict]:
    """Score each candidate; smaller repos with C#-only test projects score higher."""
    if not OPEN_ISSUES_CSV.exists():
        sys.exit(f"missing {OPEN_ISSUES_CSV} — run exception_hunter.py first")

    rows = list(csv.DictReader(OPEN_ISSUES_CSV.open(encoding="utf-8")))
    print(f"ranking {len(rows)} open-issue candidates...")
    cands = []
    for i, row in enumerate(rows):
        repo = row["repo"]
        owner, name = repo.split("/", 1)
        meta = gh.repo_meta(owner, name)
        if not meta:
            continue
        if (meta.get("language") or "").lower() not in {"c#", "csharp"}:
            continue
        size_kb = meta.get("size", 0)
        score = 0
        # Prefer small repos (faster to clone + build)
        if size_kb < 5_000:        score += 30
        elif size_kb < 50_000:     score += 15
        elif size_kb < 200_000:    score += 5
        # Prefer rejected PRs over reverted (rejected = closed unmerged, the
        # change was actively pushed back, so the bug demonstrably escaped review)
        links = parse_linked(row["linked_prs"])
        if any(k == "rejected" for *_, k in links): score += 10
        if not links:
            continue
        cands.append({
            "repo": repo,
            "issue_number": int(row["issue_number"]),
            "issue_title": row["issue_title"],
            "issue_url": row["issue_url"],
            "exception": row["matched_keyword"],
            "linked_prs": [{"repo": r, "number": n, "kind": k} for r, n, k in links],
            "repo_size_kb": size_kb,
            "score": score,
        })
        print(f"  [{i+1}/{len(rows)}] {repo}#{row['issue_number']}  size={size_kb}  score={score}")

    cands.sort(key=lambda c: -c["score"])
    CANDIDATES_JSON.parent.mkdir(parents=True, exist_ok=True)
    CANDIDATES_JSON.write_text(json.dumps(cands, indent=2), encoding="utf-8")
    print(f"\nwrote {len(cands)} ranked candidates -> {CANDIDATES_JSON}")
    return cands


# --------------------------------------------------------------------------
# Per-case runner
# --------------------------------------------------------------------------

def run(cmd: list[str], cwd: Optional[Path] = None, check: bool = True) -> subprocess.CompletedProcess:
    print("  $ " + " ".join(str(c) for c in cmd))
    r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    if check and r.returncode != 0:
        print(r.stdout[-2000:])
        print(r.stderr[-2000:])
        raise SystemExit(f"command failed: {cmd}")
    return r


def find_test_project(repo_dir: Path) -> Optional[Path]:
    candidates = [p for p in repo_dir.rglob("*.csproj")
                  if re.search(r"(test|tests|spec)\.csproj$", p.name, re.IGNORECASE)]
    if not candidates:
        return None
    candidates.sort(key=lambda p: len(p.parts))
    return candidates[0]


def find_project_dir_for_file(cs_file: Path, repo_dir: Path) -> Optional[Path]:
    """Walk up from cs_file to find the nearest .csproj; return its directory."""
    candidate = cs_file.parent
    while candidate != repo_dir.parent:
        if any(candidate.glob("*.csproj")):
            return candidate
        candidate = candidate.parent
    return None


def find_source_dir(cs_files: list[str], repo_dir: Path) -> tuple[Path, Optional[Path]]:
    """
    Return (src_dir, src_csproj) where src_dir is the directory that should be
    passed as -SourceDir to run_all.ps1, and src_csproj is the .csproj file
    for Step 3 (CFGAnalyzer).  We prefer the non-test project that contains
    the most touched .cs files.
    """
    # Map each touched file -> project dir
    by_proj: dict[Path, list[str]] = {}
    for f in cs_files:
        proj_dir = find_project_dir_for_file(repo_dir / f, repo_dir)
        if proj_dir:
            by_proj.setdefault(proj_dir, []).append(f)

    if not by_proj:
        # Fallback: lowest common ancestor (original behaviour)
        paths = [repo_dir / f for f in cs_files]
        candidate = paths[0].parent
        for p in paths[1:]:
            while not str(p).startswith(str(candidate)):
                candidate = candidate.parent
        return candidate, None

    # Prefer non-test project with most touched files
    def proj_score(item):
        proj_dir, files = item
        is_test = bool(re.search(r"(test|tests|spec)", proj_dir.name, re.IGNORECASE))
        return (0 if is_test else 1, len(files))

    best_dir = max(by_proj.items(), key=proj_score)[0]
    csproj = next(best_dir.glob("*.csproj"), None)
    return best_dir, csproj


def run_case(gh: GH, cand: dict, idx: int) -> dict:
    pr_info = cand["linked_prs"][0]
    pr_repo = pr_info["repo"]
    pr_num = pr_info["number"]
    owner, name = pr_repo.split("/", 1)

    case_id = f"{idx:02d}_{owner}_{name}_issue{cand['issue_number']}"
    case_dir = CASES_DIR / case_id
    case_dir.mkdir(parents=True, exist_ok=True)
    repo_dir = case_dir / "repo"

    print(f"\n=== case {case_id} ===")
    print(f"    issue: {cand['issue_url']}")
    print(f"    linked PR: https://github.com/{pr_repo}/pull/{pr_num}  ({pr_info['kind']})")

    pr = gh.pr(owner, name, pr_num)
    if not pr:
        return {"case": case_id, "status": "pr-not-found"}
    base_sha = pr["base"]["sha"]
    files = gh.pr_files(owner, name, pr_num)
    cs_files = [f["filename"] for f in files if f["filename"].endswith(".cs")]
    if not cs_files:
        return {"case": case_id, "status": "no-cs-files-touched"}

    (case_dir / "manifest.json").write_text(json.dumps({
        "candidate": cand,
        "base_sha": base_sha,
        "touched_files": cs_files,
    }, indent=2), encoding="utf-8")

    if not repo_dir.exists():
        run(["git", "clone", "--depth", "50", f"https://github.com/{pr_repo}.git", str(repo_dir)])
        run(["git", "fetch", "--depth", "1", "origin", base_sha], cwd=repo_dir, check=False)
        r = run(["git", "checkout", base_sha], cwd=repo_dir, check=False)
        if r.returncode != 0:
            run(["git", "fetch", "--unshallow", "origin"], cwd=repo_dir, check=False)
            run(["git", "checkout", base_sha], cwd=repo_dir)

    test_proj = find_test_project(repo_dir)
    if not test_proj:
        return {"case": case_id, "status": "no-test-project"}

    src_dir, src_csproj = find_source_dir(cs_files, repo_dir)
    if src_dir.is_file():
        src_dir = src_dir.parent
    # Fall back to the test project .csproj for Step 3 if we couldn't find
    # the source project (CFGAnalyzer handles .csproj or .sln).
    solution_arg = str(src_csproj) if src_csproj else str(test_proj)

    print(f"    src   : {src_dir.relative_to(repo_dir)}")
    print(f"    tests : {test_proj.relative_to(repo_dir)}")
    print(f"    csproj: {Path(solution_arg).relative_to(repo_dir)}")

    log_path = case_dir / "autorepair.log"
    pwsh = shutil.which("pwsh") or shutil.which("powershell")
    with log_path.open("w", encoding="utf-8") as log:
        proc = subprocess.run(
            [pwsh, "-NoProfile", "-File", str(RUN_ALL),
             "-SourceDir", str(src_dir),
             "-DriverProject", str(test_proj),
             "-Solution", solution_arg],
            stdout=log, stderr=subprocess.STDOUT, text=True,
        )
    status = "ok" if proc.returncode == 0 else f"autorepair-exit-{proc.returncode}"

    violations_src = ROOT / "AutoRepair" / "Step2_Constraints" / "reports" / "violations.json"
    if violations_src.exists():
        shutil.copy2(violations_src, case_dir / "violations.json")

    return {"case": case_id, "status": status, "src": str(src_dir.relative_to(repo_dir)),
            "test_project": str(test_proj.relative_to(repo_dir))}


# --------------------------------------------------------------------------
# Report rendering
# --------------------------------------------------------------------------

def render_case_report(case_dir: Path) -> str:
    manifest = json.loads((case_dir / "manifest.json").read_text(encoding="utf-8"))
    cand = manifest["candidate"]
    pr = cand["linked_prs"][0]

    violations_path = case_dir / "violations.json"
    violations = []
    if violations_path.exists():
        try:
            violations = json.loads(violations_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            violations = []

    lines = [
        f"# {cand['repo']}#{cand['issue_number']} - {cand['exception']}",
        "",
        f"- **Issue**: [{cand['issue_title']}]({cand['issue_url']})",
        f"- **Failed PR** ({pr['kind']}): https://github.com/{pr['repo']}/pull/{pr['number']}",
        f"- **Base SHA** (buggy state): `{manifest['base_sha']}`",
        "",
        "## Files touched by the failed fix",
        "",
    ]
    lines += [f"- `{f}`" for f in manifest["touched_files"]]
    lines += [
        "",
        "## AutoRepair findings",
        "",
        f"`violations.json`: **{len(violations)} violations** captured from the live test trace.",
        "",
    ]
    for v in violations[:10]:
        lines.append(f"- `{v.get('file','?')}:{v.get('line','?')}` - "
                     f"{v.get('predicate','?')}  ->  {v.get('result','?')}")
    if len(violations) > 10:
        lines.append(f"- ... and {len(violations) - 10} more")

    lines += [
        "",
        "## Why SonarQube misses this",
        "",
        "SonarQube is a static analyzer. It inspects source text for syntactic",
        "patterns and shallow data-flow facts. The bug above only manifests on",
        "a specific runtime path: the variable bindings, buffer sizes, or cast",
        "targets that produce the violation are not recoverable from source",
        "alone. AutoRepair instruments the code, runs the existing test suite,",
        "and observes the actual failing trace - the violation is reported",
        "with concrete evidence (file, line, predicate, observed values).",
        "",
    ]
    return "\n".join(lines)


def render_index(results: list[dict]) -> None:
    lines = [
        "# PRHunter end-to-end demo report",
        "",
        "Each row is a real open issue mined from GitHub whose fix attempt was",
        "either rejected (closed unmerged) or reverted (merged then undone).",
        "AutoRepair was run against the buggy base SHA of each failed PR.",
        "",
        "| # | Repo / Issue | Exception | Status | Report |",
        "|---|---|---|---|---|",
    ]
    for i, r in enumerate(results):
        cd = CASES_DIR / r["case"]
        manifest = json.loads((cd / "manifest.json").read_text(encoding="utf-8"))
        cand = manifest["candidate"]
        link = f"[{cand['repo']}#{cand['issue_number']}]({cand['issue_url']})"
        report_link = f"[case]({cd.relative_to(PRH / 'e2e').as_posix()}/report.md)"
        lines.append(f"| {i} | {link} | {cand['exception']} | {r['status']} | {report_link} |")
    INDEX_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\nwrote index -> {INDEX_MD}")


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--rank", action="store_true", help="re-score candidates from open_issues.csv")
    ap.add_argument("--all", action="store_true", help="run all top candidates")
    ap.add_argument("--case", type=int, help="run a single case index")
    ap.add_argument("--max", type=int, default=3, help="how many top cases to run with --all")
    ap.add_argument("--token", default=os.environ.get("GITHUB_TOKEN"))
    args = ap.parse_args()

    if not args.token:
        sys.exit("set GITHUB_TOKEN or pass --token")

    gh = GH(args.token)

    if args.rank or not CANDIDATES_JSON.exists():
        cands = rank(gh)
    else:
        cands = json.loads(CANDIDATES_JSON.read_text(encoding="utf-8"))

    if args.case is None and not args.all:
        print(f"\nTop {min(10, len(cands))} candidates:")
        for i, c in enumerate(cands[:10]):
            print(f"  [{i:>2}] score={c['score']:>2}  {c['repo']}#{c['issue_number']}  ({c['exception']})")
        print("\nrun a case with: --case <i>   or many with: --all --max <n>")
        return

    targets = [cands[args.case]] if args.case is not None else cands[: args.max]
    results = []
    for i, cand in enumerate(targets):
        idx = args.case if args.case is not None else i
        try:
            res = run_case(gh, cand, idx)
        except SystemExit as e:
            res = {"case": f"{idx:02d}_error", "status": f"error: {e}"}
        results.append(res)
        case_dir = CASES_DIR / res["case"]
        if (case_dir / "manifest.json").exists():
            (case_dir / "report.md").write_text(render_case_report(case_dir), encoding="utf-8")

    render_index(results)


if __name__ == "__main__":
    main()

"""
PRHunter — search for reverted and rejected pull requests on C# repositories.

Outputs two CSV files (reverted_prs.csv, rejected_prs.csv) under PRHunter/output/.
Each row also lists the issue numbers the PR claimed to close so the data can
later be joined with Dimitrios's findings and fed into the AutoRepair tool.

Usage:
    python pr_hunter.py                          # all default repos, --max 50 each
    python pr_hunter.py --repo dotnet/roslyn     # one repo
    python pr_hunter.py --max 200                # bigger sample per repo
    python pr_hunter.py --token ghp_xxx          # explicit token
                                                  (else GITHUB_TOKEN env var)
"""

from __future__ import annotations

import argparse
import csv
import os
import re
import sys
import time
from pathlib import Path
from typing import Iterable, Iterator

import requests

DEFAULT_REPOS = [
    "dotnet/runtime",
    "dotnet/roslyn",
    "dotnet/sdk",
    "dotnet/csharplang",
    "dotnet/aspnetcore",
    "dotnet/efcore",
]

API = "https://api.github.com"
SEARCH_API = f"{API}/search/issues"

# "Closes #123", "Fixes: #45", "resolves project/repo#9"
LINK_RE = re.compile(
    r"\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\b[:\s]+#(\d+)",
    re.IGNORECASE,
)

# "Reverts #1234", "Reverts dotnet/runtime#1234", "This reverts commit ...
# pull request #1234"
REVERT_TARGET_RE = re.compile(
    r"(?:reverts?|reverting)\s+(?:[\w.-]+/[\w.-]+)?#(\d+)",
    re.IGNORECASE,
)

CSV_COLUMNS = [
    "repo",
    "pr_number",
    "pr_title",
    "pr_url",
    "pr_state",
    "merged_at",
    "closed_at",
    "author",
    "linked_issues",
    "category",
    "evidence",
]


def make_session(token: str | None) -> requests.Session:
    s = requests.Session()
    s.headers.update(
        {
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "PRHunter/1.0",
        }
    )
    if token:
        s.headers["Authorization"] = f"Bearer {token}"
    return s


def respect_rate_limit(resp: requests.Response) -> None:
    """If the response indicates rate-limiting, sleep until reset."""
    if resp.status_code == 403 and "rate limit" in resp.text.lower():
        reset = int(resp.headers.get("X-RateLimit-Reset", "0"))
        wait = max(reset - int(time.time()), 1) + 1
        print(f"  [rate-limited] sleeping {wait}s", file=sys.stderr)
        time.sleep(wait)
        return
    remaining = resp.headers.get("X-RateLimit-Remaining")
    if remaining is not None and remaining.isdigit() and int(remaining) <= 2:
        reset = int(resp.headers.get("X-RateLimit-Reset", "0"))
        wait = max(reset - int(time.time()), 1) + 1
        print(f"  [rate budget low: {remaining}] sleeping {wait}s", file=sys.stderr)
        time.sleep(wait)


def search_issues(
    session: requests.Session, query: str, max_results: int
) -> Iterator[dict]:
    """Yield issue/PR items from the GitHub search API up to max_results."""
    per_page = 100
    fetched = 0
    page = 1
    while fetched < max_results:
        params = {
            "q": query,
            "per_page": min(per_page, max_results - fetched),
            "page": page,
            "sort": "updated",
            "order": "desc",
        }
        resp = session.get(SEARCH_API, params=params, timeout=30)
        respect_rate_limit(resp)
        if resp.status_code != 200:
            print(
                f"  [error] search failed (HTTP {resp.status_code}): {resp.text[:200]}",
                file=sys.stderr,
            )
            return
        data = resp.json()
        items = data.get("items", [])
        if not items:
            return
        for item in items:
            yield item
            fetched += 1
            if fetched >= max_results:
                return
        # Search API caps at 1000 results regardless of pagination.
        if len(items) < per_page or fetched >= 1000:
            return
        page += 1


def fetch_pr(session: requests.Session, repo: str, number: int) -> dict | None:
    """Fetch the full PR object (search results don't include merge metadata)."""
    resp = session.get(f"{API}/repos/{repo}/pulls/{number}", timeout=30)
    respect_rate_limit(resp)
    if resp.status_code != 200:
        return None
    return resp.json()


def extract_linked_issues(body: str | None) -> list[str]:
    if not body:
        return []
    return sorted({m.group(1) for m in LINK_RE.finditer(body)})


def build_row(
    repo: str, pr: dict, category: str, evidence: str
) -> dict:
    return {
        "repo": repo,
        "pr_number": pr.get("number"),
        "pr_title": (pr.get("title") or "").replace("\n", " ").strip(),
        "pr_url": pr.get("html_url", ""),
        "pr_state": pr.get("state", ""),
        "merged_at": pr.get("merged_at") or "",
        "closed_at": pr.get("closed_at") or "",
        "author": (pr.get("user") or {}).get("login", ""),
        "linked_issues": ";".join(extract_linked_issues(pr.get("body"))),
        "category": category,
        "evidence": evidence,
    }


def find_reverted(
    session: requests.Session, repo: str, max_results: int
) -> list[dict]:
    """Find PRs whose merge was undone by a later 'Revert ...' PR."""
    print(f"[{repo}] searching for revert PRs...", flush=True)
    rows: list[dict] = []
    # Revert PRs themselves: title contains the word "Revert".
    query = f"repo:{repo} is:pr is:merged in:title Revert"
    seen_targets: set[int] = set()
    for revert_item in search_issues(session, query, max_results):
        revert_title = revert_item.get("title", "")
        revert_num = revert_item["number"]
        target_num: int | None = None
        # 1) Title pattern: Revert "Original PR title" (#1234)
        m = re.search(r"\(#(\d+)\)\s*$", revert_title)
        if m:
            target_num = int(m.group(1))
        else:
            # 2) Fall back to fetching the revert PR's body and looking for
            #    "Reverts #1234" / "Reverts owner/repo#1234".
            revert_pr = fetch_pr(session, repo, revert_num)
            body = (revert_pr or {}).get("body") or ""
            m2 = REVERT_TARGET_RE.search(body)
            if m2:
                target_num = int(m2.group(1))
        if target_num is None or target_num in seen_targets:
            continue
        seen_targets.add(target_num)
        target_pr = fetch_pr(session, repo, target_num)
        if not target_pr or not target_pr.get("merged_at"):
            continue
        evidence = (
            f"reverted by PR #{revert_num}: {revert_title}"
        )
        rows.append(build_row(repo, target_pr, "reverted", evidence))
        print(
            f"  reverted PR #{target_num}: {target_pr.get('title','')[:80]}",
            flush=True,
        )
    return rows


def find_rejected(
    session: requests.Session, repo: str, max_results: int
) -> list[dict]:
    """Find PRs that were closed without being merged."""
    print(f"[{repo}] searching for rejected (closed-unmerged) PRs...", flush=True)
    rows: list[dict] = []
    query = f"repo:{repo} is:pr is:closed is:unmerged"
    for item in search_issues(session, query, max_results):
        # The search result is enough for most fields, but we still need the
        # body for issue links and the merge_commit_sha to be sure.
        pr = fetch_pr(session, repo, item["number"])
        if pr is None:
            continue
        if pr.get("merged_at"):
            continue  # paranoia: skip if it was actually merged
        evidence = "closed without merge"
        rows.append(build_row(repo, pr, "rejected", evidence))
    print(f"  collected {len(rows)} rejected PRs", flush=True)
    return rows


def write_csv(path: Path, rows: Iterable[dict]) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    rows = list(rows)
    with path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=CSV_COLUMNS)
        writer.writeheader()
        writer.writerows(rows)
    return len(rows)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo",
        action="append",
        help="Repo in 'owner/name' form. May be passed multiple times. "
        "Defaults to the C#-heavy dotnet/* set.",
    )
    parser.add_argument(
        "--max",
        type=int,
        default=50,
        help="Max PRs to collect per repo per category (default: 50).",
    )
    parser.add_argument(
        "--token",
        default=os.environ.get("GITHUB_TOKEN"),
        help="GitHub token (else read from GITHUB_TOKEN env var).",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path(__file__).parent / "output",
        help="Directory to write the CSVs to (default: ./output).",
    )
    parser.add_argument(
        "--skip-rejected",
        action="store_true",
        help="Only collect reverted PRs (rejected search is heavier).",
    )
    args = parser.parse_args(argv)

    repos = args.repo or DEFAULT_REPOS
    if not args.token:
        print(
            "[warn] no GitHub token; anonymous limit is 60 req/h and search "
            "is 10 req/min. Set GITHUB_TOKEN for real runs.",
            file=sys.stderr,
        )

    session = make_session(args.token)

    reverted: list[dict] = []
    rejected: list[dict] = []
    for repo in repos:
        try:
            reverted.extend(find_reverted(session, repo, args.max))
            if not args.skip_rejected:
                rejected.extend(find_rejected(session, repo, args.max))
        except KeyboardInterrupt:
            print("[interrupted] writing partial results...", file=sys.stderr)
            break

    rev_path = args.output_dir / "reverted_prs.csv"
    rej_path = args.output_dir / "rejected_prs.csv"
    n_rev = write_csv(rev_path, reverted)
    n_rej = write_csv(rej_path, rejected)
    print()
    print(f"=== Done ===")
    print(f"  Reverted PRs: {n_rev}  -> {rev_path}")
    print(f"  Rejected PRs: {n_rej}  -> {rej_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

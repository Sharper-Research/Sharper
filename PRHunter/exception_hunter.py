"""
ExceptionHunter — exhaustive GitHub search for C# exception-themed open issues
linked to PRs that were either REJECTED (closed without merge) or REVERTED
(merged and later reverted).

Strategy (PR-first, because it scales better than issue-first):

  Phase 1 — Find candidate PRs globally on GitHub:
    A. Rejected PRs:
         is:pr is:closed is:unmerged language:C# "<ExceptionName>" in:title,body
    B. Reverted PRs (the *revert* PR itself; we follow its body to the
         original PR):
         is:pr "This reverts commit" language:C# "<ExceptionName>"

  Phase 2 — For each candidate PR:
    * Fetch full PR (title, body, state, merged_at).
    * Extract linked issue numbers from PR body via Closes/Fixes/Resolves #N
      (and full-URL form, ONLY when preceded by a closes/fixes/resolves verb —
      this avoids the "PR merely mentions another issue" false positive).
    * For each linked issue, fetch it and KEEP only if:
         - state == "open"
         - title or body mentions an exception keyword
    * For reverted PRs: also follow the "This reverts commit <sha>" pointer
      to find the original merged PR and use *its* linked issues.

  Phase 3 — Write three CSVs into PRHunter/output/:
        rejected_prs.csv
        reverted_prs.csv
        open_issues.csv      (the deduped union of linked open issues —
                              this is the answer the user asked for)

Pagination: GitHub Search caps at 1000 results per query, so we partition
by exception keyword (one query per keyword * category). For each keyword we
walk all 10 pages of 100 results until exhausted.

Auth: reads $GITHUB_TOKEN from env. Without one the script will refuse to run
exhaustively (prints a warning and exits) — the unauthenticated 10/min search
limit makes "exhaustive" impossible.
"""

from __future__ import annotations

import argparse
import csv
import os
import re
import sys
import time
from dataclasses import dataclass, field, asdict
from pathlib import Path
from typing import Iterable, Iterator

import requests


# ---------------------------------------------------------------------------
# Logging — write to file with line buffering AND mirror to stdout
# ---------------------------------------------------------------------------

_LOG_FH = None

def log(msg: str = "") -> None:
    print(msg, flush=True)
    if _LOG_FH is not None:
        _LOG_FH.write(msg + "\n")
        _LOG_FH.flush()


# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

EXCEPTION_KEYWORDS: list[str] = [
    "NullReferenceException",
    "InvalidCastException",
    "IndexOutOfRangeException",
    "ArgumentNullException",
    "ArgumentOutOfRangeException",
    "ArgumentException",
    "InvalidOperationException",
    "KeyNotFoundException",
    "FormatException",
    "OverflowException",
    "StackOverflowException",
    "DivideByZeroException",
    "NotSupportedException",
    "NotImplementedException",
    "ObjectDisposedException",
    "AggregateException",
    "TaskCanceledException",
    "OperationCanceledException",
    "TimeoutException",
    "FileNotFoundException",
    "DirectoryNotFoundException",
    "IOException",
    "UnauthorizedAccessException",
    "OutOfMemoryException",
    "TypeLoadException",
    "MissingMethodException",
    "MissingFieldException",
    "AccessViolationException",
    "BadImageFormatException",
    "JsonException",
    "XmlException",
    "SerializationException",
    "HttpRequestException",
    "SocketException",
    "DbUpdateException",
]

API = "https://api.github.com"
GH_ACCEPT = "application/vnd.github+json"
PER_PAGE = 100
MAX_SEARCH_RESULTS = 1000  # GitHub hard cap per query

# Verb that must precede an issue reference for it to count as a real "this PR
# closes that issue" link. Used by both the same-repo `#N` pattern and the
# full-URL pattern to suppress false positives where a PR merely *mentions*
# another repository's issue.
_LINK_VERB = r"(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)"

# "Closes #123", "Fixes: #45", "resolves #9"  — same-repo
LINK_RE = re.compile(
    rf"\b{_LINK_VERB}\b[^\n#]*?#(\d+)",
    re.IGNORECASE,
)
# "Closes https://github.com/owner/repo/issues/123" — cross-repo, but only
# when preceded by a closing verb within ~80 chars. Without that guard we
# tag any drive-by URL mention as a link, which produced obviously wrong
# rows like dotnet/razor#10375 -> JeevanJames/IniFile#211 in earlier runs.
LINK_URL_RE = re.compile(
    rf"\b{_LINK_VERB}\b[^\n]{{0,80}}?https?://github\.com/([\w.\-]+)/([\w.\-]+)/issues/(\d+)",
    re.IGNORECASE,
)
REVERT_SHA_RE = re.compile(r"This reverts commit ([0-9a-f]{7,40})", re.IGNORECASE)
# Real revert PR titles look like:   Revert "Foo bar (#123)"
# i.e. the trailing token is a quote, not a paren. Allow optional trailing
# quote / whitespace so we actually capture #N.
REVERT_PR_RE = re.compile(r"\(#(\d+)\)\s*[\"'\u201d\u2019]?\s*$")


# ---------------------------------------------------------------------------
# HTTP layer with auth + rate-limit politeness
# ---------------------------------------------------------------------------

class GH:
    def __init__(self, token: str | None) -> None:
        self.s = requests.Session()
        h = {"Accept": GH_ACCEPT, "User-Agent": "ExceptionHunter/1.0"}
        if token:
            h["Authorization"] = f"Bearer {token}"
        self.s.headers.update(h)
        self.token = token

    def get(self, url: str, params: dict | None = None) -> requests.Response | None:
        for attempt in range(8):
            try:
                r = self.s.get(url, params=params, timeout=30)
            except requests.exceptions.RequestException as e:
                wait = min(60, 2 ** attempt)
                print(f"  [net-error attempt {attempt+1}] {e.__class__.__name__}: {e} -- sleep {wait}s",
                      file=sys.stderr, flush=True)
                time.sleep(wait)
                continue
            # secondary rate-limit / abuse detection
            if r.status_code == 403 and "rate limit" in r.text.lower():
                reset = int(r.headers.get("X-RateLimit-Reset", "0"))
                wait = max(5, reset - int(time.time()) + 2)
                print(f"  [rate-limit] sleeping {wait}s", file=sys.stderr, flush=True)
                time.sleep(min(wait, 120))
                continue
            if r.status_code == 422:
                # search invalid (e.g. > 1000 results)
                return r
            if r.status_code in (502, 503, 504):
                time.sleep(2 ** attempt)
                continue
            return r
        return None

    def search_issues(self, q: str) -> Iterator[dict]:
        """Yield every search hit for q, walking pages until 1000-cap or exhaustion."""
        for page in range(1, (MAX_SEARCH_RESULTS // PER_PAGE) + 1):
            r = self.get(
                f"{API}/search/issues",
                params={"q": q, "per_page": PER_PAGE, "page": page,
                        "sort": "created", "order": "desc"},
            )
            if r is None or r.status_code != 200:
                if r is not None and r.status_code == 422:
                    return
                print(f"  [warn] search failed: {r.status_code if r else 'n/a'} q={q!r}",
                      file=sys.stderr)
                return
            data = r.json()
            items = data.get("items", [])
            if not items:
                return
            for it in items:
                yield it
            if len(items) < PER_PAGE:
                return
            # gentle pacing for the 30 search-req/min authenticated cap
            time.sleep(2.2)


# ---------------------------------------------------------------------------
# Data model
# ---------------------------------------------------------------------------

@dataclass
class PRRecord:
    repo: str
    pr_number: int
    pr_title: str
    pr_url: str
    state: str
    merged: bool
    merged_at: str | None
    closed_at: str | None
    author: str
    category: str           # "rejected" or "reverted"
    matched_keyword: str
    linked_issue_refs: list[str] = field(default_factory=list)
    evidence: str = ""

    def row(self) -> dict:
        d = asdict(self)
        d["linked_issue_refs"] = ";".join(self.linked_issue_refs)
        return d


@dataclass
class IssueRecord:
    repo: str
    issue_number: int
    issue_title: str
    issue_url: str
    state: str
    matched_keyword: str
    linked_prs: list[str] = field(default_factory=list)  # "owner/repo#N (category)"

    def row(self) -> dict:
        d = asdict(self)
        d["linked_prs"] = ";".join(self.linked_prs)
        return d


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def repo_from_url(url: str) -> str:
    # https://api.github.com/repos/owner/repo/issues/123  ->  owner/repo
    m = re.search(r"/repos/([^/]+/[^/]+)/", url)
    return m.group(1) if m else ""


def parse_linked_issues(body: str | None, default_repo: str) -> set[tuple[str, int]]:
    """Return {(owner/repo, number)} for issues this PR claims to close.

    Both patterns require a closing verb (close/fix/resolve, with common
    variants) to precede the reference. Bare `#N` mentions or drive-by URL
    mentions are ignored — they were the source of cross-repo false positives
    in the earlier dataset (e.g. a dotnet/razor issue tagged as linked from
    JeevanJames/IniFile#211).
    """
    if not body:
        return set()
    found: set[tuple[str, int]] = set()
    for m in LINK_RE.finditer(body):
        try:
            found.add((default_repo, int(m.group(1))))
        except ValueError:
            pass
    for m in LINK_URL_RE.finditer(body):
        try:
            found.add((f"{m.group(1)}/{m.group(2)}", int(m.group(3))))
        except ValueError:
            pass
    return found


def keyword_in(text: str | None, keywords: Iterable[str]) -> str | None:
    if not text:
        return None
    low = text.lower()
    for k in keywords:
        if k.lower() in low:
            return k
    return None


# ---------------------------------------------------------------------------
# Main pipeline
# ---------------------------------------------------------------------------

def hunt_rejected(gh: GH, kw: str) -> Iterator[PRRecord]:
    q = f'"{kw}" in:title,body language:C# is:pr is:closed is:unmerged'
    for it in gh.search_issues(q):
        repo = repo_from_url(it.get("repository_url", "")) or repo_from_url(it.get("url", ""))
        yield PRRecord(
            repo=repo,
            pr_number=it["number"],
            pr_title=it.get("title", ""),
            pr_url=it.get("html_url", ""),
            state=it.get("state", ""),
            merged=False,
            merged_at=None,
            closed_at=it.get("closed_at"),
            author=(it.get("user") or {}).get("login", ""),
            category="rejected",
            matched_keyword=kw,
            evidence="closed without merge",
        )


def hunt_reverted(gh: GH, kw: str) -> Iterator[PRRecord]:
    """
    Search for *revert* PRs whose body mentions the exception keyword.
    Each revert PR points back (via "This reverts commit <sha>" or
    'Revert "... (#N)"') to the original PR that got merged then undone.
    We yield a record for the *original* PR (category=reverted).
    """
    q = f'"{kw}" "This reverts commit" in:body language:C# is:pr'
    for it in gh.search_issues(q):
        repo = repo_from_url(it.get("repository_url", "")) or repo_from_url(it.get("url", ""))
        body = it.get("body") or ""
        title = it.get("title") or ""
        # Try to recover the original PR number from the revert title
        orig_pr_num: int | None = None
        m = REVERT_PR_RE.search(title)
        if m:
            try:
                orig_pr_num = int(m.group(1))
            except ValueError:
                orig_pr_num = None
        sha_m = REVERT_SHA_RE.search(body)
        evidence = f"reverted by #{it['number']}"
        if sha_m:
            evidence += f" (reverts {sha_m.group(1)[:10]})"
        # If we can't identify the original PR, still emit the revert PR itself
        # so the user can inspect. Mark category=reverted.
        yield PRRecord(
            repo=repo,
            pr_number=orig_pr_num if orig_pr_num else it["number"],
            pr_title=title,
            pr_url=it.get("html_url", ""),
            state="closed",
            merged=True,
            merged_at=None,
            closed_at=it.get("closed_at"),
            author=(it.get("user") or {}).get("login", ""),
            category="reverted",
            matched_keyword=kw,
            evidence=evidence,
        )


def fetch_pr_full(gh: GH, repo: str, num: int) -> dict | None:
    if not repo:
        return None
    r = gh.get(f"{API}/repos/{repo}/pulls/{num}")
    if r is None or r.status_code != 200:
        # Fallback to issues endpoint (works when number references an issue not a PR)
        r2 = gh.get(f"{API}/repos/{repo}/issues/{num}")
        return r2.json() if r2 and r2.status_code == 200 else None
    return r.json()


def fetch_issue(gh: GH, repo: str, num: int) -> dict | None:
    r = gh.get(f"{API}/repos/{repo}/issues/{num}")
    if r is None or r.status_code != 200:
        return None
    j = r.json()
    if "pull_request" in j:  # it's actually a PR
        return None
    return j


# ---------------------------------------------------------------------------

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="PRHunter/output/exception_hunter")
    ap.add_argument("--keywords", nargs="*", default=EXCEPTION_KEYWORDS,
                    help="Override default exception keyword list.")
    ap.add_argument("--skip-rejected", action="store_true")
    ap.add_argument("--skip-reverted", action="store_true")
    ap.add_argument("--max-pr-resolve", type=int, default=0,
                    help="Limit number of PRs to resolve back to issues "
                         "(0 = no limit).")
    args = ap.parse_args()

    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        print("ERROR: $GITHUB_TOKEN not set. Set it before running for an "
              "exhaustive search.", file=sys.stderr)
        return 2

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    global _LOG_FH
    _LOG_FH = (out / "run.log").open("a", encoding="utf-8", buffering=1)
    log(f"\n===== run started =====")
    gh = GH(token)

    # ---- Resume support: read any existing CSVs and prior progress ----
    pr_fields = list(PRRecord("", 0, "", "", "", False, None, None, "", "", "").row().keys())
    rej_path = out / "rejected_prs.csv"
    rev_path = out / "reverted_prs.csv"
    done_path = out / "done_keywords.txt"

    def load_existing(path: Path) -> list[dict]:
        if not path.exists():
            return []
        with path.open("r", encoding="utf-8", newline="") as f:
            return list(csv.DictReader(f))

    existing_rejected = load_existing(rej_path)
    existing_reverted = load_existing(rev_path)
    done_keywords: set[tuple[str, str]] = set()
    if done_path.exists():
        for line in done_path.read_text(encoding="utf-8").splitlines():
            if "|" in line:
                cat, kw = line.strip().split("|", 1)
                done_keywords.add((cat, kw))

    # Open append-mode CSVs (write headers if new)
    def open_append(path: Path):
        new = not path.exists()
        f = path.open("a", encoding="utf-8", newline="", buffering=1)
        w = csv.DictWriter(f, fieldnames=pr_fields)
        if new:
            w.writeheader()
        return f, w

    rej_f, rej_w = open_append(rej_path)
    rev_f, rev_w = open_append(rev_path)

    log(f"Resuming: rejected_csv={len(existing_rejected)} rows, "
        f"reverted_csv={len(existing_reverted)} rows, "
        f"done_keywords={len(done_keywords)}")

    # ---- Phase 1: collect candidate PRs ----
    rejected: list[PRRecord] = []   # only NEW this run
    reverted: list[PRRecord] = []

    def mark_done(cat: str, kw: str) -> None:
        with done_path.open("a", encoding="utf-8") as f:
            f.write(f"{cat}|{kw}\n")
        done_keywords.add((cat, kw))

    for kw in args.keywords:
        if not args.skip_rejected:
            if ("rejected", kw) in done_keywords:
                log(f"[rejected] {kw}  (skip, done)")
            else:
                log(f"[rejected] {kw}")
                n0 = len(rejected)
                try:
                    for pr in hunt_rejected(gh, kw):
                        rejected.append(pr)
                        rej_w.writerow(pr.row())
                except Exception as e:
                    log(f"  [error] {e!r} -- partial keep, will retry next run")
                    rej_f.flush()
                    continue
                rej_f.flush()
                mark_done("rejected", kw)
                log(f"  +{len(rejected) - n0}")
        if not args.skip_reverted:
            if ("reverted", kw) in done_keywords:
                log(f"[reverted] {kw}  (skip, done)")
            else:
                log(f"[reverted] {kw}")
                n0 = len(reverted)
                try:
                    for pr in hunt_reverted(gh, kw):
                        reverted.append(pr)
                        rev_w.writerow(pr.row())
                except Exception as e:
                    log(f"  [error] {e!r} -- partial keep, will retry next run")
                    rev_f.flush()
                    continue
                rev_f.flush()
                mark_done("reverted", kw)
                log(f"  +{len(reverted) - n0}")

    rej_f.close()
    rev_f.close()

    # Reload everything fresh from disk for dedupe + Phase 2
    def csv_to_prrecords(path: Path) -> list[PRRecord]:
        rows = load_existing(path)
        out_list: list[PRRecord] = []
        for r in rows:
            try:
                out_list.append(PRRecord(
                    repo=r.get("repo", ""),
                    pr_number=int(r.get("pr_number") or 0),
                    pr_title=r.get("pr_title", ""),
                    pr_url=r.get("pr_url", ""),
                    state=r.get("state", ""),
                    merged=(r.get("merged", "False") == "True"),
                    merged_at=r.get("merged_at") or None,
                    closed_at=r.get("closed_at") or None,
                    author=r.get("author", ""),
                    category=r.get("category", ""),
                    matched_keyword=r.get("matched_keyword", ""),
                    linked_issue_refs=(r.get("linked_issue_refs") or "").split(";")
                                       if r.get("linked_issue_refs") else [],
                    evidence=r.get("evidence", ""),
                ))
            except (ValueError, TypeError):
                continue
        return out_list

    rejected = csv_to_prrecords(rej_path)
    reverted = csv_to_prrecords(rev_path)

    # dedupe (repo, pr_number, category)
    def dedupe(prs: list[PRRecord]) -> list[PRRecord]:
        seen: dict[tuple[str, int, str], PRRecord] = {}
        for p in prs:
            k = (p.repo, p.pr_number, p.category)
            if k not in seen:
                seen[k] = p
        return list(seen.values())

    rejected = dedupe(rejected)
    reverted = dedupe(reverted)
    log(f"\nUnique rejected PRs: {len(rejected)}")
    log(f"Unique reverted PRs: {len(reverted)}\n")

    # ---- Phase 2: resolve linked issues ----
    all_prs = rejected + reverted
    if args.max_pr_resolve and args.max_pr_resolve < len(all_prs):
        log(f"Resolving first {args.max_pr_resolve} PRs (of {len(all_prs)})")
        all_prs_to_resolve = all_prs[: args.max_pr_resolve]
    else:
        all_prs_to_resolve = all_prs

    iss_path = out / "open_issues.csv"
    iss_fields = list(IssueRecord("", 0, "", "", "", "").row().keys())
    iss_new = not iss_path.exists()
    iss_f = iss_path.open("a", encoding="utf-8", newline="", buffering=1)
    iss_w = csv.DictWriter(iss_f, fieldnames=iss_fields)
    if iss_new:
        iss_w.writeheader()

    # Resume: which PRs already resolved
    resolved_path = out / "resolved_prs.txt"
    resolved: set[str] = set()
    if resolved_path.exists():
        resolved = set(resolved_path.read_text(encoding="utf-8").splitlines())
    log(f"Phase 2: {len(all_prs_to_resolve)} PRs total, "
        f"{len(resolved)} already resolved")

    # Pre-load existing open_issues.csv keys for dedupe across runs
    issues: dict[tuple[str, int], IssueRecord] = {}
    if not iss_new:
        for r in load_existing(iss_path):
            try:
                key = (r.get("repo", ""), int(r.get("issue_number") or 0))
            except (ValueError, TypeError):
                continue
            issues[key] = IssueRecord(
                repo=r.get("repo", ""),
                issue_number=key[1],
                issue_title=r.get("issue_title", ""),
                issue_url=r.get("issue_url", ""),
                state=r.get("state", ""),
                matched_keyword=r.get("matched_keyword", ""),
                linked_prs=(r.get("linked_prs") or "").split(";")
                            if r.get("linked_prs") else [],
            )

    for i, pr in enumerate(all_prs_to_resolve, 1):
        if i % 25 == 0:
            log(f"  resolving PR {i}/{len(all_prs_to_resolve)}  "
                f"new_open_issues={len(issues)}")
        pr_key = f"{pr.repo}#{pr.pr_number}#{pr.category}"
        if pr_key in resolved:
            continue
        try:
            full = fetch_pr_full(gh, pr.repo, pr.pr_number)
        except Exception as e:
            log(f"  [warn] fetch_pr {pr_key}: {e!r}")
            continue
        body = (full or {}).get("body") or ""
        links = parse_linked_issues(body, pr.repo)
        for repo, num in links:
            key = (repo, num)
            if key in issues:
                # already on disk; just augment in-memory linked_prs (not rewritten)
                issues[key].linked_prs.append(f"{pr.repo}#{pr.pr_number} ({pr.category})")
                continue
            try:
                iss = fetch_issue(gh, repo, num)
            except Exception as e:
                log(f"  [warn] fetch_issue {repo}#{num}: {e!r}")
                continue
            if not iss or iss.get("state") != "open":
                continue
            kw = (keyword_in(iss.get("title"), EXCEPTION_KEYWORDS)
                  or keyword_in(iss.get("body"), EXCEPTION_KEYWORDS))
            if not kw:
                continue
            rec = IssueRecord(
                repo=repo,
                issue_number=num,
                issue_title=iss.get("title", ""),
                issue_url=iss.get("html_url", ""),
                state=iss.get("state", "open"),
                matched_keyword=kw,
                linked_prs=[f"{pr.repo}#{pr.pr_number} ({pr.category})"],
            )
            issues[key] = rec
            iss_w.writerow(rec.row())
        # mark this PR as resolved
        with resolved_path.open("a", encoding="utf-8") as rf:
            rf.write(pr_key + "\n")
        resolved.add(pr_key)

    iss_f.close()

    # ---- Phase 3: rewrite deduped rejected/reverted CSVs ----
    def write_csv(path: Path, rows: list[dict], fieldnames: list[str]) -> None:
        with path.open("w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=fieldnames)
            w.writeheader()
            for r in rows:
                w.writerow(r)

    write_csv(out / "rejected_prs.csv", [p.row() for p in rejected], pr_fields)
    write_csv(out / "reverted_prs.csv", [p.row() for p in reverted], pr_fields)

    log(f"\nWrote:")
    log(f"  {out/'rejected_prs.csv'}   ({len(rejected)} rows)")
    log(f"  {out/'reverted_prs.csv'}   ({len(reverted)} rows)")
    log(f"  {out/'open_issues.csv'}    ({len(issues)} rows)  <-- main answer")
    if _LOG_FH is not None:
        _LOG_FH.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""
fetch_and_analyze.py — End-to-end AutoRepair pipeline for external GitHub repos.

Searches GitHub Issues for a small C# repository with an open
NullReferenceException bug, clones it, instruments the source with the
Roslyn-based tracer (Step 1), runs the test suite to collect an execution
trace, and feeds the trace into the constraint analyser (Step 2) to produce
a violation report.

Usage
-----
    python fetch_and_analyze.py                        # auto-search GitHub
    python fetch_and_analyze.py --repo owner/repo      # use a specific repo
    python fetch_and_analyze.py --token ghp_xxxx       # authenticate (higher rate limits)
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

# ── Paths (relative to this script, which lives in AutoRepair/) ─────────────
SCRIPT_DIR    = Path(__file__).resolve().parent
REPO_ROOT     = SCRIPT_DIR.parent                                   # Patching/
STEP1_DIR     = SCRIPT_DIR / "Step1_Tracing"
STEP2_DIR     = SCRIPT_DIR / "Step2_Constraints"
INSTR_PROJ    = STEP1_DIR  / "Instrumenter" / "Instrumenter.csproj"
CONSTR_PROJ   = STEP2_DIR  / "Constraints"  / "Constraints.csproj"
WORK_DIR      = SCRIPT_DIR / "fetched_repo2"
TRACE_JSONL   = STEP1_DIR  / "trace_logs.jsonl"
TRACE_TXT     = STEP1_DIR  / "trace_logs.txt"
REPORTS_DIR   = STEP2_DIR  / "reports"
VIOLATIONS    = REPORTS_DIR / "violations.json"

MAX_REPO_SIZE_KB = 50_000  # ignore repos larger than ~50 MB


# ── Utilities ────────────────────────────────────────────────────────────────
def banner(msg):
    print(f"\n{'=' * 64}\n  {msg}\n{'=' * 64}")


def run_cmd(args, env=None):
    """Run a subprocess, printing the command line. Returns the CompletedProcess."""
    print(f"  $ {' '.join(str(a) for a in args)}")
    return subprocess.run(args, env=env)


def ensure_dotnet_on_path():
    """Add the dotnet SDK to PATH if it is not already reachable."""
    if shutil.which("dotnet"):
        return
    search = [
        os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\dotnet"),
        r"C:\Program Files\dotnet",
        r"C:\Program Files (x86)\dotnet",
    ]
    for d in search:
        if os.path.isfile(os.path.join(d, "dotnet.exe")):
            os.environ["PATH"] = d + os.pathsep + os.environ.get("PATH", "")
            print(f"  (added {d} to PATH)")
            return
    print("WARNING: dotnet SDK not found — build steps will fail.", file=sys.stderr)


# ── GitHub helpers ───────────────────────────────────────────────────────────
def github_get(path, token=None):
    url = ("https://api.github.com" + path) if path.startswith("/") else path
    req = urllib.request.Request(url, headers={
        "Accept": "application/vnd.github+json",
        "User-Agent": "AutoRepair-Pipeline",
    })
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.loads(resp.read())


def search_candidates(token=None, max_results=10):
    """Return a list of small C# repos that have open NullReferenceException issues."""
    q = urllib.parse.quote(
        "NullReferenceException is:issue state:open language:csharp"
    )
    data = github_get(
        f"/search/issues?q={q}&sort=created&order=desc&per_page=100", token
    )
    seen, candidates = set(), []
    for issue in data.get("items", []):
        repo_url = issue.get("repository_url", "")
        parts = repo_url.rstrip("/").split("/")
        if len(parts) < 2:
            continue
        key = f"{parts[-2]}/{parts[-1]}"
        if key in seen:
            continue
        seen.add(key)

        try:
            info = github_get(f"/repos/{key}", token)
        except Exception:
            continue

        if info.get("language") != "C#" or info.get("archived"):
            continue
        size_kb = info.get("size", 0)
        if size_kb > MAX_REPO_SIZE_KB:
            continue

        candidates.append({
            "full_name":  key,
            "size_kb":    size_kb,
            "clone_url":  info["clone_url"],
            "branch":     info.get("default_branch", "main"),
            "description": (info.get("description") or "")[:120],
            "issue_title": issue.get("title", "")[:120],
            "issue_url":   issue.get("html_url", ""),
        })
        if len(candidates) >= max_results:
            break

    candidates.sort(key=lambda c: c["size_kb"])
    return candidates


# ── Project discovery ────────────────────────────────────────────────────────
def find_projects(root: Path):
    """Return (source_csprojs, test_csprojs) found under *root*."""
    src, tests = [], []
    for p in root.rglob("*.csproj"):
        rel = p.relative_to(root).as_posix()
        if "/bin/" in rel or "/obj/" in rel:
            continue
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        is_test = (
            "<IsTestProject>true</IsTestProject>" in text
            or ".Tests" in p.stem
            or ".Test" in p.stem
            or "Test" == p.stem
            or p.stem.endswith("Tests")        # e.g. NFun.ApiTests
            or p.stem.endswith("Test")          # e.g. SomeTest
            # Has test framework package references
            or "MSTest" in text
            or "xunit" in text.lower()
            or "NUnit" in text
        )
        # Exclude test *helper* libraries (not runnable test projects)
        if is_test and "TestTools" in p.stem or "TestUtilities" in p.stem:
            is_test = "<IsTestProject>true</IsTestProject>" in text
        (tests if is_test else src).append(p)
    return src, tests


def detect_framework(csproj: Path) -> str:
    """Extract the first <TargetFramework> value from a .csproj."""
    text = csproj.read_text(encoding="utf-8-sig", errors="replace")
    m = re.search(r"<TargetFramework>([\w.]+)</TargetFramework>", text)
    if m:
        return m.group(1)
    m = re.search(r"<TargetFrameworks>([\w.;]+)</TargetFrameworks>", text)
    if m:
        return m.group(1).split(";")[0]
    return ""


def get_installed_runtime_major() -> str:
    """Return the highest installed .NET runtime major version (e.g. '10')."""
    result = subprocess.run(["dotnet", "--list-runtimes"],
                            capture_output=True, text=True)
    versions = set()
    for line in result.stdout.splitlines():
        m = re.match(r"Microsoft\.NETCore\.App\s+(\d+)", line)
        if m:
            versions.add(int(m.group(1)))
    return str(max(versions)) if versions else ""


def retarget_projects(root: Path, current_fw: str) -> str:
    """If the detected framework runtime isn't installed, retarget all
    .csproj and Directory.Build.props to the installed runtime.
    Returns the (possibly updated) framework string."""
    m = re.match(r"net(\d+)", current_fw)
    if not m:
        return current_fw
    fw_major = m.group(1)
    installed = get_installed_runtime_major()
    if not installed or fw_major == installed:
        return current_fw

    new_fw = f"net{installed}.0"
    print(f"  Runtime for {current_fw} not installed. Retargeting to {new_fw}...")

    compat_block = (
        '\n    <NoWarn>$(NoWarn);SYSLIB0011;SYSLIB0050;SYSLIB0051;SYSLIB0060'
        ';CS0618;CA2022</NoWarn>'
        '\n    <EnableUnsafeBinaryFormatterSerialization>true'
        '</EnableUnsafeBinaryFormatterSerialization>'
    )

    for p in root.rglob("*.csproj"):
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        updated = re.sub(
            r"<TargetFramework>net[\w.]+</TargetFramework>",
            f"<TargetFramework>{new_fw}</TargetFramework>",
            text,
        )
        updated = re.sub(
            r"<TargetFrameworks>[^<]+</TargetFrameworks>",
            f"<TargetFramework>{new_fw}</TargetFramework>",
            updated,
        )
        if updated != text:
            # Inject compat suppressions after the first TargetFramework
            if "EnableUnsafeBinaryFormatterSerialization" not in updated:
                updated = updated.replace(
                    f"<TargetFramework>{new_fw}</TargetFramework>",
                    f"<TargetFramework>{new_fw}</TargetFramework>{compat_block}",
                    1,
                )
            p.write_text(updated, encoding="utf-8")

    for p in root.rglob("Directory.Build.props"):
        text = p.read_text(encoding="utf-8-sig", errors="replace")
        updated = re.sub(
            r"<TargetFramework>net[\w.]+</TargetFramework>",
            f"<TargetFramework>{new_fw}</TargetFramework>",
            text,
        )
        updated = re.sub(
            r"<TargetFrameworks>[^<]+</TargetFrameworks>",
            f"<TargetFramework>{new_fw}</TargetFramework>",
            updated,
        )
        if updated != text:
            p.write_text(updated, encoding="utf-8")

    return new_fw


def match_test_to_source(test_proj: Path, src_projs: list[Path]) -> Path | None:
    """Find the source project that a test project references."""
    text = test_proj.read_text(encoding="utf-8-sig", errors="replace")
    for sp in src_projs:
        if sp.stem in text:
            return sp
    return src_projs[0] if src_projs else None


def strip_strong_naming(root: Path):
    """Remove signing config from .csproj / Directory.Build.props and strip
    public keys from InternalsVisibleTo attributes in .cs files."""
    patterns_to_remove = [
        r"<SignAssembly>.*?</SignAssembly>",
        r"<AssemblyOriginatorKeyFile>.*?</AssemblyOriginatorKeyFile>",
        r"<DelaySign>.*?</DelaySign>",
        r"<PublicSign>.*?</PublicSign>",
    ]
    for ext in ("*.csproj", "Directory.Build.props"):
        for p in root.rglob(ext):
            text = p.read_text(encoding="utf-8-sig", errors="replace")
            changed = text
            for pat in patterns_to_remove:
                changed = re.sub(pat, "", changed)
            if changed != text:
                p.write_text(changed, encoding="utf-8")
                print(f"    Stripped signing from {p.relative_to(root)}")
    # Strip public keys from InternalsVisibleTo so friend access still works
    for cs in root.rglob("*.cs"):
        rel = cs.relative_to(root).as_posix()
        if "/bin/" in rel or "/obj/" in rel:
            continue
        try:
            text = cs.read_text(encoding="utf-8-sig", errors="replace")
        except Exception:
            continue
        # [InternalsVisibleTo("Foo, PublicKey=0024...")] → [InternalsVisibleTo("Foo")]
        new_text = re.sub(
            r'(InternalsVisibleTo\(\s*"[^",]+),\s*PublicKey=[0-9a-fA-F]+\s*"',
            r'\1"',
            text,
        )
        if new_text != text:
            cs.write_text(new_text, encoding="utf-8")
            print(f"    Stripped PublicKey from InternalsVisibleTo in {cs.name}")


def rmtree_force(path: Path):
    """Remove a directory tree, handling read-only files on Windows."""
    def on_error(func, fpath, _exc_info):
        os.chmod(fpath, 0o777)
        func(fpath)
    shutil.rmtree(path, onerror=on_error)


# ── Restore (undo instrumentation) ──────────────────────────────────────────
def restore(source_dir: Path):
    """Put every .bak back and remove TraceLogger.cs — mirrors restore_project.ps1."""
    restored = 0
    for bak in source_dir.rglob("*.bak"):
        original = bak.with_suffix("")
        shutil.copy2(bak, original)
        bak.unlink()
        restored += 1
    tl = source_dir / "TraceLogger.cs"
    if tl.exists():
        tl.unlink()
    if restored:
        print(f"  Restored {restored} file(s).")


def fix_instrumentation_errors(source_dirs: list[Path], build_target: Path, framework: str) -> bool:
    """After instrumenting, try building. If semantic errors exist in
    instrumented files, restore those files from .bak and retry.
    Returns True if the build ultimately succeeds (or only non-fixable errors remain)."""
    MAX_ROUNDS = 8  # enough rounds for cascading dependency errors
    for attempt in range(MAX_ROUNDS):
        args = ["dotnet", "build", str(build_target), "--verbosity", "quiet"]
        # Don't pass -f to solution files
        if framework and build_target.suffix != ".sln":
            args += ["-f", framework]
        print(f"  Post-instrumentation build (attempt {attempt + 1})...")
        result = subprocess.run(args, capture_output=True, text=True)
        if result.returncode == 0:
            print("  Build OK.")
            return True

        # Parse error lines to find which source files are broken
        output = result.stdout + "\n" + result.stderr
        broken = set()
        has_lock_error = False
        for line in output.splitlines():
            # Skip file-lock errors (CS2012) — they aren't instrumentation problems
            if "CS2012" in line:
                has_lock_error = True
                continue
            m = re.match(r"\s*(.*?)\(\d+,\d+\):\s+error\s+CS\d+", line)
            if not m:
                continue
            err_file = Path(m.group(1).strip())
            if not err_file.is_absolute():
                continue
            # Only fix files inside one of the source directories that have backups
            in_source = False
            for sd in source_dirs:
                try:
                    err_file.relative_to(sd)
                    in_source = True
                    break
                except ValueError:
                    continue
            if not in_source:
                continue
            bak = Path(str(err_file) + ".bak")
            if bak.exists():
                broken.add(err_file)

        if not broken:
            if has_lock_error:
                print("  File lock detected (CS2012) — waiting 3s and retrying...")
                time.sleep(3)
                continue
            print("  Build errors are not in instrumented files — cannot auto-fix.")
            return False

        for bf in sorted(broken):
            bak = Path(str(bf) + ".bak")
            shutil.copy2(bak, bf)
            print(f"    Reverted: {bf.name}")

    print("  Max fix rounds reached.")
    return False


# ── Pipeline steps ───────────────────────────────────────────────────────────
def step_clone(clone_url: str) -> Path:
    banner("Cloning repository")
    if WORK_DIR.exists():
        rmtree_force(WORK_DIR)
    run_cmd(["git", "clone", "--depth", "1", clone_url, str(WORK_DIR)])
    return WORK_DIR


def step_build(target: Path, framework: str) -> bool:
    banner("Building project")
    args = ["dotnet", "build", str(target)]
    # Don't pass -f to solution files — they don't support it
    if framework and target.suffix != ".sln":
        args += ["-f", framework]
    r = run_cmd(args)
    if r.returncode != 0:
        print("  Build failed — trying restore + rebuild...")
        run_cmd(["dotnet", "restore", str(target)])
        r = run_cmd(args)
    return r.returncode == 0


def step1_instrument(source_dir: Path) -> bool:
    banner(f"Step 1a — Instrumenting source ({source_dir.name})")
    restore(source_dir)  # clean slate
    r = run_cmd(["dotnet", "run", "--project", str(INSTR_PROJ), "--", str(source_dir)])
    return r.returncode == 0


def step1_instrument_all(source_dirs: list[Path]) -> list[Path]:
    """Instrument every source directory. Returns the list that succeeded."""
    instrumented = []
    for sd in source_dirs:
        if step1_instrument(sd):
            instrumented.append(sd)
        else:
            print(f"  WARNING: instrumentation failed for {sd.name}, skipping it.")
            restore(sd)
    return instrumented


def step1_trace(test_csproj: Path, framework: str, append: bool = False) -> int:
    """Run the test suite so instrumented code writes to trace_logs.jsonl.
    If *append* is True, new events are appended to the existing file.
    Returns the number of trace events captured in this run."""
    banner(f"Step 1b — Running tests ({test_csproj.stem})")

    if not append:
        for f in (TRACE_JSONL, TRACE_TXT):
            if f.exists():
                f.unlink()
        TRACE_JSONL.touch()
        TRACE_TXT.touch()
    else:
        # Ensure file exists for append
        if not TRACE_JSONL.exists():
            TRACE_JSONL.touch()

    before = sum(1 for l in TRACE_JSONL.read_text(errors="replace").splitlines() if l.strip())

    env = os.environ.copy()
    env["TRACE_LOG_JSON_PATH"] = str(TRACE_JSONL)

    args = ["dotnet", "test", str(test_csproj), "--verbosity", "minimal"]
    if framework:
        args += ["-f", framework]
    run_cmd(args, env=env)

    after = sum(1 for l in TRACE_JSONL.read_text(errors="replace").splitlines() if l.strip())
    new_events = after - before
    print(f"  Trace events from {test_csproj.stem}: {new_events}  (total: {after})")
    return after


def step1_trace_all(test_projs: list[Path], framework: str) -> int:
    """Run all test projects, appending trace events to a single file.
    Returns total trace event count."""
    # Clean previous logs before the first run
    for f in (TRACE_JSONL, TRACE_TXT):
        if f.exists():
            f.unlink()
    TRACE_JSONL.touch()
    TRACE_TXT.touch()

    total = 0
    for i, tp in enumerate(test_projs):
        total = step1_trace(tp, framework, append=(i > 0))
    return total


def step2_analyze() -> list:
    banner("Step 2 — Constraint analysis")
    REPORTS_DIR.mkdir(parents=True, exist_ok=True)
    r = run_cmd([
        "dotnet", "run", "--project", str(CONSTR_PROJ),
        "--", "--trace", str(TRACE_JSONL), "--report", str(VIOLATIONS),
    ])
    if r.returncode != 0 or not VIOLATIONS.exists():
        print("  Constraint analyser failed or produced no output.")
        return []

    violations = json.loads(VIOLATIONS.read_text(errors="replace"))
    print(f"\n  Violations found: {len(violations)}")
    for v in violations:
        pred = v.get("predicate", "?")
        assertion = v.get("assertion", "")
        file = v.get("file", "?")
        line = v.get("line", "?")
        print(f"    [{file}:{line}] {pred} — {assertion}")
    return violations


# ── Main ─────────────────────────────────────────────────────────────────────
def try_candidate(candidate: dict) -> tuple[bool, int, list]:
    """Clone, build, instrument, trace and analyse *candidate*.

    Instruments ALL source directories and runs ALL test projects.
    Returns (success, trace_count, violations).
    """
    # ── Clone ────────────────────────────────────────────────────────────
    step_clone(candidate["clone_url"])

    # ── Discover project structure ───────────────────────────────────────
    src_projs, test_projs = find_projects(WORK_DIR)
    print(f"\n  Source projects : {[p.relative_to(WORK_DIR).as_posix() for p in src_projs]}")
    print(f"  Test projects  : {[p.relative_to(WORK_DIR).as_posix() for p in test_projs]}")

    if not src_projs:
        print("  Skipping — no source .csproj files found.")
        return False, 0, []
    if not test_projs:
        print("  Skipping — no test projects found (pipeline needs a test suite).")
        return False, 0, []

    # ── Collect all unique source directories ────────────────────────────
    source_dirs = list({p.parent for p in src_projs})
    source_dirs.sort(key=lambda d: d.as_posix())

    framework = detect_framework(test_projs[0])
    framework = retarget_projects(WORK_DIR, framework)

    print(f"\n  Source directories ({len(source_dirs)}):")
    for sd in source_dirs:
        cs_count = len(list(sd.rglob("*.cs")))
        print(f"    - {sd.relative_to(WORK_DIR).as_posix()}/  ({cs_count} .cs files)")
    print(f"  Test projects ({len(test_projs)}):")
    for tp in test_projs:
        print(f"    - {tp.relative_to(WORK_DIR).as_posix()}")
    print(f"  Target framework : {framework or '(default)'}")

    # ── Pre-build fixes ────────────────────────────────────────────────
    strip_strong_naming(WORK_DIR)

    # ── Build (verify the repo compiles before instrumenting) ────────────
    # Build the whole solution if available, otherwise each test project
    sln_files = list(WORK_DIR.rglob("*.sln"))
    build_target = sln_files[0] if sln_files else test_projs[0]
    if not step_build(build_target, framework):
        print("  Skipping — build failed.")
        return False, 0, []

    # ── Step 1 — Instrument ALL source dirs + Trace ALL test projects ────
    trace_count = 0
    instrumented_dirs = []
    try:
        instrumented_dirs = step1_instrument_all(source_dirs)
        if not instrumented_dirs:
            print("  Instrumentation failed for all source directories.", file=sys.stderr)
            return False, 0, []

        print(f"\n  Successfully instrumented {len(instrumented_dirs)}/{len(source_dirs)} directories.")

        # Build fix — use solution if available so all projects are checked
        fix_target = sln_files[0] if sln_files else test_projs[0]
        if not fix_instrumentation_errors(instrumented_dirs, fix_target, framework):
            print("  Could not fix instrumentation build errors.", file=sys.stderr)
            return False, 0, []

        trace_count = step1_trace_all(test_projs, framework)
    finally:
        for sd in instrumented_dirs:
            restore(sd)

    # ── Step 2 — Constraint analysis ─────────────────────────────────────
    violations = []
    if trace_count > 0:
        violations = step2_analyze()
    else:
        print("\n  No trace events collected — skipping analysis.")

    # Consider the pipeline successful only if we captured trace data.
    return trace_count > 0, trace_count, violations


def main():
    parser = argparse.ArgumentParser(
        description="Fetch a C# repo with a null-deref bug and run the AutoRepair pipeline."
    )
    parser.add_argument("--repo",  help="owner/repo to use directly (skips GitHub search)")
    parser.add_argument("--token", help="GitHub PAT (or set GITHUB_TOKEN env var)")
    args = parser.parse_args()

    token = args.token or os.environ.get("GITHUB_TOKEN")
    ensure_dotnet_on_path()

    # ── 1. Choose a repository ───────────────────────────────────────────
    if args.repo:
        banner(f"Using specified repo: {args.repo}")
        candidates = [{
            "full_name": args.repo,
            "clone_url": f"https://github.com/{args.repo}.git",
            "size_kb":   0,
            "branch":    "main",
        }]
    else:
        banner("Searching GitHub for C# repos with NullReferenceException issues")
        candidates = search_candidates(token)
        if not candidates:
            sys.exit("No suitable repositories found. Try --repo owner/repo instead.")

        print(f"  Found {len(candidates)} candidate(s):\n")
        for i, c in enumerate(candidates, 1):
            print(f"    {i}. {c['full_name']:40s}  {c['size_kb']:>6,} KB  {c['issue_title']}")

    # ── 2. Try candidates until one works ────────────────────────────────
    for idx, candidate in enumerate(candidates):
        print(f"\n  >>> Trying candidate {idx + 1}/{len(candidates)}: "
              f"{candidate['full_name']} ({candidate.get('size_kb', '?')} KB)")

        try:
            ok, trace_count, violations = try_candidate(candidate)
        except Exception as exc:
            print(f"  Error: {exc}")
            ok = False
        if ok:
            banner("Pipeline complete")
            print(f"  Repository   : {candidate['full_name']}")
            print(f"  Trace file   : {TRACE_JSONL}")
            print(f"  Trace events : {trace_count}")
            print(f"  Report       : {VIOLATIONS}")
            print(f"  Violations   : {len(violations)}")
            return

    sys.exit("None of the candidates could be processed successfully.")


if __name__ == "__main__":
    main()

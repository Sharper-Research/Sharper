"""
Run AutoRepair Step 1 (instrument + trace) across many small C# repositories.

Why this exists
---------------
`AutoRepair/run_all.ps1` is single-target: it instruments one source dir, runs
one test project, and writes outputs to shared paths that are overwritten
between runs.  PRHunter's `e2e_demo.py` does loop over repos but is bound to
GitHub-mined "failed PR" candidates and needs a populated `open_issues.csv`.

`run_multi_repos.py` fills the gap: take a curated list of small C# GitHub
repos, clone each at a pinned SHA, find its source + test projects, run
Step 1 against it in isolation, and copy each `trace_logs.jsonl` into a
per-repo workspace so results don't trample each other.

Usage
-----
    # Run the built-in curated list of small C# repos
    python AutoRepair/run_multi_repos.py

    # Run a custom list (one "owner/repo[@sha]" per line)
    python AutoRepair/run_multi_repos.py --repos-file my_repos.txt

    # Inline list
    python AutoRepair/run_multi_repos.py --repos owner/repoA,owner/repoB@abc1234

    # Stop after N successful traces (default: keep going through whole list)
    python AutoRepair/run_multi_repos.py --target-success 10

    # Rewrite unsupported <TargetFramework>s (e.g. net6.0) to a runtime that
    # is actually installed.  Without this, repos that target uninstalled
    # runtimes will fail at testhost launch even if the instrumenter is fine.
    python AutoRepair/run_multi_repos.py --retarget net8.0
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
from pathlib import Path
from typing import Optional

ROOT = Path(__file__).resolve().parent.parent
AUTOREPAIR_DIR = ROOT / "AutoRepair"
STEP1_SCRIPT = AUTOREPAIR_DIR / "Step1_Tracing" / "generate_trace.ps1"
RESTORE_SCRIPT = AUTOREPAIR_DIR / "Step1_Tracing" / "restore_project.ps1"
SHARED_TRACE = AUTOREPAIR_DIR / "Step1_Tracing" / "trace_logs.jsonl"

STEP2_PROJECT = AUTOREPAIR_DIR / "Step2_Constraints" / "Constraints" / "Constraints.csproj"
STEP3_PROJECT = AUTOREPAIR_DIR / "Step3_Repair" / "CFGAnalyzer" / "CFGAnalyzer.csproj"

WORKSPACE = AUTOREPAIR_DIR / "multi_repo_workspace"
RESULTS_DIR = WORKSPACE / "results"
SUMMARY_JSON = WORKSPACE / "summary.json"
SUMMARY_MD = WORKSPACE / "SUMMARY.md"


# Curated list of small C# repos with discoverable test projects.
# Each entry: "owner/repo[@sha]".  SHAs pin behaviour so the script stays
# reproducible even if upstream branches move.
DEFAULT_REPOS = [
    # Already-verified target from PRHunter case 01.
    "nodirmcsd/WordNinjaSharp@234a9be64172ba69edf6f125e90a02d2033172e8",
    # 10 small C# repos that have been confirmed to trace end-to-end with
    # this orchestrator (--retarget net8.0). See multi_repo_workspace/SUMMARY.md
    # for the canonical results table.
    "StephenCleary/Comparers",
    "StephenCleary/Deque",
    "StephenCleary/AsyncEx",
    "danielwertheim/ensure.that",
    "StefH/JsonConverter",
    "mganss/HtmlSanitizer",
    "Tyrrrz/Onova",
    "SimonCropp/Argon",
    "JonPSmith/EfCore.GenericServices",
    # Bonus candidates that exist on GitHub but didn't trace cleanly under
    # the current instrumenter (Markdig: rewriter conflict, CliWrap: tests
    # don't exercise instrumented paths, Hosting: no test project found).
    # Kept as fallbacks; orchestrator just records them as failed.
    "xoofx/Markdig",
    "Tyrrrz/CliWrap",
    "StephenCleary/Hosting",
]


# --------------------------------------------------------------------------
# Helpers
# --------------------------------------------------------------------------

def run(cmd, cwd: Optional[Path] = None, check: bool = True,
        capture: bool = True, timeout: Optional[int] = None) -> subprocess.CompletedProcess:
    print(f"  $ {' '.join(str(c) for c in cmd)}")
    r = subprocess.run(
        cmd, cwd=cwd,
        capture_output=capture, text=True,
        timeout=timeout,
    )
    if check and r.returncode != 0:
        if capture:
            sys.stdout.write(r.stdout[-1000:] or "")
            sys.stderr.write(r.stderr[-1000:] or "")
        raise RuntimeError(f"command failed (exit {r.returncode}): {cmd}")
    return r


def parse_repo_spec(spec: str) -> tuple[str, Optional[str]]:
    """`owner/repo[@sha]` → (owner/repo, sha or None)."""
    if "@" in spec:
        repo, sha = spec.split("@", 1)
        return repo.strip(), sha.strip()
    return spec.strip(), None


def clone(repo: str, sha: Optional[str], dest: Path) -> Optional[str]:
    """Shallow-clone `owner/repo` into `dest`. Returns the resolved SHA."""
    url = f"https://github.com/{repo}.git"
    if dest.exists():
        # Reuse a previous clone if present.
        try:
            r = run(["git", "rev-parse", "HEAD"], cwd=dest, check=True)
            return r.stdout.strip()
        except Exception:
            shutil.rmtree(dest, ignore_errors=True)

    dest.parent.mkdir(parents=True, exist_ok=True)
    if sha:
        # Fetch only the pinned SHA (depth 1 saves bandwidth on large repos).
        run(["git", "init", str(dest)])
        run(["git", "remote", "add", "origin", url], cwd=dest)
        # Try fetching the specific SHA first; some servers refuse this
        # (uploadpack.allowReachableSHA1InWant must be on), so fall back to
        # a shallow main fetch.
        r = run(["git", "fetch", "--depth", "1", "origin", sha], cwd=dest, check=False)
        if r.returncode != 0:
            run(["git", "fetch", "--depth", "100", "origin"], cwd=dest)
        r = run(["git", "checkout", sha], cwd=dest, check=False)
        if r.returncode != 0:
            run(["git", "fetch", "--unshallow", "origin"], cwd=dest, check=False)
            run(["git", "checkout", sha], cwd=dest)
    else:
        run(["git", "clone", "--depth", "1", url, str(dest)])

    r = run(["git", "rev-parse", "HEAD"], cwd=dest, check=True)
    return r.stdout.strip()


# --------------------------------------------------------------------------
# Project discovery
# --------------------------------------------------------------------------

TEST_PROJ_RE = re.compile(r"(test|tests|spec)\.csproj$", re.IGNORECASE)

# net6.0/net7.0 runtimes are commonly missing on dev boxes that only ship the
# net8/net10 SDKs.  When --retarget is given we rewrite these to the supplied
# TFM so the testhost can actually launch.  We deliberately skip net48 etc. —
# those need the full .NET Framework, which retargeting can't fix.
RETARGETABLE_TFMS = {"net5.0", "net6.0", "net7.0", "net9.0", "net11.0"}
TFM_SINGLE_RE = re.compile(r"<TargetFramework>([^<]+)</TargetFramework>")
TFM_MULTI_RE = re.compile(r"<TargetFrameworks>([^<]+)</TargetFrameworks>")
PROJECT_OPEN_RE = re.compile(r"<Project\b[^>]*>")

# Warnings that the instrumenter can produce when injecting trace calls into
# host code (mostly nullable-related). Repos with WarningsAsErrors=true would
# otherwise fail to build instrumented sources. Includes CA-prefixed analyzer
# codes that some repos (e.g. SimonCropp/Argon) escalate to errors via .NET
# analyzers (CA1802, CA1840).
INJECT_NOWARN = (
    "CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8625;CS0168;CS0219;CS8765;CS0114;"
    "CA1802;CA1805;CA1840;CA1860;CA2007;CA1822;CA1305;CA1310"
)

INJECT_PROPERTYGROUP = (
    "<PropertyGroup>"
    f"<NoWarn>$(NoWarn);{INJECT_NOWARN}</NoWarn>"
    "<TreatWarningsAsErrors>false</TreatWarningsAsErrors>"
    "<EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>"
    "<EnableNETAnalyzers>false</EnableNETAnalyzers>"
    "<RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>"
    "<AnalysisMode>None</AnalysisMode>"
    "<AnalysisLevel>none</AnalysisLevel>"
    "</PropertyGroup>"
)


def retarget_csprojs(repo_dir: Path, target_tfm: str) -> list[str]:
    """Rewrite every <TargetFramework[s]> in the repo to `target_tfm` if it
    references a runtime we can't run, and inject NoWarn + clear
    TreatWarningsAsErrors so the instrumented code builds even when the host
    project enables warnings-as-errors. Returns a list of edited files.

    We do this in csproj-space rather than passing /p:NoWarn=... to dotnet
    test because PowerShell mangles semicolon-separated /p: values, and
    MSBuild rejects comma-separated NoWarn values with MSB1006.
    """
    edited: list[str] = []
    for csproj in repo_dir.rglob("*.csproj"):
        try:
            text = csproj.read_text(encoding="utf-8")
        except Exception:
            continue
        original = text

        if target_tfm:
            def fix_single(m: re.Match) -> str:
                tfm = m.group(1).strip()
                if tfm in RETARGETABLE_TFMS:
                    return f"<TargetFramework>{target_tfm}</TargetFramework>"
                return m.group(0)

            def fix_multi(m: re.Match) -> str:
                tfms = [t.strip() for t in m.group(1).split(";") if t.strip()]
                if not any(t in RETARGETABLE_TFMS for t in tfms):
                    return m.group(0)
                new = []
                for t in tfms:
                    if t in RETARGETABLE_TFMS:
                        if target_tfm not in new:
                            new.append(target_tfm)
                    else:
                        if t not in new:
                            new.append(t)
                return f"<TargetFrameworks>{';'.join(new)}</TargetFrameworks>"

            text = TFM_SINGLE_RE.sub(fix_single, text)
            text = TFM_MULTI_RE.sub(fix_multi, text)

        # Inject NoWarn / TreatWarningsAsErrors=false right after the
        # <Project ...> opening tag so it overrides anything the host sets.
        if "<!-- AUTOREPAIR_INJECTED -->" not in text:
            m = PROJECT_OPEN_RE.search(text)
            if m:
                inject = "\n  <!-- AUTOREPAIR_INJECTED -->\n  " + INJECT_PROPERTYGROUP + "\n"
                text = text[:m.end()] + inject + text[m.end():]

        if text != original:
            csproj.write_text(text, encoding="utf-8")
            edited.append(str(csproj.relative_to(repo_dir)))
    return edited


def find_test_project(repo_dir: Path) -> Optional[Path]:
    """Pick the shallowest *Tests.csproj-style file in the repo."""
    candidates = [p for p in repo_dir.rglob("*.csproj")
                  if TEST_PROJ_RE.search(p.name)]
    # Skip vendored copies under /samples/, /examples/ — those rarely build.
    candidates = [p for p in candidates
                  if not any(seg in {"samples", "example", "examples"}
                             for seg in p.parts)]
    if not candidates:
        return None
    candidates.sort(key=lambda p: (len(p.parts), len(p.name)))
    return candidates[0]


def find_source_project(test_project: Path, repo_dir: Path) -> Optional[Path]:
    """
    From the test .csproj, follow the first <ProjectReference> to find the
    source project under test. Falls back to the largest non-test .csproj.
    """
    try:
        text = test_project.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        text = ""
    refs = re.findall(r'<ProjectReference\s+[^>]*Include\s*=\s*"([^"]+)"', text, re.IGNORECASE)
    for ref in refs:
        if "$(" in ref:
            continue
        candidate = (test_project.parent / ref).resolve()
        if candidate.exists():
            return candidate

    # Fallback: any non-test .csproj in the repo.
    others = [p for p in repo_dir.rglob("*.csproj")
              if p != test_project and not TEST_PROJ_RE.search(p.name)
              and not any(seg in {"samples", "example", "examples"} for seg in p.parts)]
    if not others:
        return None
    others.sort(key=lambda p: len(p.parts))
    return others[0]


# --------------------------------------------------------------------------
# Step 2 + Step 3 builders/runners
# --------------------------------------------------------------------------

_STEP_BUILDS_DONE = False


def ensure_step_projects_built() -> None:
    """Build Constraints + CFGAnalyzer once before the batch starts so the
    per-repo invocations can use --no-build."""
    global _STEP_BUILDS_DONE
    if _STEP_BUILDS_DONE:
        return
    for proj in (STEP2_PROJECT, STEP3_PROJECT):
        print(f"  [pre] dotnet build {proj.name}")
        sys.stdout.flush()
        r = subprocess.run(
            ["dotnet", "build", str(proj), "-v", "quiet"],
            capture_output=True, text=True, timeout=600,
        )
        if r.returncode != 0:
            sys.stdout.write(r.stdout[-1500:] or "")
            sys.stderr.write(r.stderr[-1500:] or "")
            raise RuntimeError(f"failed to pre-build {proj}")
    _STEP_BUILDS_DONE = True


def run_step2(case_dir: Path) -> dict:
    """Analyze the case's trace_logs.jsonl with the Constraints rule engine.
    Returns a dict to merge into the per-repo result."""
    trace_path = case_dir / "trace_logs.jsonl"
    report_path = case_dir / "violations.json"
    log_path = case_dir / "step2.log"

    if not trace_path.exists() or trace_path.stat().st_size == 0:
        return {"step2_status": "skipped-no-trace"}

    try:
        with log_path.open("w", encoding="utf-8") as log:
            proc = subprocess.run(
                ["dotnet", "run", "--project", str(STEP2_PROJECT), "--no-build", "--",
                 "--trace", str(trace_path),
                 "--report", str(report_path)],
                stdout=log, stderr=subprocess.STDOUT, text=True,
                timeout=300,
            )
    except subprocess.TimeoutExpired:
        return {"step2_status": "timeout"}
    except Exception as e:
        return {"step2_status": "error", "step2_error": str(e)}

    if proc.returncode != 0 or not report_path.exists():
        return {"step2_status": f"exit-{proc.returncode}"}

    try:
        violations = json.loads(report_path.read_text(encoding="utf-8"))
    except Exception:
        violations = []

    # Bucket violation predicates so the summary table is readable.
    by_pred: dict[str, int] = {}
    for v in violations:
        p = v.get("predicate", "?")
        by_pred[p] = by_pred.get(p, 0) + 1

    return {
        "step2_status": "ok",
        "violations_total": len(violations),
        "violations_by_predicate": by_pred,
        "violations_path": str(report_path.relative_to(ROOT)),
    }


def run_step3(case_dir: Path, src_csproj: Path) -> dict:
    """Run CFGAnalyzer driven by violations.json. Writes repair_plan.json
    and patches/ into case_dir. Returns a dict to merge into the result."""
    violations_path = case_dir / "violations.json"
    if not violations_path.exists():
        return {"step3_status": "skipped-no-violations"}

    log_path = case_dir / "step3.log"
    plan_path = case_dir / "repair_plan.json"
    patches_dir = case_dir / "patches"

    # Clear any patches from a prior run so the count reflects only this one.
    if patches_dir.exists():
        for p in patches_dir.glob("*"):
            try:
                p.unlink()
            except Exception:
                pass

    try:
        with log_path.open("w", encoding="utf-8") as log:
            proc = subprocess.run(
                ["dotnet", "run", "--project", str(STEP3_PROJECT), "--no-build", "--",
                 str(src_csproj),
                 "--violations", str(violations_path),
                 "--output-dir", str(case_dir)],
                stdout=log, stderr=subprocess.STDOUT, text=True,
                timeout=900,
            )
    except subprocess.TimeoutExpired:
        return {"step3_status": "timeout"}
    except Exception as e:
        return {"step3_status": "error", "step3_error": str(e)}

    result: dict = {"step3_status": "ok" if proc.returncode == 0 else f"exit-{proc.returncode}"}

    if plan_path.exists():
        try:
            plan = json.loads(plan_path.read_text(encoding="utf-8"))
            result["repair_plan_path"] = str(plan_path.relative_to(ROOT))
            result["repair_total_violations"] = plan.get("totalViolations", plan.get("TotalViolations", 0))
            by_action = plan.get("byAction", plan.get("ByAction", {}))
            if by_action:
                result["repair_by_action"] = by_action
        except Exception:
            pass

    if patches_dir.exists():
        patch_files = sorted(p.name for p in patches_dir.glob("*.diff"))
        result["patches_generated"] = len(patch_files)
        if patch_files:
            result["patch_files"] = patch_files
            result["patches_dir"] = str(patches_dir.relative_to(ROOT))

    return result


# --------------------------------------------------------------------------
# Per-repo runner
# --------------------------------------------------------------------------

def trace_one(spec: str, idx: int, retarget: Optional[str] = None,
              run_step_1: bool = True, run_step_2: bool = True,
              run_step_3: bool = True) -> dict:
    repo, sha = parse_repo_spec(spec)
    safe = repo.replace("/", "__")
    case_dir = WORKSPACE / "cases" / f"{idx:02d}_{safe}"
    case_dir.mkdir(parents=True, exist_ok=True)
    repo_dir = case_dir / "repo"

    print(f"\n=== [{idx:02d}] {repo} ===")
    result: dict = {"index": idx, "repo": repo, "requested_sha": sha,
                    "case_dir": str(case_dir.relative_to(ROOT))}

    # 1. Clone (or reuse).
    try:
        actual_sha = clone(repo, sha, repo_dir)
        result["sha"] = actual_sha
    except Exception as e:
        result["status"] = "clone-failed"
        result["error"] = str(e)
        return result

    # 1a. Neutralize any global.json so a pinned SDK version (e.g. 10.0.203)
    # doesn't reject the locally-installed SDK. We rename to .bak rather
    # than deleting so the change is auditable in the case dir.
    pinned = list(repo_dir.rglob("global.json"))
    for gj in pinned:
        try:
            gj.rename(gj.with_suffix(".json.autorepair-bak"))
        except Exception:
            pass
    if pinned:
        result["global_json_neutralized"] = [str(p.relative_to(repo_dir)) for p in pinned]
        print(f"    neutralized {len(pinned)} global.json file(s)")

    # 1b. Always inject NoWarn + TreatWarningsAsErrors=false; optionally
    # also retarget unsupported TFMs.
    edited = retarget_csprojs(repo_dir, retarget or "")
    if edited:
        result["csproj_edits"] = len(edited)
        if retarget:
            result["retargeted_to"] = retarget
        print(f"    csproj edits ({len(edited)}): NoWarn injected"
              f"{', TFM->' + retarget if retarget else ''}")

    # 2. Discover test + source projects.
    test_proj = find_test_project(repo_dir)
    if not test_proj:
        result["status"] = "no-test-project"
        return result
    src_proj = find_source_project(test_proj, repo_dir)
    if not src_proj:
        result["status"] = "no-source-project"
        result["test_project"] = str(test_proj.relative_to(repo_dir))
        return result

    src_dir = src_proj.parent
    result["test_project"] = str(test_proj.relative_to(repo_dir))
    result["source_project"] = str(src_proj.relative_to(repo_dir))
    print(f"    src   : {src_dir.relative_to(repo_dir)}")
    print(f"    tests : {test_proj.relative_to(repo_dir)}")

    # If we're skipping Step 1 entirely (typically for a re-run that just
    # wants Step 2/3 against an already-captured trace), short-circuit here.
    if not run_step_1:
        existing = case_dir / "trace_logs.jsonl"
        if existing.exists() and existing.stat().st_size > 0:
            try:
                line_count = sum(1 for _ in existing.open("r", encoding="utf-8", errors="ignore"))
            except Exception:
                line_count = 0
            result["status"] = "ok"
            result["trace_events"] = line_count
            result["trace_path"] = str(existing.relative_to(ROOT))
            result["step1_skipped"] = True
        else:
            result["status"] = "no-trace"
            result["step1_skipped"] = True
            return result

    # 3. Make sure the shared trace path is gone before this run, so if the
    #    instrumenter writes nothing we notice (rather than copying a stale
    #    file from the previous repo).
    if run_step_1 and SHARED_TRACE.exists():
        SHARED_TRACE.unlink()

    pwsh = shutil.which("pwsh") or shutil.which("powershell") or "powershell"

    if run_step_1:
        # 4. Invoke Step 1. Cap the timeout per repo so a hung test suite
        #    doesn't block the whole batch.
        log_path = case_dir / "step1.log"
        try:
            with log_path.open("w", encoding="utf-8") as log:
                proc = subprocess.run(
                    [pwsh, "-NoProfile", "-ExecutionPolicy", "Bypass",
                     "-File", str(STEP1_SCRIPT),
                     "-TargetSourceDir", str(src_dir),
                     "-TestProject", str(test_proj)],
                    stdout=log, stderr=subprocess.STDOUT, text=True,
                    timeout=1800,
                )
            result["step1_exit"] = proc.returncode
        except subprocess.TimeoutExpired:
            result["status"] = "step1-timeout"
            try:
                subprocess.run([pwsh, "-NoProfile", "-File", str(RESTORE_SCRIPT), str(src_dir)],
                               timeout=120, capture_output=True)
            except Exception:
                pass
            return result
        except Exception as e:
            result["status"] = "step1-error"
            result["error"] = str(e)
            return result

        # 5. Capture the trace.
        if SHARED_TRACE.exists() and SHARED_TRACE.stat().st_size > 0:
            dest = case_dir / "trace_logs.jsonl"
            shutil.copy2(SHARED_TRACE, dest)
            try:
                line_count = sum(1 for _ in dest.open("r", encoding="utf-8", errors="ignore"))
            except Exception:
                line_count = 0
            result["trace_events"] = line_count
            result["trace_path"] = str(dest.relative_to(ROOT))
            result["status"] = "ok" if line_count > 0 else "trace-empty"
        else:
            result["status"] = "no-trace"

        # 6. Always restore the cloned repo BEFORE running Step 3 so line
        # numbers in violations.json match the un-instrumented source.
        try:
            subprocess.run(
                [pwsh, "-NoProfile", "-ExecutionPolicy", "Bypass",
                 "-File", str(RESTORE_SCRIPT), str(src_dir)],
                timeout=120, capture_output=True,
            )
        except Exception:
            pass

    # 7. Step 2: derive constraint violations from the trace.
    if run_step_2 and result.get("status") == "ok":
        s2 = run_step2(case_dir)
        result.update(s2)
        v_total = s2.get("violations_total")
        if v_total is not None:
            print(f"    step2 -> {s2.get('step2_status')} ({v_total} violations)")
        else:
            print(f"    step2 -> {s2.get('step2_status')}")
        sys.stdout.flush()

    # 8. Step 3: CFG analysis + patch synthesis driven by violations.json.
    if run_step_3 and result.get("step2_status") == "ok":
        s3 = run_step3(case_dir, src_proj)
        result.update(s3)
        n_patches = s3.get("patches_generated", 0)
        print(f"    step3 -> {s3.get('step3_status')} ({n_patches} patches)")
        sys.stdout.flush()

    return result


# --------------------------------------------------------------------------
# Reporting
# --------------------------------------------------------------------------

def write_summary(results: list[dict]) -> None:
    SUMMARY_JSON.write_text(json.dumps(results, indent=2), encoding="utf-8")

    ok = [r for r in results if r.get("status") == "ok"]
    s2_ok = [r for r in results if r.get("step2_status") == "ok"]
    s3_ok = [r for r in results if r.get("step3_status") == "ok"]
    total_violations = sum(r.get("violations_total", 0) or 0 for r in results)
    total_patches = sum(r.get("patches_generated", 0) or 0 for r in results)

    lines = [
        "# AutoRepair multi-repo pipeline — summary",
        "",
        f"- repos attempted: **{len(results)}**",
        f"- Step 1 (trace) success: **{len(ok)}**",
        f"- Step 2 (constraints) success: **{len(s2_ok)}**",
        f"- Step 3 (CFG / patches) success: **{len(s3_ok)}**",
        f"- target met (>=10 traces): **{'YES' if len(ok) >= 10 else 'NO'}**",
        f"- total violations across all repos: **{total_violations}**",
        f"- total patches synthesised across all repos: **{total_patches}**",
        "",
        "| # | Repo | Step1 events | Step2 violations | Step3 patches | Test project |",
        "|---|---|---:|---:|---:|---|",
    ]
    for r in results:
        events = r.get("trace_events", "")
        v = r.get("violations_total")
        v_cell = "" if v is None else str(v)
        p = r.get("patches_generated")
        p_cell = "" if p is None else str(p)
        tp = r.get("test_project", "")
        lines.append(
            f"| {r['index']:02d} | `{r['repo']}` | {events} | {v_cell} | {p_cell} | `{tp}` |"
        )
    SUMMARY_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\nWrote {SUMMARY_JSON}")
    print(f"Wrote {SUMMARY_MD}")


# --------------------------------------------------------------------------
# CLI
# --------------------------------------------------------------------------

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--repos", help="comma-separated owner/repo[@sha] list")
    ap.add_argument("--repos-file", help="text file with one owner/repo[@sha] per line")
    ap.add_argument("--target-success", type=int, default=0,
                    help="stop early once N repos have traced successfully (0 = run all)")
    ap.add_argument("--keep-going", action="store_true",
                    help="do not exit-1 even if fewer than --target-success repos succeed")
    ap.add_argument("--retarget", default=None,
                    help="rewrite csproj <TargetFramework> values for runtimes not "
                         "installed locally (e.g. net6.0 -> net8.0). Pass an installed TFM.")
    ap.add_argument("--steps", default="1,2,3",
                    help="comma-separated subset of pipeline steps to run "
                         "(default: 1,2,3 — full pipeline). Use '1' for trace-only.")
    args = ap.parse_args()

    steps_to_run = {int(s) for s in args.steps.split(",") if s.strip()}
    run_step_1 = 1 in steps_to_run
    run_step_2 = 2 in steps_to_run
    run_step_3 = 3 in steps_to_run

    repos: list[str] = []
    if args.repos:
        repos.extend(p.strip() for p in args.repos.split(",") if p.strip())
    if args.repos_file:
        repos.extend(line.strip() for line in Path(args.repos_file).read_text().splitlines()
                     if line.strip() and not line.startswith("#"))
    if not repos:
        repos = list(DEFAULT_REPOS)

    WORKSPACE.mkdir(parents=True, exist_ok=True)
    RESULTS_DIR.mkdir(parents=True, exist_ok=True)

    # Tee stdout to a log file inside the workspace so external piping
    # (Out-File / SIGPIPE) can't truncate it. Each line is also written
    # to the original stdout for live viewing.
    log_path = WORKSPACE / "run.log"
    log_fp = log_path.open("w", encoding="utf-8")
    real_stdout = sys.stdout

    class Tee:
        def write(self, s):
            real_stdout.write(s)
            real_stdout.flush()
            log_fp.write(s)
            log_fp.flush()
        def flush(self):
            real_stdout.flush()
            log_fp.flush()
    sys.stdout = Tee()

    print(f"Processing {len(repos)} repos. Workspace: {WORKSPACE}")
    print(f"Target successes: {args.target_success or 'all'}")
    print(f"Steps to run: {sorted(steps_to_run)}")
    print(f"Log: {log_path}")

    if run_step_2 or run_step_3:
        ensure_step_projects_built()

    results: list[dict] = []
    successes = 0
    t0 = time.time()
    for i, spec in enumerate(repos):
        try:
            r = trace_one(spec, i, retarget=args.retarget,
                          run_step_1=run_step_1,
                          run_step_2=run_step_2, run_step_3=run_step_3)
        except Exception as e:
            r = {"index": i, "repo": spec, "status": "exception", "error": str(e)}
        results.append(r)
        if r.get("status") == "ok":
            successes += 1
        # Stream the partial summary after every repo so a crash mid-batch
        # still leaves something inspectable.
        write_summary(results)
        print(f"  -> status={r.get('status')}  events={r.get('trace_events','-')}  "
              f"successes={successes}/{args.target_success or len(repos)}  "
              f"elapsed={int(time.time()-t0)}s")
        sys.stdout.flush()
        if args.target_success and successes >= args.target_success:
            print(f"\nReached target of {args.target_success} successful traces — stopping.")
            break

    write_summary(results)

    if args.target_success and successes < args.target_success and not args.keep_going:
        print(f"\nFAIL: only {successes}/{args.target_success} repos traced successfully.")
        return 1
    print(f"\nDone. {successes}/{len(results)} repos traced successfully.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

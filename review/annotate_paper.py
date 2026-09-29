import argparse
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

import fitz


ROOT = Path(__file__).resolve().parent.parent
REVIEW = ROOT / "review"
AUTHOR = "Sharper Artifact Review"
ANALYSIS = "Sharper/Step3_Repair/CFGAnalyzer/Analysis/"
TESTS = "Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs"
REPAIR = "Sharper/Step3_Repair/CFGAnalyzer/Repair/"

COMMENTS = [
    (1, "Towards an Automated Verified Repair", "Title: distinguish model checking from repair correctness",
     "Suggested title: Sharper: Trace-Guided Repair with Model Checking and Test-Based Validation for C#. "
     "The implementation checks selected safety obligations in supported models and accepts patches by tests. "
     "It does not prove semantic correctness of every generated patch. Keep 'verified repair' only if the paper "
     "defines the exact property, verifies the emitted candidate, and provides that evidence.",
     [ANALYSIS + "PaperSourceAnalysis.cs", REPAIR + "CandidateValidator.cs"]),
    (1, "The violation is the condition", "Correct failure-predicate polarity",
     "Replace with: A failure predicate is true when the modeled fault condition holds. Its negation is a "
     "candidate safety condition for that fault, not a guarantee of normal execution. Table 1 already uses this "
     "failure polarity. State that bounds predicates assume a defined array length; null safety is a separate "
     "obligation. Do not equate avoiding one exception with preserving the method's contract.",
     [ANALYSIS + "PaperRuleEngine.cs", ANALYSIS + "PaperSourceAnalysis.cs"]),
    (1, "then verifies", "Replace the abstract's universal verification claim",
     "Suggested abstract: We present Sharper, a prototype C# repair tool built on Roslyn. From a failing test "
     "execution, Sharper records events and derives candidate fault predicates. For a supported source subset, "
     "it performs backward predicate propagation and symbolic checks of selected failure locations. An explicit "
     "model interface additionally checks supplied loop invariants and nonrecursive procedure bodies. The tool "
     "generates guard or catch-based candidates and accepts a candidate only after an isolated failing reproducer "
     "and regression suite pass with stable test identities and unchanged snapshotted inputs. We demonstrate "
     "acceptance on a synthetic bounds fixture and rejection of an overfitting candidate for an extracted "
     "dotnet/runtime regression. Model safety and test acceptance are reported separately; neither establishes "
     "general semantic correctness of C# repairs.",
     ["review/verified-run/acceptance/repair_plan.json", "review/verified-run/runtime-rejection/repair_plan.json"]),
    (2, "industrial codebase", "Replace unsupported industrial repair success",
     "Replace the paragraph claiming an industrial repair and a human-comparable patch with the actual "
     "evaluation: The synthetic bounds fixture specifies default-return behavior and improves from 2/4 to 4/4 "
     "passing tests. The extracted dotnet/runtime regression improves from 3/5 to 4/5 under a catch/default "
     "candidate, which the gate rejects because it violates the inherited true-result oracle. This is a "
     "detection-and-rejection case, not a verified upstream repair, industrial deployment, or human comparison. "
     "Report the analyzer test count from the attached TRX separately; unit tests are not independent repair cases.",
     ["review/fixtures/bounds/Fixture.Tests/FixtureTests.cs", "review/fixtures/runtime/RuntimeCode.Tests/MemberMethodResolverTests.cs",
      "review/verified-run/analyzer-suite.trx"]),
    (2, "Verified Program", "Figure 1: label outputs by their actual assurance",
     "Replace 'Verified Program' with 'Test-accepted candidate'. Replace 'Constraint-satisfying patch' with "
     "'Candidate patch'. Show model checking as a separate result attached to the supported source/model "
     "analysis, not as proof of the emitted patch. Add outcome branches for Counterexample, Incomplete, and "
     "Rejected. The failing test is an observed failure, not a formal proof of the entire specification.",
     [REPAIR + "ViolationDrivenRepair.cs", ANALYSIS + "PaperRuleEngine.cs"]),
    (2, "Currently", "State implementation and reproducibility scope",
     "Replace the unsupported cross-language extension claim with: The artifact targets C# and is validated "
     "on Windows x64 with .NET 10, Roslyn 5.3.0, Microsoft.Z3 4.12.2, PowerShell, and VSTest projects. "
     "No other .NET language frontend or platform evaluation is demonstrated. Add the reproduction command "
     "and report the exact artifact revision rather than implying language independence from Roslyn alone.",
     ["Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj", "review/reproduce.ps1"]),
    (3, "state of the program", "Qualify the trace-state claim",
     "Replace 'the state of the program at each statement' with 'selected observations emitted by the "
     "instrumenter'. Values may be stringified or unavailable; the artifact does not establish complete "
     "heap, thread, call-frame, or alias reconstruction. Distinguish repeated dynamic occurrences using "
     "event indices in the mathematical notation. A failure event need not be the last event when tests "
     "continue running. Use a disposable checkout; source instrumentation is not proved semantics-preserving.",
     ["Sharper/Step1_Tracing/Instrumenter/Program.cs", "Sharper/Step2_Constraints/Constraints/Runtime/StateTracker.cs"]),
    (3, "backward slice", "Do not claim a complete dynamic slicer",
     "Replace with: The supported backward engine follows paths reaching the selected failure, substitutes "
     "symbol-resolved local assignments, conjoins branch guards, and records contributing lines. This is a "
     "restricted relevance analysis, not a demonstrated general heap-aware or interprocedural dynamic slice. "
     "The active violation pipeline must not be conflated with the separate legacy call-graph traversal. "
     "Initialization is based on the selected source expression and violation kind; general reconstruction "
     "of the complete failure state from the trace is not established.",
     [ANALYSIS + "PaperRuleEngine.cs", ANALYSIS + "PaperSourceAnalysis.cs", REPAIR + "ViolationDrivenRepair.cs"]),
    (3, "Table 1", "Separate template availability from verifier support",
     "Retain the three failure templates as definitions, but specify their domains: one-dimensional arrays "
     "for the source bounds adapter; modeled references for null; and a reference-cast template whose "
     "compatible predicate is not discharged by the current forward solver. Remove the unqualified claim "
     "of collection verification. Arithmetic uses signed Int32 bitvectors for supported unchecked operations. "
     "Checked exceptional paths, general heap mutation, and runtime type compatibility return Incomplete; "
     "a syntactically constructed predicate is not a completed verification result.",
     [ANALYSIS + "PaperSourceAnalysis.cs", ANALYSIS + "PaperForwardVerifier.cs", TESTS]),
    (4, "root-cause candidate", "Correct Figure 2 Root and its surrounding definition",
     "For deterministic x := e, exact substitution gives eval(Phi'[e/x], sigma) = "
     "eval(Phi', sigma[x := eval(e, sigma)]). Therefore the printed false-before/true-after premises "
     "cannot identify that assignment when Phi is its exact preimage. Replace Root and R with an explicitly "
     "different observed-activation definition: A_Q(event) iff the statement is relevant, the observed "
     "transition is valid, Q is defined in both states, Q(before) is false, and Q(after) is true. Use the "
     "SAME fixed Q on both sides, not distinct propagated predicates. This is an observed activation "
     "candidate, not causality, uniqueness, or minimality. The implementation accepts supplied scalar state "
     "pairs; automatic extraction of these pairs from arbitrary traces is not implemented. The exact "
     "replacement formula and derivation are in PAPER_AMENDMENTS.md. Also repair the incomplete sentence "
     "beginning 'For each location'.",
     [ANALYSIS + "PaperEvaluation.cs", TESTS, "review/PAPER_AMENDMENTS.md"]),
    (4, "repair condition", "Define what the forward result actually verifies",
     "Replace with: For acyclic supported source, Sharper negates the propagated failure predicate and "
     "checks that assumption against the selected failure site. This checks source/model safety, not the "
     "generated patched program. Source loop/call analysis instead explores the supported graph from true "
     "and reports Counterexample, Incomplete, or exhaustive success. Explicit models may supply multiple "
     "obligations and inductive invariants. State the quantified inputs, selected obligations, and scope "
     "with every result. An infeasible assumption is not a proof of a useful repair.",
     [ANALYSIS + "PaperSourceAnalysis.cs", ANALYSIS + "PaperSourceModel.cs", ANALYSIS + "PaperForwardVerifier.cs"]),
    (5, "repeats the analysis", "Remove the unimplemented automatic refinement loop",
     "Replace the automatic root-cause/extra-condition iteration claim with: The explicit forward checker "
     "can expose a later supplied safety violation. Additional conditions are currently supplied by the "
     "model author and rechecked; automatic counterexample-guided repair refinement is future work. "
     "Document the 1000-state default exploration budget and five-second solver-query timeout. Feasible "
     "cutoffs and unknown effects yield Incomplete. For explicit invariants, check initiation and "
     "preservation; this establishes partial correctness of the model, not termination.",
     [ANALYSIS + "PaperForwardVerifier.cs", ANALYSIS + "PaperProgramVerifier.cs", TESTS]),
    (5, "Listing 1.1", "Mark the listing as conceptual, not an executed tool repair",
     "The listing allocates arrays, writes array elements, and contains two loops. The automatic source "
     "adapter does not implement general array allocation/write semantics, so this complete listing is "
     "not an end-to-end verified repair. Label it 'Motivating example (not automatically repaired by the "
     "current artifact)' or replace it with the reproducible bounds fixture. Cite the two-obligation "
     "model regression only for the narrower property it checks. Do not describe a manually constructed "
     "model as automatic C# extraction of this listing.",
     [ANALYSIS + "PaperSourceModel.cs", TESTS, "review/fixtures/bounds/Fixture.cs"]),
    (5, "For each root-cause candidate", "Describe the actual candidate-generation input",
     "Replace with: For each eligible violation location, Sharper produces a source-level candidate. "
     "Supported direct array returns use a null/bounds guard and a default return; other eligible "
     "statements may use a catch/default template. Root candidates are not automatically feeding this "
     "pipeline. Default-return behavior requires an API-specific oracle and can be wrong. A candidate "
     "is not applied merely because it removes the exception. Validation requires baseline failures, "
     "passing candidate reproducer/regression runs, stable test identities, unchanged snapshotted test "
     "and project inputs, and source/candidate hashes. Only one accepted candidate may be applied; "
     "independently accepted candidates are not automatically composed.",
     [REPAIR + "ViolationDrivenRepair.cs", REPAIR + "PatchSynthesizer.cs", REPAIR + "CandidateValidator.cs"]),
    (6, "Figure 3 instantiates", "Separate the valid example argument from implementation evidence",
     "The explanation that the first guard leaves i = 5 and allows buf2[5] is valid under i = 0 and "
     "check > 5. Retain it as motivation for multiple obligations. Replace 'instantiates the analysis' "
     "with 'illustrates why a local guard need not be a whole-program repair'. The current model "
     "regression checks a later buffer obligation under a weaker versus stronger assumption; it does "
     "not execute this full listing or synthesize a complete second-loop repair. If arbitrary initial "
     "i is discussed, include the lower bound as well. Do not report a repair success for this example.",
     [TESTS, "review/PAPER_AMENDMENTS.md"]),
    (6, "In contrast", "Narrow the related-work comparison to demonstrated behavior",
     "Replace the claims of generator independence, patched-program verification, complete dynamic "
     "slicing, and path witnesses with: Sharper combines trace-derived violation candidates, restricted "
     "predicate propagation, supported model checks, and isolated test validation in a C# prototype. "
     "Z3 may return a model at a violating node; this is not a replayable concrete path witness, and "
     "invariant abstraction can produce unreachable counterexamples. The artifact does not establish "
     "comparative superiority or a general external patch-generator interface. Preserve related-work "
     "citations as context, not as evidence that Sharper implements those systems' capabilities.",
     [ANALYSIS + "PaperForwardVerifier.cs", REPAIR + "ViolationDrivenRepair.cs"]),
    (6, "checks every patched program", "Replace the conclusion's universal guarantee",
     "Suggested conclusion: Sharper is an open-source research prototype for trace-guided C# repair "
     "candidates. It combines restricted predicate propagation and safety checks with an isolated "
     "reproducer/regression acceptance gate. The artifact demonstrates a test-accepted synthetic "
     "bounds repair and rejection of an overfitting candidate for an extracted runtime regression. "
     "These results support feasibility within the documented subset, not universal semantic "
     "correctness. Unsupported effects and incomplete exploration are reported explicitly. Future "
     "work includes broader source semantics, automatic trace localization, invariant inference, "
     "and specification-based checking of emitted patches.",
     ["review/verified-run/acceptance/repair_plan.json", "review/verified-run/runtime-rejection/repair_plan.json"]),
    (6, "Planned work", "Update completed work and add threats to validity",
     "Regression-test checking is now implemented; do not list it solely as future work. State that "
     "it is evidence, not proof of intended behavior. Add threats: two small purpose-built fixtures; "
     "an incomplete test oracle may accept overfitting; stringified and incomplete trace observations; "
     "limited C# semantics; no broad current-version benchmark or human study. The copied validation "
     "workspace is filesystem isolation, not a security sandbox: dotnet test executes target code "
     "with the user's privileges. Unsupported test platforms, skipped/ambiguous results, and timeouts "
     "are rejected. The two-minute test limit and verified environment must be reported.",
     [REPAIR + "CandidateValidator.cs", "review/README.md"]),
    (6, "TODO", "Fill data availability and finish submission checks",
     "Replace the TODO with https://github.com/Manish-SDU/Sharper/tree/Sharper and cite the exact "
     "reviewed commit recorded in the final package manifest, not only a moving branch. Include "
     "review/reproduce.ps1, fixture inputs, source hashes, TRX files, model inputs, and expected "
     "acceptance/rejection results. The annotated PDF is an author revision copy, not a clean "
     "submission manuscript. Apply these changes to the editable manuscript, rebuild the PDF, "
     "check venue/anonymization requirements and author details, confirm distribution permissions "
     "and licensing, and have all authors approve the claims before submission. No paper has "
     "been submitted by this tool and publication acceptance is not implied.",
     ["review/reproduce.ps1", "review/package.ps1", "review/PAPER_AMENDMENTS.md"]),
]


def digest(path):
    data = path.read_bytes()
    if path.suffix.lower() != ".pdf":
        data = data.replace(b"\r\n", b"\n")  # Git may convert text line endings per checkout
    return hashlib.sha256(data).hexdigest()


def load_evidence(directory):
    tree = ET.parse(directory / "analyzer-suite.trx")
    counters = tree.find(".//{*}Counters")
    if counters is None or int(counters.get("passed", "0")) <= 0:
        raise ValueError("Missing analyzer test results")
    if counters.get("total") != counters.get("passed") or int(counters.get("failed", "0")):
        raise ValueError("Analyzer suite must pass without skipped tests")
    test_names = [result.get("testName", "") for result in tree.findall(".//{*}UnitTestResult")]
    if sum("SourceInitializerDoesNotHideLaterLoopFailures" in name for name in test_names) < 2:
        raise ValueError("Repeated-access regression evidence is missing")
    if not any("AutomaticAcceptanceRequiresStableReproducerAndRegressionResults" in name and "mutated-tests" in name
               for name in test_names):
        raise ValueError("Test-input mutation regression evidence is missing")
    for case, status in [("acceptance", "AcceptedByTests"), ("runtime-rejection", "Rejected")]:
        plan = json.loads((directory / case / "repair_plan.json").read_text(encoding="utf-8-sig"))
        if not plan["Items"] or any(item["Validation"]["Status"] != status for item in plan["Items"]):
            raise ValueError("Unexpected decision for " + case)
    return int(counters.get("passed"))


def verify_outputs(source, output, manifest_path):
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if digest(source) != manifest["original_pdf_sha256"] or digest(output) != manifest["annotated_pdf_sha256"]:
        raise ValueError("PDF hash mismatch")
    for name, expected in manifest["reviewed_files_sha256"].items():
        if digest(ROOT / name) != expected:
            raise ValueError("Review evidence changed: " + name)
    with fitz.open(source) as original, fitz.open(output) as annotated:
        if len(original) != len(annotated):
            raise ValueError("Page count changed")
        notes = []
        for page_number in range(len(original)):
            before = original[page_number]
            after = annotated[page_number]
            if before.get_text() != after.get_text():
                raise ValueError("Original page text changed")
            if before.read_contents() != after.read_contents():
                raise ValueError("Original page content stream changed")
            for annotation in list(after.annots() or []):
                if annotation.info.get("title") == AUTHOR:
                    notes.append((page_number + 1, annotation.info.get("content")))
                    after.delete_annot(annotation)
            after = annotated.reload_page(after)
            if before.get_pixmap(annots=False).samples != after.get_pixmap(annots=False).samples:
                raise ValueError("Underlying page rendering changed")
        expected = [(note["page"], note["content"]) for note in manifest["comments"]]
        if notes != expected:
            raise ValueError("Missing or altered PDF comments")
    print(f"Verified {len(notes)} anchored comments; all original pages and reviewed file hashes preserved.")


def main():
    parser = argparse.ArgumentParser(description="Create or validate the Sharper manuscript revision copy.")
    parser.add_argument("--evidence", type=Path, default=REVIEW / "verified-run")
    parser.add_argument("--check", action="store_true")
    arguments = parser.parse_args()
    source = REVIEW / "paper.pdf"
    output = REVIEW / "paper-annotated.pdf"
    manifest_path = REVIEW / "paper-review-manifest.json"
    if arguments.check:
        verify_outputs(source, output, manifest_path)
        return
    passed = load_evidence(arguments.evidence)
    original_hash = digest(source)
    records = []
    evidence_paths = {Path(__file__).resolve(), arguments.evidence / "analyzer-suite.trx"}
    evidence_paths.update(arguments.evidence.rglob("*.json"))
    evidence_paths.update(arguments.evidence.rglob("*.trx"))
    report = ["# Sharper Manuscript Revision Review", "",
              "Status: author revisions required before submission. The original PDF is unchanged; "
              "the annotated PDF contains proposed edits, not an accepted or submitted paper.", "",
              f"Current analyzer suite: **{passed}/{passed} passed**. Synthetic candidate: AcceptedByTests. "
              "Extracted runtime candidate: Rejected. These results are not a universal correctness proof.", "",
              "## Validated Corrections", "",
              "- Repeated loop-local int-array reads now preserve continuation and check later visits; "
              "the safe-first/unsafe-second regression returns Counterexample.",
              "- Validation rejects changes to snapshotted tests and project inputs even when test names remain unchanged.",
              "- The original Root formula still needs the explicitly different observed-activation amendment.", "",
              "## Required Manuscript Changes", ""]
    with fitz.open(source) as document:
        for number, (page_number, anchor, title, change, evidence) in enumerate(COMMENTS, 1):
            page = document[page_number - 1]
            quads = page.search_for(anchor, quads=True)
            if not quads:
                raise ValueError(f"Missing anchor on page {page_number}: {anchor}")
            identifier = f"SHARPER-{number:02d}"
            content = identifier + ": " + title + "\n\n" + change + "\n\nEvidence: " + "; ".join(evidence)
            annotation = page.add_highlight_annot(quads[0])
            annotation.set_info(title=AUTHOR, subject=identifier + ": " + title, content=content)
            annotation.set_colors(stroke=(1, 0.82, 0.15))
            annotation.set_opacity(0.30)
            annotation.update()
            records.append({"id": identifier, "page": page_number, "anchor": anchor, "title": title,
                            "content": content, "evidence": evidence})
            report.extend([f"### {identifier} | Page {page_number} | {title}", "", change, "",
                           "Evidence: " + ", ".join(f"[{path}](../{path})" for path in evidence), ""])
            evidence_paths.update(ROOT / path for path in evidence)
        document.save(output, garbage=4, deflate=True)
    if digest(source) != original_hash:
        raise ValueError("Original manuscript changed")
    report_path = REVIEW / "PAPER_REVIEW.md"
    report_path.write_text("\n".join(report), encoding="utf-8")
    evidence_paths.add(report_path)
    manifest = {"original_pdf_sha256": original_hash, "annotated_pdf_sha256": digest(output),
                "pymupdf_version": fitz.VersionBind, "analyzer_tests_passed": passed,
                "status": "Author revisions required; no submission or acceptance claimed",
                "reviewed_files_sha256": {path.relative_to(ROOT).as_posix(): digest(path)
                                          for path in sorted(evidence_paths)}, "comments": records}
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    verify_outputs(source, output, manifest_path)


if __name__ == "__main__":
    main()

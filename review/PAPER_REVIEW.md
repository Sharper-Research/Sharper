# Sharper Manuscript Revision Review

Status: author revisions required before submission. The original PDF is unchanged; the annotated PDF contains proposed edits, not an accepted or submitted paper.

Current analyzer suite: **98/98 passed**. Synthetic candidate: AcceptedByTests. Extracted runtime candidate: Rejected. These results are not a universal correctness proof.

## Validated Corrections

- Nested and top-level CFGs now retain their owning relationships. Call discovery includes branch values, does not treat delegate creation as invocation, and exports distinct nested-function IDs.
- Repeated loop-local int-array reads now preserve continuation and check later visits; the safe-first/unsafe-second regression returns Counterexample.
- Validation rejects changes to snapshotted tests and project inputs even when test names remain unchanged.
- The original Root formula still needs the explicitly different observed-activation amendment.

## Required Manuscript Changes

### SHARPER-01 | Page 1 | Title: distinguish model checking from repair correctness

Suggested title: Sharper: Trace-Guided Repair with Model Checking and Test-Based Validation for C#. The implementation checks selected safety obligations in supported models and accepts patches by tests. It does not prove semantic correctness of every generated patch. Keep 'verified repair' only if the paper defines the exact property, verifies the emitted candidate, and provides that evidence.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs), [Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs)

### SHARPER-02 | Page 1 | Correct failure-predicate polarity

Replace with: A failure predicate is true when the modeled fault condition holds. Its negation is a candidate safety condition for that fault, not a guarantee of normal execution. Table 1 already uses this failure polarity. State that bounds predicates assume a defined array length; null safety is a separate obligation. Do not equate avoiding one exception with preserving the method's contract.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs)

### SHARPER-03 | Page 1 | Replace the abstract's universal verification claim

Suggested abstract: We present Sharper, a prototype C# repair tool built on Roslyn. From a failing test execution, Sharper records events and derives candidate fault predicates. For a supported source subset, it performs backward predicate propagation and symbolic checks of selected failure locations. An explicit model interface additionally checks supplied loop invariants and nonrecursive procedure bodies. The tool generates guard or catch-based candidates and accepts a candidate only after an isolated failing reproducer and regression suite pass with stable test identities and unchanged snapshotted inputs. We demonstrate acceptance on a synthetic bounds fixture and rejection of an overfitting candidate for an extracted dotnet/runtime regression. Model safety and test acceptance are reported separately; neither establishes general semantic correctness of C# repairs.

Evidence: [review/verified-run/acceptance/repair_plan.json](../review/verified-run/acceptance/repair_plan.json), [review/verified-run/runtime-rejection/repair_plan.json](../review/verified-run/runtime-rejection/repair_plan.json)

### SHARPER-04 | Page 2 | Replace unsupported industrial repair success

Replace the paragraph claiming an industrial repair and a human-comparable patch with the actual evaluation: The synthetic bounds fixture specifies default-return behavior and improves from 2/4 to 4/4 passing tests. The extracted dotnet/runtime regression improves from 3/5 to 4/5 under a catch/default candidate, which the gate rejects because it violates the inherited true-result oracle. This is a detection-and-rejection case, not a verified upstream repair, industrial deployment, or human comparison. Report the analyzer test count from the attached TRX separately; unit tests are not independent repair cases.

Evidence: [review/fixtures/bounds/Fixture.Tests/FixtureTests.cs](../review/fixtures/bounds/Fixture.Tests/FixtureTests.cs), [review/fixtures/runtime/RuntimeCode.Tests/MemberMethodResolverTests.cs](../review/fixtures/runtime/RuntimeCode.Tests/MemberMethodResolverTests.cs), [review/verified-run/analyzer-suite.trx](../review/verified-run/analyzer-suite.trx)

### SHARPER-05 | Page 2 | Figure 1: label outputs by their actual assurance

Replace 'Verified Program' with 'Test-accepted candidate'. Replace 'Constraint-satisfying patch' with 'Candidate patch'. Show model checking as a separate result attached to the supported source/model analysis, not as proof of the emitted patch. Add outcome branches for Counterexample, Incomplete, and Rejected. The failing test is an observed failure, not a formal proof of the entire specification.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs)

### SHARPER-06 | Page 2 | State implementation and reproducibility scope

Replace the unsupported cross-language extension claim with: The artifact targets C# and is validated on Windows x64 with .NET 10, Roslyn 5.3.0, Microsoft.Z3 4.12.2, PowerShell, and VSTest projects. No other .NET language frontend or platform evaluation is demonstrated. Add the reproduction command and report the exact artifact revision rather than implying language independence from Roslyn alone.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.csproj), [review/reproduce.ps1](../review/reproduce.ps1)

### SHARPER-07 | Page 3 | Qualify the trace-state claim

Replace 'the state of the program at each statement' with 'selected observations emitted by the instrumenter'. Values may be stringified or unavailable; the artifact does not establish complete heap, thread, call-frame, or alias reconstruction. Distinguish repeated dynamic occurrences using event indices in the mathematical notation. A failure event need not be the last event when tests continue running. Use a disposable checkout; source instrumentation is not proved semantics-preserving.

Evidence: [Sharper/Step1_Tracing/Instrumenter/Program.cs](../Sharper/Step1_Tracing/Instrumenter/Program.cs), [Sharper/Step2_Constraints/Constraints/Runtime/StateTracker.cs](../Sharper/Step2_Constraints/Constraints/Runtime/StateTracker.cs)

### SHARPER-08 | Page 3 | Do not claim a complete dynamic slicer

Replace with: The supported backward engine follows paths reaching the selected failure, substitutes symbol-resolved local assignments, conjoins branch guards, and records contributing lines. This is a restricted relevance analysis, not a demonstrated general heap-aware or interprocedural dynamic slice. The active violation pipeline must not be conflated with the separate legacy call-graph traversal. Initialization is based on the selected source expression and violation kind; general reconstruction of the complete failure state from the trace is not established.

Use this CFG implementation paragraph: Sharper constructs Roslyn control-flow graphs for ordinary methods and top-level entry points. Local functions and lambdas are obtained recursively from their owning graphs. Call discovery visits block operations and branch values, including conditions and return expressions; creating a delegate does not itself invoke its body. Exported nested-function identities include their enclosing method and source position. These static may-call graphs are distinct from the restricted source/model graphs used for predicate propagation and safety checks. Collecting a lambda CFG does not establish support for lambda effects in the source verifier.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs), [Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/ProjectAnalyzer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/ProjectAnalyzer.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphBuilder.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphBuilder.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphJsonExporter.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphJsonExporter.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallResolvers/StaticCallResolver.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallResolvers/StaticCallResolver.cs), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/CallGraphSnapshotTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/CallGraphSnapshotTests.cs)

### SHARPER-09 | Page 3 | Separate template availability from verifier support

Retain the three failure templates as definitions, but specify their domains: one-dimensional arrays for the source bounds adapter; modeled references for null; and a reference-cast template whose compatible predicate is not discharged by the current forward solver. Remove the unqualified claim of collection verification. Arithmetic uses signed Int32 bitvectors for supported unchecked operations. Checked exceptional paths, general heap mutation, and runtime type compatibility return Incomplete; a syntactically constructed predicate is not a completed verification result.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs)

### SHARPER-10 | Page 4 | Correct Figure 2 Root and its surrounding definition

For deterministic x := e, exact substitution gives eval(Phi'[e/x], sigma) = eval(Phi', sigma[x := eval(e, sigma)]). Therefore the printed false-before/true-after premises cannot identify that assignment when Phi is its exact preimage. Replace Root and R with an explicitly different observed-activation definition: A_Q(event) iff the statement is relevant, the observed transition is valid, Q is defined in both states, Q(before) is false, and Q(after) is true. Use the SAME fixed Q on both sides, not distinct propagated predicates. This is an observed activation candidate, not causality, uniqueness, or minimality. The implementation accepts supplied scalar state pairs; automatic extraction of these pairs from arbitrary traces is not implemented. The exact replacement formula and derivation are in PAPER_AMENDMENTS.md. Also repair the incomplete sentence beginning 'For each location'.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs), [review/PAPER_AMENDMENTS.md](../review/PAPER_AMENDMENTS.md)

### SHARPER-11 | Page 4 | Define what the forward result actually verifies

Replace with: For acyclic supported source, Sharper negates the propagated failure predicate and checks that assumption against the selected failure site. This checks source/model safety, not the generated patched program. Source loop/call analysis instead explores the supported graph from true and reports Counterexample, Incomplete, or exhaustive success. Explicit models may supply multiple obligations and inductive invariants. State the quantified inputs, selected obligations, and scope with every result. An infeasible assumption is not a proof of a useful repair.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs)

### SHARPER-12 | Page 5 | Remove the unimplemented automatic refinement loop

Replace the automatic root-cause/extra-condition iteration claim with: The explicit forward checker can expose a later supplied safety violation. Additional conditions are currently supplied by the model author and rechecked; automatic counterexample-guided repair refinement is future work. Document the 1000-state default exploration budget and five-second solver-query timeout. Feasible cutoffs and unknown effects yield Incomplete. For explicit invariants, check initiation and preservation; this establishes partial correctness of the model, not termination.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs), [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperProgramVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperProgramVerifier.cs), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs)

### SHARPER-13 | Page 5 | Mark the listing as conceptual, not an executed tool repair

The listing allocates arrays, writes array elements, and contains two loops. The automatic source adapter does not implement general array allocation/write semantics, so this complete listing is not an end-to-end verified repair. Label it 'Motivating example (not automatically repaired by the current artifact)' or replace it with the reproducible bounds fixture. Cite the two-obligation model regression only for the narrower property it checks. Do not describe a manually constructed model as automatic C# extraction of this listing.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceModel.cs), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs), [review/fixtures/bounds/Fixture.cs](../review/fixtures/bounds/Fixture.cs)

### SHARPER-14 | Page 5 | Describe the actual candidate-generation input

Replace with: For each eligible violation location, Sharper produces a source-level candidate. Supported direct array returns use a null/bounds guard and a default return; other eligible statements may use a catch/default template. Root candidates are not automatically feeding this pipeline. Default-return behavior requires an API-specific oracle and can be wrong. A candidate is not applied merely because it removes the exception. Validation requires baseline failures, passing candidate reproducer/regression runs, stable test identities, unchanged snapshotted test and project inputs, and source/candidate hashes. Only one accepted candidate may be applied; independently accepted candidates are not automatically composed.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs), [Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs), [Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs)

### SHARPER-15 | Page 6 | Separate the valid example argument from implementation evidence

The explanation that the first guard leaves i = 5 and allows buf2[5] is valid under i = 0 and check > 5. Retain it as motivation for multiple obligations. Replace 'instantiates the analysis' with 'illustrates why a local guard need not be a whole-program repair'. The current model regression checks a later buffer obligation under a weaker versus stronger assumption; it does not execute this full listing or synthesize a complete second-loop repair. If arbitrary initial i is discussed, include the lower bound as well. Do not report a repair success for this example.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs), [review/PAPER_AMENDMENTS.md](../review/PAPER_AMENDMENTS.md)

### SHARPER-16 | Page 6 | Narrow the related-work comparison to demonstrated behavior

Replace the claims of generator independence, patched-program verification, complete dynamic slicing, and path witnesses with: Sharper combines trace-derived violation candidates, restricted predicate propagation, supported model checks, and isolated test validation in a C# prototype. Z3 may return a model at a violating node; this is not a replayable concrete path witness, and invariant abstraction can produce unreachable counterexamples. The artifact does not establish comparative superiority or a general external patch-generator interface. Preserve related-work citations as context, not as evidence that Sharper implements those systems' capabilities.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs), [Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs)

### SHARPER-17 | Page 6 | Replace the conclusion's universal guarantee

Suggested conclusion: Sharper is an open-source research prototype for trace-guided C# repair candidates. It combines restricted predicate propagation and safety checks with an isolated reproducer/regression acceptance gate. The artifact demonstrates a test-accepted synthetic bounds repair and rejection of an overfitting candidate for an extracted runtime regression. These results support feasibility within the documented subset, not universal semantic correctness. Unsupported effects and incomplete exploration are reported explicitly. Future work includes broader source semantics, automatic trace localization, invariant inference, and specification-based checking of emitted patches.

Evidence: [review/verified-run/acceptance/repair_plan.json](../review/verified-run/acceptance/repair_plan.json), [review/verified-run/runtime-rejection/repair_plan.json](../review/verified-run/runtime-rejection/repair_plan.json)

### SHARPER-18 | Page 6 | Insert the Evaluation and Case Studies section before Related Work

Use the complete paste-ready Evaluation section in PAPER_REVIEW.md, including its results table, two case studies, and threats to validity. Replace the planned-evaluation claim with:

We evaluate three questions: whether the emitted predicates agree with concrete execution on a boundary-input matrix; whether CFG construction and call discovery preserve the tested ownership and invocation relationships; and whether the candidate gate accepts a contract-supported guard while rejecting an exception-suppressing but behaviorally incorrect candidate. Across 240 input combinations, the emitted bounds predicate agrees with the mathematical oracle and compiled C# execution. Seven call-graph snapshots and three semantic CFG regressions pass. A synthetic bounds fixture improves from 2/4 to 4/4 passing tests and is AcceptedByTests. Fresh tracing of an extracted dotnet/runtime regression observes two null dereferences at one source location; its catch/default candidate improves from 3/5 to 4/5 but is Rejected because the inherited true-result test still fails. These are two purpose-built scenarios, not a representative repair benchmark.

Use this threats paragraph: The results demonstrate feasibility within a restricted C# subset. They do not establish a population repair rate, comparative superiority, instrumentation transparency, or correctness beyond the checked properties and test oracles. Unit tests, parameter combinations, and repeated fault observations are not independent repair cases. The runtime fixture is extracted, not a full upstream scan or successful upstream repair. The copied validation workspace is filesystem isolation, not a security sandbox; target code runs with the user's privileges. No human study or controlled performance evaluation is reported. Regression-test validation is implemented, while broader semantics and specification-based checking of emitted patches remain future work.

Evidence: [Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs), [review/README.md](../review/README.md), [review/reproduce.ps1](../review/reproduce.ps1), [Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs](../Sharper/Step3_Repair/CFGAnalyzer/CFGAnalyzer.Tests/PaperFormulaTests.cs), [review/verified-run/runtime-trace/violations.json](../review/verified-run/runtime-trace/violations.json), [review/verified-run/environment.json](../review/verified-run/environment.json)

### SHARPER-19 | Page 6 | Fill data availability and finish submission checks

Replace the TODO with https://github.com/Manish-SDU/Sharper/tree/Sharper and cite the exact reviewed commit recorded in the final package manifest, not only a moving branch. Include review/reproduce.ps1, fixture inputs, source hashes, TRX files, model inputs, and expected acceptance/rejection results. The annotated PDF is an author revision copy, not a clean submission manuscript. Apply these changes to the editable manuscript, rebuild the PDF, check venue/anonymization requirements and author details, confirm distribution permissions and licensing, and have all authors approve the claims before submission. No paper has been submitted by this tool and publication acceptance is not implied.

Evidence: [review/reproduce.ps1](../review/reproduce.ps1), [review/package.ps1](../review/package.ps1), [review/PAPER_AMENDMENTS.md](../review/PAPER_AMENDMENTS.md)

## Paste-Ready Evaluation Section

Author instruction: insert the following Evaluation section before Related Work. Replace any conflicting evaluation claims elsewhere. The section reports this artifact's measured results; it is not a broad benchmark study.

## Evaluation

### Questions and Protocol

We evaluate three research questions. RQ1 asks whether the emitted failure predicate agrees with a mathematical bounds oracle and concrete C# execution on a boundary-input matrix. RQ2 asks whether CFG construction and call discovery preserve the tested ownership and invocation relationships. RQ3 asks whether the test gate distinguishes a candidate consistent with the supplied contract from an exception-suppressing candidate that violates it.

The evaluation uses two repair scenarios: a synthetic array-access fixture with an explicit default-return contract, and an extracted regression derived from dotnet/runtime issue 120561. The latter is pinned to source commit `8d3c4013b7e7c6d603b08a1fbf563d51e2c2e00c`; it is not a full-repository experiment. The analyzer's unit and snapshot tests provide implementation checks and are reported separately from these scenarios. No random sampling or human participants are involved.

The recorded environment is Microsoft Windows NT 10.0.26200.0 (AMD64), .NET SDK 10.0.401, and PowerShell 5.1.26100.9549. The tool uses Roslyn 5.3.0 and Microsoft.Z3 4.12.2. Starting from unchanged fixture sources, the reproduction script builds the tool, executes the analyzer suite, checks the loop/call and activation models, instruments a disposable runtime-fixture copy, executes its tests, and detects violations from the resulting trace. It then evaluates candidates using isolated baseline-suite, baseline-reproducer, candidate-reproducer, and candidate-suite runs. Acceptance requires actual baseline failure, stable test identities, unchanged snapshotted test/project inputs, and nonempty, non-skipped passing candidate results. Each test invocation has a two-minute limit. The original fixture hashes are checked afterward. The recorded complete local run took 52.312 seconds, including build and validation; this single observation is not a controlled performance measurement.

### Results

| Evaluation unit | Observation | Interpretation |
| --- | --- | --- |
| Analyzer suite | 98/98 passed; no skips | Implementation regression evidence |
| Formula/execution matrix | 240/240 combinations agree | Correspondence for the tested source fragment and inputs |
| CFG tests | 7 snapshots and 3 semantic regressions pass | Tested ownership, call discovery, and export identity properties |
| Explicit loop/call model | VerifiedInductiveModel; 2 safety locations | Partial correctness of the supplied model |
| Fixed-predicate transition | ObservedActivation-v1; candidate true | Observed activation, not causal localization |
| Synthetic bounds scenario | Baseline 2/4; candidate 4/4; AcceptedByTests | Candidate satisfies this supplied test oracle |
| Extracted runtime scenario | Baseline 3/5; candidate 4/5; Rejected | Gate rejects the overfitting candidate |
| Fresh runtime tracing | 31 events; 2 fault observations at 1 site | Detection evidence, not two independent bugs |

For RQ1, the integration test compiles both the original C# fragment and the generated candidate. It checks the exact serialized predicate and safety-condition strings and compares predicate evaluation, the mathematical bounds oracle, the original program's exception behavior, and candidate return values. The matrix varies array length over 0 through 4, the next index over eight values including Int32 extrema and boundary values, both Boolean branch outcomes, and three original index values: 5 x 8 x 2 x 3 = 240 combinations. All comparisons agree. Null-array behavior is checked separately because the bounds predicate assumes that array length is defined. This is finite correspondence evidence, not proof for all C# programs.

For RQ2, seven sample-project snapshots are checked together with semantic regressions for top-level/local-function ownership, nested local/lambda bodies, and calls in conditions and returns. An unused delegate declaration contributes no invocation edge. Two lambdas have distinct exported node identifiers, and every exported edge endpoint refers to an existing node. These tests exposed and now guard against creating nested CFGs without their owning graph, omitting branch-value calls, counting delegate creation as invocation, and merging distinct lambdas during export. The static may-call graph remains separate from the restricted source/model verifier; collecting a body's CFG does not imply that all of its C# effects are supported by verification.

For RQ3, one scenario is accepted and one is rejected. We do not turn this result into a population repair-success rate: the cases were selected to exercise the gate's positive and negative behavior. The acceptance and rejection decisions are separate from the source/model verification status.

### Case Study 1: Contract-Supported Bounds Guard

The synthetic method selects an index by a Boolean branch and returns the selected array element. Its tests require ordinary reads to retain their values and invalid or null-array reads to return zero. Before repair, two valid-read tests pass while the invalid-read and null-array tests fail. Sharper propagates the selected bounds-failure predicate backward through both assignments and branch guards, joining alternatives with disjunction. The proposed entry condition is the negation of that predicate. The selected source/model check reports VerifiedAcyclicModel; its scope is the original supported source under that condition, not proof of the generated patch.

The generated candidate guards the direct array return with a null check and lower/upper bounds checks and returns the default value on failure. The isolated reproducer passes, and all four regression tests pass with unchanged test identities and snapshotted inputs. The gate reports AcceptedByTests. Returning zero is justified here by the explicitly supplied contract; it is not a generally valid repair for arbitrary array-access APIs. This case demonstrates candidate generation and contract-supported test acceptance, not recovery of an unknown specification.

### Case Study 2: Rejecting an Overfitting Runtime Candidate

The extracted runtime method discovers a ShouldSerialize-style method using reflection. Its BindingFlags.DeclaredOnly lookup omits inherited methods. For a derived instance whose applicable method is declared by its base type, the lookup returns null and the subsequent dereference fails. The unchanged baseline passes three of five tests. Disposable instrumentation preserves that 3/5 outcome and emits 31 trace events; detection reports two observations of NotNull(dereference.receiver) at line 13 in one source file. Both observations concern the same location, so they are not counted as separate defects.

Sharper generates a catch/default candidate. This suppresses the exception and satisfies the inherited-method test whose expected return is false. However, the inherited method in the strengthened oracle returns true; replacing that behavior with default(false) is incorrect. Consequently the candidate passes four of five tests and is rejected. Both plan entries describe observations of the same site and reference the same candidate file. The reflection-heavy source analysis reports Incomplete rather than claiming a proof. This case demonstrates why exception disappearance alone is insufficient: the gate preserves a failing behavioral oracle instead of applying an overfitting candidate. No successful upstream repair is claimed.

### Threats to Validity

The two small, purpose-built scenarios are insufficient to establish effectiveness across a population of repositories. The runtime case is an extracted regression, while the synthetic case supplies the desired fallback behavior explicitly. An incomplete oracle can still accept an incorrect patch. Instrumentation is not proved semantics-preserving; matching outcomes on this fixture establish only the observed comparison. Trace values can be incomplete or stringified, and the supported source/model semantics exclude general heap effects, recursive reasoning, concurrency, and other C# features. The supplied-invariant model result is partial correctness, not termination, and the observed-activation result is not causality. The original printed Root rule requires the separate formal amendment; it is not validated by these tests.

The copied validation workspace is filesystem isolation rather than a security sandbox; builds and tests run with the user's privileges. Unit tests, parameter combinations, trace events, and repeated fault observations are not independent repair cases. We report no human study, controlled runtime comparison, industrial deployment, or comparative superiority over other repair systems. The measured results support feasibility within the documented subset and identify broader source semantics, stronger specifications, and independent benchmark evaluation as future work.

### Artifact Availability

The artifact is available at https://github.com/Manish-SDU/Sharper/tree/Sharper. Results can be reproduced from the repository root with `./review/reproduce.ps1 -Output review/rerun`, using a fresh output directory. The package records its exact Git revision in REVIEW_MANIFEST.json and includes source, fixtures, model inputs, traces, TRX outcomes, candidate plans, and source hashes. The final paper should cite that immutable revision in addition to the branch URL.

# Sharper Manuscript — Independent Review and Defence Briefing

**Reviewer:** Sahal Abdullah (SDU Software Engineering, University of Southern Denmark)
**Reviewed artifact:** `MANISH_Review.pdf` (highlighted copy of *Sharper: Towards an Automated Verified Repair Tool for C#*)
**Reviewed implementation:** branch `Sharper`, commit `188faa394` — *Make corrected fixed-predicate Root canonical in code and paper comments*
**Date of review:** 2026-10-03
**Status:** Author revisions required before submission. No submission or acceptance is claimed.

This document is my own line-by-line review of the manuscript against the implementation and the
recorded evidence in `review/verified-run/`. It is written as meeting-defence material: for every
claim it states the plain-language meaning, the technical meaning, where the claim is implemented,
and what can and cannot be defended. It complements, and does not replace,
[PAPER_REVIEW.md](PAPER_REVIEW.md) and [PAPER_AMENDMENTS.md](PAPER_AMENDMENTS.md).

The existing [README.md](README.md) is deliberately left unchanged; its SHA-256 is recorded in
[paper-review-manifest.json](paper-review-manifest.json) and must stay stable for
`annotate_paper.py --check`.

---

## Contents

- [0. One-paragraph summary of the tool](#0-one-paragraph-summary-of-the-tool)
- [1. Section-by-section review](#1-section-by-section-review)
- [2. The seven highlighted spans](#2-the-seven-highlighted-spans)
- [3. Four substantive defects](#3-four-substantive-defects)
- [4. Measured numbers](#4-measured-numbers)
- [5. Anticipated questions and answers](#5-anticipated-questions-and-answers)
- [6. Priority fix list](#6-priority-fix-list)
- [7. Reviewer conclusion](#7-reviewer-conclusion)

---

## 0. One-paragraph summary of the tool

Sharper takes one failing C# test, records what actually happened during that execution, converts the
crash into a logical predicate, uses that predicate to select a source-level guard template,
model-checks the emitted candidate with Z3, and applies the candidate only if the original failing
reproducer passes and the full regression suite passes with unmodified test identities and unchanged
snapshotted inputs. The substantive contribution is the acceptance gate: the artifact contains a case
where a candidate removes the exception and improves the passing-test count, and the gate still
rejects it.

Terminology discipline for the meeting: say **candidate** and **accepted**, not **verified repair**.
Most of the defensible criticism of this manuscript follows from that one word.

---

## 1. Section-by-section review

### 1.1 Title and abstract — assurance level

**Manuscript text.** "Towards an Automated **Verified** Repair Tool"; "verifies it by propagating the
condition in static analyser. If the fix is found to be **correct**, then it patches the code."

**Plain language.** The paper promises a machine that proves its repairs are right. A proof means no
input anywhere can break the patched code. That is a much larger promise than the tool delivers.

**Technical.** "Verified" implies a discharged proof obligation against a stated specification over
the full program semantics. The implementation performs (a) symbolic safety checks of *selected*
obligations within a *restricted modelled subset* of C#, and (b) *test-based acceptance*. These are
distinct assurance levels and the manuscript conflates them.

**Implementation.** [PaperForwardVerifier.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperForwardVerifier.cs)
performs the solver check; [CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs)
performs the test gate. They are separate components producing separate statuses. The code is more
precise than the abstract.

**Risk: HIGH.** Corresponds to SHARPER-01 and SHARPER-03. Proposed title:
*Sharper: Trace-Guided Repair with Model Checking and Test-Based Validation for C#*.

**Defence.** "The title overpromises. We verify selected safety obligations in a supported subset and
accept by tests. The tool reports those as two separate statuses; the manuscript should too."

### 1.2 Abstract — failure-predicate polarity is inverted

**Manuscript text.** "The violation is the condition ... that, **if satisfied, would have resulted in
normal execution without failures**."

**Table 1.** `Phi_f^bounds(a,i) = (i < 0) OR (i >= len(a))` — true exactly when the fault occurs.

**Plain language.** The abstract says the formula means "everything is fine". The table says it means
"everything is broken". They are opposites.

**Technical.** Table 1 uses **failure polarity**. The abstract describes the **safety condition**,
which is the negation. The manuscript never states the negation step. Additionally, the negation of a
single failure predicate is a *necessary* condition for avoiding *that one exception class*; it is not
sufficient for "normal execution without failures".

**Implementation.** [PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs)
propagates in failure polarity; the plans emit `ProposedEntryCondition` as the negation
([acceptance/repair_plan.json](verified-run/acceptance/repair_plan.json)). The code performs the
negation; the manuscript omits it.

**Risk: MEDIUM-HIGH.** SHARPER-02. One-sentence fix; leaving it in costs credibility in a formal
section.

### 1.3 Introduction — the industrial repair claim

**Manuscript text.** "evaluate it on an **industrial codebase**. In the studied case, the prototype
**identified and repaired** the targeted defect and produced a modification **structurally comparable
to the patch developed independently by a human engineer**."

**Recorded evidence.** [runtime-rejection/repair_plan.json](verified-run/runtime-rejection/repair_plan.json):

```
"Status": "Rejected"
"Reason": "Candidate failed or skipped reproducer/regression tests, produced no tests, or did not build"
```

Baseline 3/5, candidate 4/5, **Rejected**. The defect was detected and the candidate refused. It was
not repaired.

**Risk: CRITICAL.** SHARPER-04. A reviewer who runs [reproduce.ps1](reproduce.ps1) observes the
opposite of this claim. That reads as misrepresentation rather than as imprecise wording. This is the
single highest-damage item in the manuscript.

**Defence.** "That paragraph refers to an earlier HtmlSanitizer experiment that is not a rerun on the
current version. The current artifact's runtime case is detection-and-rejection, not repair. The
paragraph is replaced by the measured results." Then reframe: a documented refusal is a stronger
result for this paper than another green test run, because refusing an overfitted patch is the
property the paper argues for.

### 1.4 Section 2 — cross-language extension claim

**Manuscript text.** "with the infrastructure to extend it various other languages from .NET".

**Technical.** Roslyn covers C# and VB, but patch synthesis is C#-syntax-bound:
[PatchSynthesizer.cs:51-52](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs#L51-L52)
constructs the guard as C# source text via `SyntaxFactory.ParseExpression`. No other frontend is
implemented or evaluated.

**Risk: MEDIUM.** SHARPER-06. Replace with the recorded environment: Windows x64, .NET SDK 10.0.401,
Roslyn 5.3.0, Microsoft.Z3 4.12.2, PowerShell 5.1 ([environment.json](verified-run/environment.json)).

### 1.5 Section 2.1 Record and Run

**Manuscript text.** `tau_{P,r} = <e_1,...,e_n>`, `e_i = (k_i, l_i, d_i)`; records "the state of the
program at each statement"; "the terminal failure event identifies `l_f`".

**Plain language.** The program is rewritten so that it writes a diary as it runs — which method was
entered, which branch was taken, which variable changed — and the last diary entry before the crash
identifies where it died.

**Technical.** Source-level Roslyn instrumentation
([Instrumenter/Program.cs](../Sharper/Step1_Tracing/Instrumenter/Program.cs)) producing a totally
ordered event sequence. Event kinds: method entry, branch and loop outcomes, assignments, array
indices, checked arithmetic, runtime types, failure.

**Three defects.**

1. **"State at each statement" is not what is recorded.** The reconstruction in
   [PaperEvaluation.cs:46-56](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs#L46-L56)
   accepts only `System.Int32`, `System.Boolean` and `null`; anything else is dropped via
   `state.Remove(name)`. No heap, aliasing or call-frame reconstruction is established. Correct
   phrasing: *selected observations emitted by the instrumenter*.
2. **The failure event need not be terminal.** When a test host continues after a reported exception,
   later events follow. The runtime case records two failure observations at one site
   ([runtime-trace/violations.json](verified-run/runtime-trace/violations.json)).
3. **The notation cannot express a loop, and the running example is a loop.** The manuscript writes
   `sigma_l`, the state at location `l`. In `ProcessBuffers`, line 9 is visited with `i = 0..5`, so one
   location has six distinct states and `sigma_l` is undefined. The implementation already resolves
   this by indexing on the **dynamic event number** `k`, using `sigma_k` and `sigma_(k+1)`
   (`EvaluateTraceActivation` in
   [PaperEvaluation.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs)). The
   manuscript notation lags the implementation.

**Risk: MEDIUM-HIGH**, mainly item 3, because it is a formal defect inside a formal section and it
bites exactly in the paper's own example. SHARPER-07.

### 1.6 Section 2.2 Constraint Generator

**Manuscript text.** Computes a backward slice `S(l_f)` of locations the failure is data- or
control-dependent on, "following classical program slicing [9]".

**Plain language.** Start at the crash and walk backwards asking which earlier lines could have caused
it; keep those and discard the rest.

**Technical.** Weiser slicing is whole-program, interprocedural and heap-aware. What is implemented is
a **restricted relevance analysis**: it follows paths reaching the selected failure, substitutes
symbol-resolved local assignments, conjoins branch guards and records contributing lines. No
heap-aware or interprocedural slicing is demonstrated in the active pipeline.

**Implementation.** [PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs)
and [PaperSourceAnalysis.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperSourceAnalysis.cs).

**Distinction that must be maintained in discussion.** The general Roslyn CFG and call-graph
machinery ([ProjectAnalyzer.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/ProjectAnalyzer.cs),
[CallGraphBuilder.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/CallGraphBuilder.cs)) is a separate
component from the restricted source/model verifier. Collecting a lambda's CFG does not establish that
the verifier supports lambda effects. Conflating the two is the easiest way to be caught overclaiming.

**Risk: MEDIUM-HIGH.** SHARPER-08.

### 1.7 Table 1 — template domains

| Kind | Predicate | Status in the implementation |
| --- | --- | --- |
| Array bounds | `(i < 0) OR (i >= len(a))` | Implemented; one-dimensional Int32 arrays only; assumes `len(a)` is defined, so nullness is a separate obligation |
| Null dereference | `v = null` | Implemented for modelled references |
| Invalid cast | `(v != null) AND NOT compatible(v, tau)` | Constructed but **not discharged**; `compatible` is not in the solver theory, so the result is `Incomplete` |

The manuscript's phrase "with limited support for selected C# collection types" is **not supported by
the artifact**. No collection-type support appears in the recorded evidence. Remove the claim or
demonstrate it. SHARPER-09.

**Defence.** "Three templates are defined. Bounds and null are discharged for the supported domain.
The cast predicate is constructed but returns `Incomplete`, because runtime type compatibility is not
in the solver theory. A syntactically constructed predicate is not a completed verification result."

### 1.8 Figure 2 — the inference rules

| Rule | Plain language | Technical content |
| --- | --- | --- |
| Initialize | At the crash, write the formula describing the crash | `template(l_f, sigma_f) = Phi_f`, instantiated from observed state |
| Assign | Moving backwards past `x := e`, replace `x` by `e` | `Phi'[e/x]`, capture-avoiding substitution; weakest precondition for a deterministic assignment |
| Assign-Irrel | If the formula never mentions `x`, the line is irrelevant | `x` not in `fv(Phi')`, predicate unchanged |
| Guard-T / Guard-F | We reached here only because the test was true, so conjoin it | `Phi := rho AND Phi'` (resp. `NOT rho AND Phi'`) — path-condition accumulation |
| Frame | The line touches nothing relevant, so skip it | `def(s) INTERSECT fv(Phi') = empty` and `guard(s) = empty` |
| Join | Two ways to reach the crash: failure if either path fails, so disjoin | `OR_i Psi_i` over `succ_S(l)` |

**Why Join is a disjunction** — a likely question. The predicate is in failure polarity and failure is
existential: the program is unsafe if *any* successor path can fail. Negating for the repair condition
turns the disjunction into a **conjunction of safety conditions over all paths**, i.e. demonic,
all-paths safety. That is the correct direction.

**Implementation.** [PaperRuleEngine.cs:169-175](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs#L169-L175)
is a single switch whose arms are literally named `Assign`, `Assign-Irrel`, `Guard-T`, `Guard-F` and
`Frame`, with `Join` at
[PaperRuleEngine.cs:182](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs#L182). This
one-to-one correspondence with Figure 2 is the strongest paper-to-code demonstration available and
should be shown directly.

**Two formal nits.** `init(l_f, sigma_f) ==> <l_f, Phi_f>` is ill-typed, because the left-hand side is
not a configuration `<l, Phi>`. And `guard(s)`, used as a side condition of Frame, is never formally
defined.

### 1.9 Section 2.3 Patch Generator

**Manuscript text.** "For each root-cause candidate ... The **predicate determines the guard**."

**Actual behaviour.** For each *eligible violation location* — not for each Root candidate — a fixed
template fires:

```
g_repair = g AND (a != null) AND (i >= 0) AND (i < a.Length)
```

evaluated under C# short-circuit order
([PatchSynthesizer.cs:51-52](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs#L51-L52)).
Direct array returns instead use an if/return-default template
([PatchSynthesizer.cs:83-86](../Sharper/Step3_Repair/CFGAnalyzer/Repair/PatchSynthesizer.cs#L83-L86)).

**Risk: HIGH.** The manuscript implies the guard is derived from the propagated predicate. It is a
template. See [defect 2](#32-defect-2--the-figure-3-guard-is-not-backward-derived).

### 1.10 Section 2.4 and Figure 3 — the running example

**Scenario.** `ProcessBuffers(check = 6, i = 0)`; `buf1` has length 5, `buf2` has length 2.

- Loop 1 writes `buf1[0..5]`, so it fails at `buf1[5]` (valid indices 0..4).
- Guarding only loop 1 with `i < 5` causes it to exit at `i = 5`.
- Loop 2's guard `i >= 0` then immediately attempts `buf2[5]`, and `buf2` has length 2.

**Plain language.** Fixing the first fault supplies the input that triggers the second. A locally
correct patch is not a globally correct patch. This is precisely why test-passing patches overfit, and
it is the manuscript's best argument.

**Emitted candidate** ([listing/patches/Listing.L9.diff](verified-run/listing/patches/Listing.L9.diff)):

```csharp
while ((i < check) && buf1 != null && i >= 0 && i < buf1.Length)
...
while ((i >= 0)   && buf2 != null && i >= 0 && i < buf2.Length)
```

Two counterexample stages, then `VerifiedExploredModel`, then 1/4 to 4/4 and `AcceptedByTests`
([listing/repair_plan.json](verified-run/listing/repair_plan.json)).

**Two anticipated objections.**

- *The second guard repeats `i >= 0`.* The template is uniform and no simplification pass is applied.
  The repetition is idempotent and semantically harmless; simplification is cosmetic future work.
- *The patch stops the loop early, which changes behaviour.* Correct, and it must be conceded. The
  candidate implements a **loop-stopping policy**. The tests check **normal completion**, not buffer
  contents. No claim is made that the intended functional specification was recovered. Arguing
  functional equivalence here would be false and is easily refuted.

---

## 2. The seven highlighted spans

These are the spans marked in `MANISH_Review.pdf`, with the response I consider defensible for each.

### H1 — page 3: "the state of the program at each statement"

Concede. These are selected observations emitted by the instrumenter: typed scalars (`Int32`,
`Boolean`, `null`), with other values stringified or dropped. No heap, alias or call-frame
reconstruction is claimed. Instrumentation is not proved semantics-preserving, so a disposable
checkout is used.
Evidence: [Instrumenter/Program.cs](../Sharper/Step1_Tracing/Instrumenter/Program.cs),
[PaperEvaluation.cs:46-56](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs#L46-L56).

### H2 — page 3: "Thus tau_{P,r} describes one observed execution, not all feasible executions of P"

This sentence is correct, and it contradicts the abstract and conclusion. Agree with it and use it:
one trace yields a failure *hypothesis*; generalisation comes only from the forward model check, and
only inside the supported subset. This is the cleanest internal justification for retitling.

### H3 — page 3: "backward slice ... following classical program slicing [9]"

Concede the scope. Weiser is cited for the concept; no general slicer is implemented. The engine is a
restricted relevance analysis — path-following, symbol-resolved local substitution, guard conjunction,
contributing-line recording. The legacy call-graph traversal is a separate component and must not be
conflated with the active pipeline.

### H4 — page 4: "For each location l'_f a transition boundary at which a condition necessary for the observed failure is established ... passed to repair generation"

Two separate problems.

1. The sentence has no main verb; it is grammatically incomplete. Minor, but conspicuous in a formal
   section.
2. The claim that these locations are passed to repair generation is **not true of the
   implementation**. The pipeline is violation-driven
   ([ViolationDrivenRepair.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/ViolationDrivenRepair.cs)),
   and every recorded plan reports `"RootStatus": "NotEvaluated"` —
   [listing/repair_plan.json](verified-run/listing/repair_plan.json),
   [acceptance/repair_plan.json](verified-run/acceptance/repair_plan.json).

Do not defend point 2. Root classifies an observed transition; it does not feed patch generation
today. Wiring Root into synthesis is future work, and the plans record the status honestly.

### H5 — page 5: "checks whether enforcing the condition introduces a violation at a later program location"

Name which of the three checks is meant.

1. **Acyclic supported source** — assume the negated predicate and check the selected failure site;
   result `VerifiedAcyclicModel`.
2. **Loops and calls** — explore the lowered graph from `true`; result `Counterexample`, `Incomplete`,
   or exhaustive success (`VerifiedExploredModel`).
3. **Every emitted candidate** — re-parsed from its saved bytes and checked independently, with the
   result bound to a SHA-256 digest (`CandidateModelSha256` in each plan).

All budgets are fail-closed: 1000-state exploration, 5-second query timeout, 16 backward iterations,
20000-node expression limit. Any cutoff returns `Incomplete`, never success. The hash binding is worth
stating explicitly, since it establishes that the bytes checked are the bytes shipped.

### H6 — page 5: "The repair candidate is patched and validated against the test-suite and the reproducer"

State the full gate. Acceptance requires all of: an actual baseline failure; the reproducer passing
after the patch; the full suite passing after the patch; identical test identities before and after;
unchanged hashes for snapshotted test and project inputs; and non-empty, non-skipped results. The
identity and hash conditions exist so that a candidate cannot "fix" a defect by weakening its own
oracle. Build failure, timeout, absent results or modified tests all reject. Two-minute limit per test
invocation. The status is `AcceptedByTests`, never "verified".
Evidence: [CandidateValidator.cs](../Sharper/Step3_Repair/CFGAnalyzer/Repair/CandidateValidator.cs).

Volunteer the limitation: the copied workspace is filesystem isolation, not a security sandbox;
builds and tests run with the user's privileges.

### H7 — page 6: "validates candidate patches against a failure condition across the patched program"

Correct the scope. What is checked is the selected failure and the lowered Int32 array accesses in the
supported source — not the whole program, not functional equivalence, not resource exhaustion. Two
further claims in the same passage also need narrowing: Z3 returns a **model at a violating node**,
which is not a replayable concrete path witness, and invariant abstraction can produce counterexamples
unreachable in the concrete program; and "independent of the patch generator" is not demonstrated,
because no external generator interface exists in the artifact. SHARPER-16.

---

## 3. Four substantive defects

### 3.1 Defect 1 — the printed Root rule is vacuous

This is the strongest available technical objection to the manuscript and must be raised by the
authors rather than by a reviewer.

**Figure 2, Root.** `l in S(l_f)`, `sigma_l -s_l-> sigma_l'`, **`sigma_l NOT|= Phi_l`**,
**`sigma_l' |= Phi_l'`** implies `l in R(l_f)`.

**Plain language.** The rule says the guilty line is the one where the bad condition flips from false
to true. But it checks *two different formulas* at the two points, and by the Assign rule those two
formulas are constructed so as to always agree. The flip can therefore never be observed, and the rule
selects nothing.

**Technical.** Assign gives `Phi_l = Phi_l'[e/x]`. The substitution lemma gives

```
[[ Phi'[e/x] ]]_sigma  =  [[ Phi' ]]_(sigma[x <- [[e]]_sigma])
```

For a deterministic assignment, `sigma_l'` *is* `sigma_l[x <- [[e]]_sigma_l]`. Hence

```
[[ Phi_l ]]_sigma_l  =  [[ Phi_l' ]]_sigma_l'
```

The premises "false before" and "true after" evaluate the same truth value, so their conjunction is a
contradiction and `R(l_f)` is empty at every assignment. The rule is unsatisfiable by construction.

**Worked instance.** Let `Q = (index >= 5)`, statement `index = index + 1`, observed `4 -> 5`.

- *Printed Root*: `Phi_l = Q[index+1/index] = (index >= 4)`. At `index = 4` this is already true, so
  the premise requiring false fails and the rule misses the activation.
- *Corrected Root*: `NOT (4 >= 5) AND (5 >= 5) = true`. The activation is identified.

**Correction, already canonical in the code** as `Root-ObservedActivation-v1`:

```
Root_Q(k)  iff  s_k in S
             AND ValidTransition(s_k, sigma_k, sigma_(k+1))
             AND Defined(Q, sigma_k) AND Defined(Q, sigma_(k+1))
             AND NOT [[Q]]_sigma_k AND [[Q]]_sigma_(k+1)
```

One fixed `Q`, evaluated at both states, indexed by dynamic event `k` — which also resolves the loop
ambiguity in section 1.5.

**Implementation.**
[PaperEvaluation.cs:69-74](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs#L69-L74):
`EvaluateRoot` passes `fixedPredicate` twice. The printed rule survives only as
`EvaluatePrintedRoot`, labelled `PrintedRoot-legacy` and marked `[Obsolete]`. Recorded outcome:
[activation.json](verified-run/activation.json), `IsCandidate: true`.

The implementation is also stricter than the manuscript: it returns `Unknown` on missing observations,
`InvalidTransition` when the post-state contradicts the statement semantics, and it rejects
transitions in which an unassigned variable changed
([PaperEvaluation.cs:176-185](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperEvaluation.cs#L176-L185)).

**Scope limit to state explicitly.** Root establishes an **observed value transition**. It does not
establish counterfactual causality, minimality, or a unique root cause. Calling it "root cause" while
meaning "first point at which a predicate flipped" is a semantic overclaim.

**Recommended delivery.** Present the two-line proof, state that the corrected rule is already
canonical in code under a versioned identifier with a regression test pinning the difference, and note
that only the typeset Figure 2 remains to be updated. Presented this way it demonstrates rigour;
defended rather than conceded, it is damaging.

### 3.2 Defect 2 — the Figure 3 guard is not backward-derived

**Objection.** Two pages establish backward propagation rules, and then the repair is produced by a
fixed template. What did the rules contribute?

**Why the objection holds.** Figure 3 labels the derived guard `i < check AND i < 5`. Applying Guard-T
to the failure predicate at `buf1[i]` with `rho = (i < check)` gives

```
Phi_loop = (i < check) AND ((i < 0) OR (i >= 5))
```

Negating for safety:

```
NOT Phi_loop = NOT (i < check) OR (0 <= i < 5)
```

which is a **disjunction**. Figure 3's label is a **conjunction**, so it is not derivable from
Figure 2. It is the template output `g AND a != null AND i >= 0 AND i < len(a)` with `len(buf1) = 5`
substituted and the lower bound elided — the same shape as the emitted patch
`while ((i < check) && buf1 != null && i >= 0 && i < buf1.Length)`. The figure presents template output
as derivation.

**Position to take.** Guard construction is a bounds template followed by forward,
counterexample-guided refinement, capped at eight edits. The backward machinery does three other
things: it builds the relevance set, instantiates the failure predicate, and supplies the proposed
entry condition. It does not synthesise the guard. The separately implemented cyclic backward
least-fixed-point analysis, with solver-proved convergence, does not make these guard edits
backward-derived. SHARPER-15.

**Framing that preserves the contribution.** The novelty is not the guard — `i < a.Length` is not a
research result. The novelty is the loop: generate, model-check, obtain a counterexample at a
*different* location, refine, re-check, then test-gate. Figure 3 exists to show that fixing the first
buffer is what exposes the second.

**Related anticipated question.** *Why include `i >= 0` when `i` starts at 0?* For the figure's fixed
initial state it is redundant, but `i` is a caller-supplied parameter and may be negative; the test
`NegativeStartStopsBeforeInvalidWrite` calls `ProcessBuffers(1, -1)`
([ListingTests.cs](fixtures/listing/Listing.Tests/ListingTests.cs)). For arbitrary initial `i` the
lower bound is necessary.

### 3.3 Defect 3 — "checks every patched program" overstates the result

**Manuscript text.** "checks **every** patched program statically for **any** remaining path to the
failure."

**Actual behaviour.** Every emitted candidate receives a model-check **attempt**. Supported shapes —
direct-return guards and loop guards — verify. Catch-based candidates return `Incomplete`, because
exception effects are outside the modelled semantics; see
[runtime-rejection/repair_plan.json](verified-run/runtime-rejection/repair_plan.json),
`"Status": "Incomplete"`, `"Reason": "No supported Table 1 template at this exact expression"`.

**Position.** An attempted check is not a completed proof, and each patch retains its own outcome.
`Incomplete` may still proceed to tests; a **known counterexample blocks acceptance outright**. That
last property is the safety-relevant one and should be stated first. SHARPER-17.

### 3.4 Defect 4 — artifact hygiene

1. **Data availability is `[TODO: insert link]`.** For a tool paper this is close to disqualifying at
   some venues. Replace with `https://github.com/Manish-SDU/Sharper/tree/Sharper` **and the exact
   reviewed commit**; a moving branch is not an artifact. SHARPER-19.
2. **Absolute author paths leak into shipped evidence.**
   [listing/repair_plan.json](verified-run/listing/repair_plan.json) contains
   `"ContainingFilePath": "C:\\Users\\manis\\Desktop\\Sharper\\review\\..."`. Under double-blind
   review this de-anonymises a co-author. Rewrite to repository-relative paths before submission.
3. **There is no Evaluation section.** A seven-page tool paper with no measured results and a `[TODO]`
   artifact link invites desk rejection. SHARPER-18 supplies a complete paste-ready section with
   research questions, a results table, three case studies and threats to validity.

---

## 4. Measured numbers

Taken from the recorded evidence. These must be quoted exactly; none may substitute for another.

| Quantity | Value | Correct interpretation |
| --- | --- | --- |
| Analyzer suite | 128/128, no skips | Implementation regression evidence. Not 128 repair cases. |
| Formula/execution matrix | 240/240 agree | 5 lengths x 8 next-index values x 2 branch outcomes x 3 index values. Emitted predicate agrees with the mathematical oracle and with compiled C#. Finite correspondence, not a proof. |
| CFG tests | 7 snapshots, 3 semantic regressions | Ownership and call-discovery properties |
| Explicit loop/call model | `VerifiedInductiveModel`, 2 safety locations | Partial correctness of the supplied model; not termination |
| Canonical Root | `Root-ObservedActivation-v1`, candidate true | Code, CLI and replacement comment agree |
| Synthetic bounds fixture | 2/4 to 4/4, `AcceptedByTests` | Contract explicitly supplied by the fixture |
| Extracted dotnet/runtime case | 3/5 to 4/5, **Rejected** | Detection and rejection, not repair. Pinned at `8d3c4013b7e7c6d603b08a1fbf563d51e2c2e00c` |
| Paper Listing 1.1 | 1/4 to 4/4, `AcceptedByTests`, `VerifiedExploredModel` | After two counterexample stages |
| Listing candidate, concrete runs | 56 boundary inputs complete | Executed, not only modelled |
| Fresh runtime trace | 31 events, 2 observations at 1 site | One defect observed twice. Never "two bugs". |
| Full reproduction | 78.352 s | Single local observation; not a benchmark |
| Budgets | 1000 states, 5 s/query, 16 backward iterations, 20000-node expressions, 8 guard edits, 2 min/test | All fail-closed to `Incomplete` |

**Anticipated trap.** *"How many bugs did you fix?"* — Three purpose-built scenarios: two accepted, one
deliberately rejected. No population repair rate is reported. Unit tests, parameter combinations and
repeated fault observations are not independent repair cases. The figures 128 and 240 must never be
allowed to drift into sounding like repair successes.

---

## 5. Anticipated questions and answers

**Is this verified or not?**
Two separate results that are never merged. Model safety: selected obligations discharged by Z3 within
a supported C# subset. Test acceptance: `AcceptedByTests` under the hardened gate. Neither implies
general semantic correctness, and the tool prints them as separate fields.

**What is new relative to GenProg, Angelix, concolic repair?**
Not patch generation; Angelix and CPR perform symbolic synthesis well beyond this prototype. The
contribution is on the validation side: a trace-derived failure condition re-checked against the
emitted patch bytes, plus a gate that rejects an overfitting candidate. No comparative superiority is
claimed, and no comparison experiment was run.

**Why reject a candidate that passes more tests?**
Because 4/5 is not correctness. The catch/default candidate suppresses the `NullReferenceException`
and satisfies the test expecting `false`, but the inherited method genuinely returns `true`.
Substituting `default(false)` for real behaviour is incorrect. Exception disappearance is not repair.

**Does the analysis scale?**
No scaling claim is made. Budgets are fail-closed and report `Incomplete` rather than a false success.
The single 78-second figure covers three small fixtures end to end and is not a performance
evaluation.

**Why only arrays and null?**
Those are the templates with discharged solver support. The modelled semantics are signed 32-bit
bitvectors for unchecked arithmetic plus fixed-size one-dimensional Int32 arrays with fresh
identities, exact lengths, zero initialisation and alias-visible stores. General heap mutation,
checked-overflow paths, recursion, concurrency and runtime type compatibility are out of scope and
return `Incomplete`.

**The patch changes behaviour: the loop stops early.**
Correct. It is a loop-stopping policy, and the oracle checks normal completion rather than buffer
contents. No claim is made that the intended functional specification was recovered; this is stated in
threats to validity.

**Could the gate still accept a wrong patch?**
Yes. An incomplete oracle can accept an incorrect patch; case 1 works only because the contract was
explicitly supplied. The gate removes common failure modes — tampered tests, stale candidate bytes,
skipped results — but does not eliminate overfitting.

**When a question cannot be answered precisely.**
"I do not want to overstate that; let me point to the exact evidence file rather than guess." In a
paper whose theme is avoiding overclaim, naming a limit is consistent with the argument.

---

## 6. Priority fix list

Ordered by damage prevented.

| # | Action | Rationale | Item |
| --- | --- | --- | --- |
| 1 | Remove the "industrial codebase ... identified and repaired ... human-comparable" paragraph | Contradicted by the artifact's own recorded result | SHARPER-04 |
| 2 | Replace Figure 2's Root with `Root-ObservedActivation-v1` | The printed rule is provably vacuous | SHARPER-10 |
| 3 | Supply the data-availability link and the exact commit | Tool paper without a resolvable artifact | SHARPER-19 |
| 4 | Insert the Evaluation section | No measured results currently appear | SHARPER-18 |
| 5 | Retitle; remove "verified repair" from abstract and conclusion | Central overclaim | 01, 03, 17 |
| 6 | Correct the polarity sentence in the abstract | Directly contradicts Table 1 | SHARPER-02 |
| 7 | State that the guard is a template plus forward refinement | Figure 3 presents template output as derivation | SHARPER-15 |
| 8 | Qualify the slice, trace-state, collection-type and multi-language claims | Four unsupported claims | 06, 07, 08, 09 |
| 9 | Repair the incomplete sentence on page 4; add event indices to the notation | Formal-section defects | 07, 10 |
| 10 | Relabel Figure 1 "Verified Program" as "Test-accepted candidate" | The figure asserts what the text cannot | SHARPER-05 |
| 11 | Remove `C:\Users\manis\...` paths from shipped JSON | Double-blind de-anonymisation; artifact polish | this review |

---

## 7. Reviewer conclusion

The implementation is in better condition than the manuscript. The code separates model safety from
test acceptance, binds each model-check result to the SHA-256 digest of the candidate bytes it
checked, refuses candidates whose test inputs changed, reports fail-closed `Incomplete` statuses
instead of optimistic successes, and already carries a corrected replacement for an inference rule
that the manuscript prints in an unsatisfiable form. Figure 2's rules appear one-to-one as named arms
in [PaperRuleEngine.cs](../Sharper/Step3_Repair/CFGAnalyzer/Analysis/PaperRuleEngine.cs).

The manuscript's problems are therefore mostly claim calibration rather than engineering: the title
and abstract promise verification the tool does not perform; the introduction asserts an industrial
repair that the artifact records as a rejection; Figure 3 presents template output as a derivation;
and Figure 2's Root rule cannot fire at an assignment. Each has a prepared correction in
[PAPER_REVIEW.md](PAPER_REVIEW.md) and [PAPER_AMENDMENTS.md](PAPER_AMENDMENTS.md).

My recommendation is to present the prototype together with the acceptance gate and the rejection
case as the contribution, to raise the Root correction before a reviewer does, and to adopt the
corrected wording throughout before submission. The highest-priority item is the introduction's
industrial repair claim, because it is checkable against the shipped artifact and currently
contradicted by it.

Reviewed by **Sahal Abdullah** — sahab@mmmi.sdu.dk — 2026-10-03.

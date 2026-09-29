# Required Paper Amendments

These are explicit corrections to the supplied manuscript, not a claim that its existing formulas are equivalent. The original PDF remains an input record; no editable manuscript source was supplied.

## Root: Implemented Replacement

This is the canonical operational definition in code and the exact replacement for Figure 2's Root rule. `PaperEvaluator.EvaluateRoot` and `--check-root` implement it under the identifier `Root-ObservedActivation-v1`. `EvaluateObservedActivation` and `--check-activation` are compatibility aliases, not alternative definitions.

For an exact deterministic assignment transfer, the printed Assign rule implies

$$
\llbracket \Phi'[e/x] \rrbracket_\sigma
= \llbracket \Phi' \rrbracket_{\sigma[x\gets\llbracket e\rrbracket_\sigma]}.
$$

Consequently the superseded printed Root premise, false before and true after under these two predicates, cannot identify that assignment. `EvaluatePrintedRoot` retains this only as a legacy diagnostic and labels its result `PrintedRoot-legacy`. The obsolete two-predicate overload forwards to that diagnostic; operational code uses the single-predicate overload.

Replace the rule and localization paragraph with: **Root denotes a slice-relevant observed activation candidate**, using one fixed, well-defined predicate $Q$. For dynamic event $k$, statement $s_k$, and observed states $\sigma_k,\sigma_{k+1}$, define

$$
\operatorname{Root}_Q(k) \iff
s_k\in S\;\land\;\operatorname{ValidTransition}(s_k,\sigma_k,\sigma_{k+1})
\;\land\;\operatorname{Defined}(Q,\sigma_k)\land\operatorname{Defined}(Q,\sigma_{k+1})
\;\land\;\neg\llbracket Q\rrbracket_{\sigma_k}\land\llbracket Q\rrbracket_{\sigma_{k+1}}.
$$

This differs intentionally from printed Root. It establishes an observed value transition, not counterfactual causality, minimality, or unique root cause. The implementation accepts supplied scalar pairs or automatically reconstructs them from type-tagged trace updates for a supplied assignment, location and fixed predicate. It isolates thread state and clears it at method/control boundaries and unrelated files. Missing or untyped values yield Unknown. This requires complete update logging and unambiguous variable names; it is not arbitrary-trace causal localization. The JSONL activation fixture is synthetic, distinct from the captured runtime trace.

For example, with $Q\equiv index\ge5$, `index = index + 1` and observed `4 -> 5`, the corrected Root result is true. The old substituted predicate is already true at 4, so the printed two-predicate test returns false. The regression suite checks this distinction, unchanged/true-to-true/true-to-false states, slice exclusion, invalid transitions, missing observations and both CLI names. The Assign rule is unchanged.

`Evaluated` means the candidate condition was evaluated, not proved causal. `Unknown` means required observations or semantics are unavailable; `InvalidTransition` means the observations contradict the supported statement semantics. Predicate propagation alone has no observed transition and therefore reports `RootStatus: NotEvaluated`, together with the canonical rule identifier and scope. The old `BlockedByPrintedRootDefinition` status has been removed. The violation-driven repair pipeline does not claim to have evaluated Root merely because it generated a patch.

## Cyclic Backward Equations

For the supported scalar graph, let $B_v$ be the selected failure predicate at its location and false elsewhere. Starting with $F_v^0=\bot$, iterate

$$
F_v^{n+1}=B_v\lor\bigvee_{(v,s,u)\in E}\operatorname{pre}_s(F_u^n).
$$

Assignments use substitution, guards use conjunction, and joins use disjunction. Sharper reports `PropagatedCyclicFixedPoint` only when Z3 proves $\bigvee_v(F_v^{n+1}\ne F_v^n)$ unsatisfiable over the declared modeled inputs. Starting from false and applying monotone predecessor equations establishes the least fixed point for reachable selected failures in the supported normal-transition semantics. It does not establish termination or every possible exception's absence. Source analysis allows 16 iterations; the direct API defaults to 64. Expressions exceeding 20000 nodes, unsupported transfers, and unresolved solver queries return `Incomplete`, with no completed entry predicate. Heap transfers are not generally supported by this backward algorithm.

Source analysis reports this result separately from forward array-safety exploration. In particular, a guarded first listing access can be backward-safe while a later access still produces a forward counterexample. Loop-guard edits remain bounds-template synthesis plus forward refinement; implementing cyclic analysis does not make those edits backward-derived.

## Loops and Calls

Replace unrestricted verification claims with safety of an explicit typed model. For each loop cutpoint, supply an invariant $I$. Z3 checks initiation and preservation; subsequent paths are checked from arbitrary states satisfying $I$. A false or non-preserved invariant is rejected. Every feasible cycle must be covered; otherwise the result is Incomplete. This establishes partial correctness of the model, not termination.

Procedure calls are expanded from explicit bodies. Parameters and locals are renamed per call, arguments are bound, callee obligations are checked, and the return value is assigned to the caller. Missing bodies, recursion, unknown effects and exploration limits return Incomplete; no unchecked callee contracts are assumed.

The model uses signed 32-bit bitvectors for unchecked arithmetic. It now models fixed nonnegative-size one-dimensional Int32 array allocations, exact lengths, fresh non-null identities, zero initialization, and alias-visible reads/writes using a symbolic array heap. General object-heap mutation, checked-overflow exceptional paths, concurrency, allocation exhaustion and full runtime type compatibility remain outside the verified model. Counterexamples after invariant abstraction may be unreachable in the concrete program.

The source adapter now automatically lowers supported structured `while`, `for`, and `do` loops, local assignments and increments, `break`/`continue`, and direct non-generic static calls with available synchronous bodies. Call parameters and locals have distinct identities. Unknown effects, recursion, static initialization effects, missing bodies, and unsupported expressions are rejected. The resulting graph is explored from all modeled inputs. `VerifiedExploredModel` is reported only after exhaustive feasible-path exploration; any reachable budget cutoff returns `Incomplete`, not bounded verification success. Supplied invariant models remain a separate mode. Neither mode claims unrestricted C# correspondence or automatic invariant inference.

For a selected local initializer reading an `int[]`, source exploration preserves continuation and checks later visits. Array writes are now eligible failure sites, and every lowered Int32 array read/write is checked before its heap effect. The original, first-guard-only and two-guard variants of Listing 1.1 are analyzed directly from C# and executed at `(check = 6, i = 0)`. The first two fail; the final variant completes. Dynamic-size allocations, array initializers, multidimensional arrays and general heap expressions are not silently approximated.

## Emitted Listing Candidate

For supported array writes at the start of while-loop bodies, a bounds template strengthens the loop guard with non-nullness and lower/upper index bounds. Forward counterexample locations guide additional edits, up to eight. This is implemented template refinement, not cyclic backward-precondition inference or automatic causal localization.

The separate synthesis rule is $g_{repair}=g\land a\ne null\land i\ge0\land i<len(a)$, evaluated with C# short-circuit conjunction in that order. It is a guard-construction rule, not a consequence of $\operatorname{Root}_Q$. Root classifies an observed transition; the template constructs a candidate; forward analysis checks candidate safety; tests check the supplied behavioral oracle. None of these steps implies the others.

The two-buffer fixture automatically produces a candidate guarded at both loops. The saved source is reparsed and checked, with `VerifiedExploredModel` reported only after exhaustive feasible-path exploration. The selected failure and lowered Int32 array accesses are checked; functional equivalence and allocation exhaustion are not. The result is bound to a SHA-256 candidate hash. A changed candidate or known candidate counterexample blocks test acceptance.

The candidate's stated policy stops a loop before an invalid write. Its normal-completion tests improve from 1/4 to 4/4; they do not observe the private buffer contents. Separate compiled execution covers 56 boundary inputs. This is an executed source-level safety and test-acceptance result, not a recovered functional specification.

Every emitted candidate now receives a separate model-check attempt bound to its saved bytes. Direct-return guards are checked from true, including nullness, and the bounds fixture reports `VerifiedAcyclicModel`. Catch-based candidates report `Incomplete` because their effects are unsupported. An attempted check is not necessarily a completed proof; each patch retains its own outcome. `Incomplete` may proceed to tests, while a known counterexample blocks acceptance.

## Formula Correspondence

| Manuscript element | Current correspondence | Verification evidence |
| --- | --- | --- |
| Table 1 bounds and null templates | Implemented with failure polarity; bounds assume defined length | `Figure2_RepairDriverPropagatesBothCSharpBranches`: 240 compiled-input comparisons |
| Reference-cast template | Constructed; runtime compatibility is not solver-discharged | Unsupported effects return Incomplete |
| Initialize, Assign, Assign-Irrel, Guard-T/F, Frame, Join | Implemented for the supported expression/statement domain | `PaperFormulaTests` transfer and branching regressions |
| Corrected Root | Canonical fixed-Q definition implemented by `EvaluateRoot` and `--check-root` | `RootRequiresFixedPredicateActivation`, `RootCommandAndCompatibilityAliasReportCanonicalDefinition`, and recorded activation outputs |
| Superseded printed Root | Legacy diagnostic only, explicitly labeled `PrintedRoot-legacy` | Exact-assignment equality above and `Figure2_LiteralRootEvaluatesBothPredicatesAndChecksTheTransition` |
| Root from typed trace updates | Same canonical definition, automatically reconstructed scalar pairs | `TraceActivationReconstructsTypedStatesWithoutCrossingBoundaries`; recorded `trace-activation.json` |
| Cyclic backward extension | Least fixed point only on solver-proved convergence | `CyclicBackwardPropagationRequiresSemanticConvergence`; source loop/call integration |
| Loop invariants and procedure bodies | Checked supplied invariants and nonrecursive bodies | Recorded `loop-and-call.json`: VerifiedInductiveModel |
| Forward patched-program safety | Attempted for every emitted candidate, proved only within supported semantics | Per-candidate status and SHA-256 in all three repair plans |

The code and replacement comment now use the same Root definition. The original PDF is preserved as an input record; its old typeset rule is superseded by the anchored replacement comment. Apply that replacement and the cyclic equations to the editable manuscript when preparing the clean submission PDF.

## Repair Acceptance

Use **AcceptedByTests**, not "verified repair," when the isolated candidate passes both the selected reproducer and regression suite. Acceptance requires actual baseline failures, identical test identities before/after, nonempty non-skipped passing candidate results, and source/candidate hashes. Snapshotted tests and project inputs must retain their hashes after baseline and candidate testing; unchanged test names do not excuse modified assertions. Build failures, timeouts, absent results and changed tests reject a candidate. Validation is currently VSTest-specific and has a two-minute limit per test invocation. Its copied workspace is not a security sandbox: target builds and tests execute with the user's privileges.

One accepted candidate may be applied after its hashes are rechecked. Independently accepted patches are not composed automatically. Passing tests can still overfit an incomplete specification.

## Evaluation Claims

Use the complete [paste-ready Evaluation section](PAPER_REVIEW.md#paste-ready-evaluation-section) for the protocol, research questions, results table, three case-study narratives, and threats to validity. Its generator checks the recorded analyzer results, CFG regressions, fresh runtime detections, listing refinement, candidate hash, and baseline/candidate test counts before producing the text.

The bounds-guard fixture is synthetic and explicitly specifies default-return behavior. The dotnet/runtime fixture is an extracted real-bug detection and rejection case, not a full-repository scan or successful upstream repair. The historical HtmlSanitizer experiment is not a current-version rerun. No human-participant study, universal repair guarantee, or quantitative comparison with other repair systems is established by this artifact.

For reviewers, these cases are suitable as worked examples and prototype-feasibility evidence. They are not sufficient to support broad repair effectiveness or superiority claims. A stronger effectiveness evaluation needs independent real defects with explicit selection criteria, meaningful output oracles, failure/unsupported denominators, comparison or ablation experiments, and repeatable cost measurements. Reviewer acceptance also depends on the venue and novelty; it cannot be guaranteed by an artifact check.
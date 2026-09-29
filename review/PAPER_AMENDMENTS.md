# Required Paper Amendments

These are explicit corrections to the supplied manuscript, not a claim that its existing formulas are equivalent. The original PDF remains an input record; no editable manuscript source was supplied.

## Root

For an exact deterministic assignment transfer, the printed Assign rule implies

$$
\llbracket \Phi'[e/x] \rrbracket_\sigma
= \llbracket \Phi' \rrbracket_{\sigma[x\gets\llbracket e\rrbracket_\sigma]}.
$$

Consequently the printed Root premise, false before and true after under these two predicates, cannot identify that assignment. Retain this as a regression counterexample, not a claimed localization success.

Replace the localization claim with **observed activation candidates** using one fixed, well-defined predicate $Q$:

$$
A_Q(\ell,\sigma,\sigma') \iff
\ell\in S\;\land\;\operatorname{ValidTransition}(s_\ell,\sigma,\sigma')
\;\land\;\operatorname{Defined}(Q,\sigma)\land\operatorname{Defined}(Q,\sigma')
\;\land\;\neg\llbracket Q\rrbracket_\sigma\land\llbracket Q\rrbracket_{\sigma'}.
$$

This differs intentionally from printed Root. It establishes an observed value transition, not counterfactual causality, minimality, or unique root cause. The implementation accepts supplied scalar state pairs and validates assignments/guards; missing observations yield Unknown. Automatic extraction of these states from general execution traces is not implemented.

## Loops and Calls

Replace unrestricted verification claims with safety of an explicit typed model. For each loop cutpoint, supply an invariant $I$. Z3 checks initiation and preservation; subsequent paths are checked from arbitrary states satisfying $I$. A false or non-preserved invariant is rejected. Every feasible cycle must be covered; otherwise the result is Incomplete. This establishes partial correctness of the model, not termination.

Procedure calls are expanded from explicit bodies. Parameters and locals are renamed per call, arguments are bound, callee obligations are checked, and the return value is assigned to the caller. Missing bodies, recursion, unknown effects and exploration limits return Incomplete; no unchecked callee contracts are assumed.

The model uses signed 32-bit bitvectors for unchecked arithmetic. It now models fixed nonnegative-size one-dimensional Int32 array allocations, exact lengths, fresh non-null identities, zero initialization, and alias-visible reads/writes using a symbolic array heap. General object-heap mutation, checked-overflow exceptional paths, concurrency, allocation exhaustion and full runtime type compatibility remain outside the verified model. Counterexamples after invariant abstraction may be unreachable in the concrete program.

The source adapter now automatically lowers supported structured `while`, `for`, and `do` loops, local assignments and increments, `break`/`continue`, and direct non-generic static calls with available synchronous bodies. Call parameters and locals have distinct identities. Unknown effects, recursion, static initialization effects, missing bodies, and unsupported expressions are rejected. The resulting graph is explored from all modeled inputs. `VerifiedExploredModel` is reported only after exhaustive feasible-path exploration; any reachable budget cutoff returns `Incomplete`, not bounded verification success. Supplied invariant models remain a separate mode. Neither mode claims unrestricted C# correspondence or automatic invariant inference.

For a selected local initializer reading an `int[]`, source exploration preserves continuation and checks later visits. Array writes are now eligible failure sites, and every lowered Int32 array read/write is checked before its heap effect. The original, first-guard-only and two-guard variants of Listing 1.1 are analyzed directly from C# and executed at `(check = 6, i = 0)`. The first two fail; the final variant completes. Dynamic-size allocations, array initializers, multidimensional arrays and general heap expressions are not silently approximated.

## Emitted Listing Candidate

For supported array writes at the start of while-loop bodies, a bounds template strengthens the loop guard with non-nullness and lower/upper index bounds. Forward counterexample locations guide additional edits, up to eight. This is implemented template refinement, not cyclic backward-precondition inference or automatic causal localization.

The two-buffer fixture automatically produces a candidate guarded at both loops. The saved source is reparsed and checked, with `VerifiedExploredModel` reported only after exhaustive feasible-path exploration. The selected failure and lowered Int32 array accesses are checked; functional equivalence and allocation exhaustion are not. The result is bound to a SHA-256 candidate hash. A changed candidate or known candidate counterexample blocks test acceptance.

The candidate's stated policy stops a loop before an invalid write. Its normal-completion tests improve from 1/4 to 4/4; they do not observe the private buffer contents. Separate compiled execution covers 56 boundary inputs. This is an executed source-level safety and test-acceptance result, not a recovered functional specification. Other repair paths must not inherit its emitted-candidate verification claim.

## Repair Acceptance

Use **AcceptedByTests**, not "verified repair," when the isolated candidate passes both the selected reproducer and regression suite. Acceptance requires actual baseline failures, identical test identities before/after, nonempty non-skipped passing candidate results, and source/candidate hashes. Snapshotted tests and project inputs must retain their hashes after baseline and candidate testing; unchanged test names do not excuse modified assertions. Build failures, timeouts, absent results and changed tests reject a candidate. Validation is currently VSTest-specific and has a two-minute limit per test invocation. Its copied workspace is not a security sandbox: target builds and tests execute with the user's privileges.

One accepted candidate may be applied after its hashes are rechecked. Independently accepted patches are not composed automatically. Passing tests can still overfit an incomplete specification.

## Evaluation Claims

Use the complete [paste-ready Evaluation section](PAPER_REVIEW.md#paste-ready-evaluation-section) for the protocol, research questions, results table, three case-study narratives, and threats to validity. Its generator checks the recorded analyzer results, CFG regressions, fresh runtime detections, listing refinement, candidate hash, and baseline/candidate test counts before producing the text.

The bounds-guard fixture is synthetic and explicitly specifies default-return behavior. The dotnet/runtime fixture is an extracted real-bug detection and rejection case, not a full-repository scan or successful upstream repair. The historical HtmlSanitizer experiment is not a current-version rerun. No human-participant study, universal repair guarantee, or quantitative comparison with other repair systems is established by this artifact.
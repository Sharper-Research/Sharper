# Tracing

The Roslyn instrumenter rewrites C# sources in place, preserving originals as `*.cs.bak`. Use a disposable checkout. It records execution events, not a complete or lossless model of C# state.

From the repository root:

```powershell
.\Sharper\Step1_Tracing\generate_trace.ps1
.\Sharper\Step1_Tracing\restore_project.ps1
```

The default target is `review/fixtures/runtime/RuntimeCode`; the driver is its sibling `RuntimeCode.Tests` project. Override both with `-TargetSourceDir` and `-TestProject` for another repository. Test failures are expected inputs to fault detection; build failures are not evidence of a program fault.

Events are written to `Sharper/Step1_Tracing/trace_logs.jsonl`. Restoration consumes only instrumentation backups (`*.cs.bak`), not repair backups (`*.sharper.bak`).

For the complete recorded evaluation, use [the review workflow](../../review/README.md). The tracing pipeline does not infer causality or guarantee preservation of all concurrency, aliasing, or exception behavior.

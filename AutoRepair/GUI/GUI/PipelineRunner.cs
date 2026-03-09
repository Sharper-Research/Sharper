using System.Diagnostics; // need this for Process and ProcessStartInfo
using System.IO;          // need this for File, Path, Directory, FileInfo

namespace AutoRepairGUI;

// simple record to bundle the result of the analysis phase
// Success tells us if it worked, PatchesJsonPath is where the patches file is (if any)
public record AnalysisResult(bool Success, string? PatchesJsonPath);

// this class is the engine that runs the whole 3-step pipeline
// it spawns powershell processes for each step and streams their output back to the gui
// two phase approach: first a dry run that generates patches, then an apply phase
public sealed class PipelineRunner
{
    private readonly string _autoRepairRoot;  // root folder of the AutoRepair directory
    private readonly string _sourceDir;       // the users project source folder
    private readonly string? _testProject;    // path to the test .csproj if we found one

    private Process? _activeProcess; // keeps track of the currently running child process so we can kill it

    private string? _violationsFile; // saved from analysis so we can reuse it during apply

    // event that fires whenever the child process writes output, gui subscribes to this
    public event EventHandler<string>? OutputReceived;
    // event that fires when a step changes status (running, success, failed)
    public event EventHandler<(int Step, StepStatus Status)>? StepChanged;

    // constructor just saves the paths, nothing fancy
    public PipelineRunner(string autoRepairRoot, string sourceDir, string? testProject)
    {
        _autoRepairRoot = autoRepairRoot;
        _sourceDir = sourceDir;
        _testProject = testProject;
    }

    // this is the main analysis method, runs all 3 steps in dry-run mode
    // doesnt actually change source files, just generates a patches.json for the user to review
    public async Task<AnalysisResult> RunAnalysisAsync(CancellationToken ct)
    {
        // step 1: instrument the code and run tests to generate a trace
        Emit("\n═══════════════════════════════════════════════════\n");
        Emit("   STEP 1 — Instrumentation & Trace Generation\n");
        Emit("═══════════════════════════════════════════════════\n\n");
        OnStep(1, StepStatus.Running); // tell the gui step 1 is running

        // before we start, clean up any leftover .bak files from previous runs
        var restoreScript = Path.Combine(_autoRepairRoot, "Step1_Tracing", "restore_project.ps1");
        if (File.Exists(restoreScript)) // only if the restore script exists
        {
            Emit("Restoring source directory from backups...\n");
            await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{_sourceDir}\"", ct);

            if (_testProject != null) // also restore the test project dir if we have one
            {
                var testDir = Path.GetDirectoryName(_testProject)!; // get the folder the test csproj is in
                Emit("Restoring test directory from backups...\n");
                await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{testDir}\"", ct);
            }
        }

        var step1Script = Path.Combine(_autoRepairRoot, "Step1_Tracing", "generate_trace.ps1"); // path to the tracing script
        var step1Args = BuildStep1Args(step1Script); // build the command line arguments

        int rc1 = await RunPowerShellAsync(step1Args, ct); // actually run it
        if (rc1 != 0) // non-zero exit code means some tests failed
        {
            Emit($"\n⚠  Step 1 finished with exit code {rc1} (test failures detected — this is expected).\n");
        }
        OnStep(1, StepStatus.Success); // mark step 1 as done

        // make sure the trace file was actually created and isnt empty
        var traceFile = Path.Combine(_autoRepairRoot, "Step1_Tracing", "trace_logs.jsonl");
        if (!File.Exists(traceFile) || new FileInfo(traceFile).Length == 0)
        {
            Emit("❌  No trace data produced. Cannot proceed.\n");
            OnStep(1, StepStatus.Failed);
            return new AnalysisResult(false, null); // bail out early
        }

        ct.ThrowIfCancellationRequested(); // check if user hit cancel between steps

        // step 2: analyze the trace to find constraint violations
        Emit("\n═══════════════════════════════════════════════════\n");
        Emit("   STEP 2 — Constraint Analysis\n");
        Emit("═══════════════════════════════════════════════════\n\n");
        OnStep(2, StepStatus.Running); // tell the gui step 2 is running

        var step2Script = Path.Combine(_autoRepairRoot, "Step2_Constraints", "analyze_trace.ps1");
        int rc2 = await RunPowerShellAsync($"-File \"{step2Script}\"", ct); // run the analysis
        if (rc2 != 0) // non-zero means something went wrong
        {
            Emit($"\n❌  Step 2 failed (exit code {rc2}).\n");
            OnStep(2, StepStatus.Failed);
            return new AnalysisResult(false, null);
        }
        OnStep(2, StepStatus.Success); // step 2 done

        // save the violations file path for later when we need to apply patches
        _violationsFile = Path.Combine(_autoRepairRoot, "Step2_Constraints", "reports", "violations.json");
        if (!File.Exists(_violationsFile)) // if no violations file was produced, cant do step 3
        {
            Emit("❌  No violations report produced. Cannot proceed to Step 3.\n");
            OnStep(2, StepStatus.Failed);
            return new AnalysisResult(false, null);
        }

        ct.ThrowIfCancellationRequested(); // check for cancellation again

        // step 3: generate patches based on the violations (dry run, no --apply flag)
        Emit("\n═══════════════════════════════════════════════════\n");
        Emit("   STEP 3 — Patch Generation (dry-run preview)\n");
        Emit("═══════════════════════════════════════════════════\n\n");
        OnStep(3, StepStatus.Running);

        var step3Script = Path.Combine(_autoRepairRoot, "Step3_Repair", "generate_patches.ps1");
        // no -Apply flag here so it just generates the patches without writing them
        var step3Args = $"-File \"{step3Script}\" -ViolationsPath \"{_violationsFile}\" -TargetSourceDir \"{_sourceDir}\"";

        int rc3 = await RunPowerShellAsync(step3Args, ct); // run patch generation
        if (rc3 != 0) // patch generation failed
        {
            Emit($"\n❌  Step 3 failed (exit code {rc3}).\n");
            OnStep(3, StepStatus.Failed);
            return new AnalysisResult(false, null);
        }

        // check if patches.json was actually created
        var patchesJson = Path.Combine(_autoRepairRoot, "Step3_Repair", "reports", "patches.json");
        if (!File.Exists(patchesJson)) // no patches generated, nothing to fix
        {
            Emit("ℹ   No patches.json produced (nothing to patch).\n");
            return new AnalysisResult(true, null);
        }

        // dry run finished, patches are ready for the user to review
        Emit($"\n✔  Dry-run complete. Patches written to:\n     {patchesJson}\n");
        Emit("     Awaiting your review before applying.\n");
        return new AnalysisResult(true, patchesJson);
    }

    // phase 2: actually applies the patches to the source files and runs tests to verify
    // only called after the user reviews the patches and clicks apply
    public async Task<bool> ApplyPatchesAndVerifyAsync(CancellationToken ct)
    {
        // need the violations file from the analysis phase
        if (_violationsFile == null || !File.Exists(_violationsFile))
        {
            Emit("❌  Violations file not available. Re-run the analysis first.\n");
            return false; // cant apply without violations data
        }

        OnStep(3, StepStatus.Running); // mark step 3 as running again

        // really important: we have to restore the original source files before applying patches
        // because after step 1 the files still have instrumentation code in them
        // if we patched the instrumented files and then restored, the restore would undo our patches
        var restoreScript = Path.Combine(_autoRepairRoot, "Step1_Tracing", "restore_project.ps1");
        if (File.Exists(restoreScript))
        {
            Emit("Restoring source files to pre-instrumentation state...\n");
            await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{_sourceDir}\"", ct); // restore source dir

            if (_testProject != null) // also restore the test dir if we have one
            {
                var testDir = Path.GetDirectoryName(_testProject)!;
                await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{testDir}\"", ct);
            }
        }

        // now apply patches to the clean original source files (note the -Apply flag this time)
        var step3Script = Path.Combine(_autoRepairRoot, "Step3_Repair", "generate_patches.ps1");
        var step3Args = $"-File \"{step3Script}\" -ViolationsPath \"{_violationsFile}\" -TargetSourceDir \"{_sourceDir}\" -Apply";

        int rc = await RunPowerShellAsync(step3Args, ct); // run it with apply flag
        if (rc != 0) // something went wrong applying patches
        {
            Emit($"\n❌  Patch application failed (exit code {rc}).\n");
            OnStep(3, StepStatus.Failed);
            return false;
        }
        OnStep(3, StepStatus.Success); // patches applied successfully

        // now run the tests to make sure the patches actually fixed the problems
        Emit("\n═══════════════════════════════════════════════════\n");
        Emit("   VERIFICATION — Running tests on patched code\n");
        Emit("═══════════════════════════════════════════════════\n\n");

        if (_testProject != null) // only run tests if we have a test project
        {
            int rcTest = await RunDotnetAsync($"test \"{_testProject}\" --verbosity minimal", ct);
            if (rcTest == 0) // all tests pass, patches are good
            {
                Emit("\n✅  All tests pass on the patched code!\n");
                return true;
            }
            else // some tests still fail even after patching
            {
                Emit("\n⚠   Some tests still fail after patching.\n");
                return false;
            }
        }
        else // no test project so we just have to trust the patches
        {
            Emit("ℹ   No test project detected — skipping verification.\n");
            return true;
        }
    }

    // restores source and test directories from .bak backups
    // used when the user rejects patches and wants to go back to the original code
    public async Task RestoreSourceAsync(CancellationToken ct)
    {
        var restoreScript = Path.Combine(_autoRepairRoot, "Step1_Tracing", "restore_project.ps1");
        if (!File.Exists(restoreScript)) return; // nothing to do if script doesnt exist

        // restore the source directory
        await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{_sourceDir}\"", ct);

        if (_testProject != null) // also restore the test directory if we have one
        {
            var testDir = Path.GetDirectoryName(_testProject)!;
            await RunPowerShellAsync($"-File \"{restoreScript}\" -targetDir \"{testDir}\"", ct);
        }
    }

    // forcefully kills the currently running child process and all its children
    public void Kill()
    {
        try { _activeProcess?.Kill(entireProcessTree: true); } catch { } // try/catch because it might already be dead
    }

    // builds the command line arguments for step 1 (tracing)
    // includes the source dir and optionally the test project path
    private string BuildStep1Args(string scriptPath)
    {
        var args = $"-File \"{scriptPath}\" -TargetSourceDir \"{_sourceDir}\""; // always need source dir
        if (_testProject != null) // add test project if we have one
            args += $" -DriverProject \"{_testProject}\"";
        return args;
    }

    // runs a powershell script with the given arguments
    // sets up the process with no profile, bypass execution policy, and redirected output
    private Task<int> RunPowerShellAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",                                        // use powershell
            Arguments = $"-NoProfile -ExecutionPolicy Bypass {arguments}",      // no profile for speed, bypass policy
            UseShellExecute = false,                                            // need this false for redirection
            RedirectStandardOutput = true,                                      // capture stdout
            RedirectStandardError = true,                                       // capture stderr
            CreateNoWindow = true,                                              // dont show a console window
            WorkingDirectory = _autoRepairRoot,                                 // run from the autorepair root
        };

        // add the local dotnet install to PATH so the child process can find it
        var localDotnet = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet");
        if (Directory.Exists(localDotnet)) // only if dotnet is installed locally
        {
            var currentPath = psi.Environment.ContainsKey("PATH") ? psi.Environment["PATH"] : "";
            psi.Environment["PATH"] = localDotnet + ";" + currentPath; // prepend it to PATH
        }

        return RunProcessAsync(psi, ct); // delegate to the generic process runner
    }

    // runs dotnet.exe with the given arguments (used for test verification)
    private Task<int> RunDotnetAsync(string arguments, CancellationToken ct)
    {
        // first try the local dotnet install path
        var dotnetExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet", "dotnet.exe");

        if (!File.Exists(dotnetExe)) // fall back to global dotnet if local doesnt exist
            dotnetExe = "dotnet";

        var psi = new ProcessStartInfo
        {
            FileName = dotnetExe,              // dotnet executable
            Arguments = arguments,             // e.g. "test MyProject.csproj --verbosity minimal"
            UseShellExecute = false,           // need false for redirection
            RedirectStandardOutput = true,     // capture stdout
            RedirectStandardError = true,      // capture stderr
            CreateNoWindow = true,             // no console window
            WorkingDirectory = _autoRepairRoot, // run from autorepair root
        };

        return RunProcessAsync(psi, ct); // delegate to generic process runner
    }

    // generic method that actually starts a process, hooks up output streaming, and waits for it to finish
    // returns the exit code of the process
    private async Task<int> RunProcessAsync(ProcessStartInfo psi, CancellationToken ct)
    {
        var process = new Process { StartInfo = psi, EnableRaisingEvents = true }; // create the process
        _activeProcess = process; // save reference so Kill() can find it

        var tcs = new TaskCompletionSource<int>(); // used to turn the event-based api into async/await

        // hook up stdout, each line gets sent to the gui as it arrives
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Emit(e.Data + "\n"); // forward output line
        };
        // hook up stderr, prefix with ERR: so its obvious in the log
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Emit("ERR: " + e.Data + "\n"); // forward error line
        };
        // when process exits, set the result on the TaskCompletionSource
        process.Exited += (_, _) =>
        {
            tcs.TrySetResult(process.ExitCode); // this unblocks the await below
        };

        process.Start();                // actually start the process
        process.BeginOutputReadLine();  // start async reading of stdout
        process.BeginErrorReadLine();   // start async reading of stderr

        // register cancellation so if the token is cancelled we kill the process
        using var reg = ct.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); } catch { } // kill it
            tcs.TrySetCanceled(ct); // signal cancellation on the task
        });

        return await tcs.Task; // wait for the process to finish and return exit code
    }

    // helper to fire the OutputReceived event, shorthand so we dont have to write the full invoke everywhere
    private void Emit(string text) => OutputReceived?.Invoke(this, text);
    // helper to fire the StepChanged event
    private void OnStep(int step, StepStatus status) => StepChanged?.Invoke(this, (step, status));
}

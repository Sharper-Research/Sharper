using System.IO;              // for file and directory operations like Directory.Exists, File.ReadAllText, etc.
using System.Text.Json;       // for parsing the patches.json file that step 3 generates
using System.Windows;         // core WPF types like Window, Visibility, RoutedEventArgs
using System.Windows.Controls; // WPF controls like TextBlock, Border, TabControl
using System.Windows.Media;   // for SolidColorBrush so we can change colors in code

namespace AutoRepairGUI; // everything in this project lives under this namespace

// this is the code-behind for the main window
// handles all the user interaction: drag/drop, button clicks, pipeline execution
public partial class MainWindow : Window
{
    private string? _sourceDir;          // path to the C# source project folder, null if nothing loaded yet
    private string? _testProject;        // path to the .csproj of the test project, null if not found
    private string? _autoRepairRoot;     // path to the AutoRepair folder that contains the pipeline scripts
    private PipelineRunner? _runner;     // the object that actually runs the pipeline steps
    private CancellationTokenSource? _cts; // used to cancel the pipeline if the user clicks Cancel

    // constructor, just calls InitializeComponent which WPF needs to set up the XAML
    public MainWindow()
    {
        InitializeComponent(); // this hooks up all the named elements from the XAML
    }

    // drag and drop event handlers

    // this fires when the user is dragging something over the window (before they let go)
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) // check if they're dragging files (not text or something else)
        {
            e.Effects = DragDropEffects.Copy;  // show the copy cursor so the user knows they can drop here
            DropZone.BorderBrush = FindResource("AccentBrush") as SolidColorBrush; // highlight the drop zone blue
        }
        else
        {
            e.Effects = DragDropEffects.None; // not a file, show the "no" cursor
        }
        e.Handled = true; // tell WPF we handled this event so it doesnt bubble up further
    }

    // this fires when the user actually drops something on the window
    private void Window_Drop(object sender, DragEventArgs e)
    {
        DropZone.BorderBrush = FindResource("Surface2Brush") as SolidColorBrush; // reset the border back to normal color

        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return; // bail out if its not a file drop somehow

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop)!; // get the array of dropped file paths
        if (paths.Length == 0) return; // no paths? nothing to do

        AcceptPath(paths[0]); // just take the first dropped item and try to load it
    }

    // this fires when the user clicks on the drop zone (alternative to dragging)
    private void DropZone_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog // open a standard windows file picker dialog
        {
            Title = "Select a C# project (.csproj) or any file in the project folder", // dialog title
            Filter = "C# Project (*.csproj)|*.csproj|All files (*.*)|*.*" // filter to show .csproj first but allow all files
        };
        if (dlg.ShowDialog() == true) // ShowDialog returns true if the user picked something and clicked OK
        {
            AcceptPath(dlg.FileName); // use whatever file they picked
        }
    }

    // this figures out what the user dropped/selected and sets up the project paths
    private void AcceptPath(string path)
    {
        string? sourceDir = null; // will hold the resolved source directory
        string? testProj = null;  // will hold the test project path if found

        // try to figure out what kind of path was given to us
        if (Directory.Exists(path)) // they dropped a folder
        {
            sourceDir = path;
        }
        else if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) // they dropped a .csproj file
        {
            sourceDir = Path.GetDirectoryName(path); // use the folder that contains the .csproj
        }
        else if (File.Exists(path) && path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)) // they dropped a .sln file
        {
            sourceDir = Path.GetDirectoryName(path); // use the folder that contains the .sln
        }
        else if (File.Exists(path)) // they dropped some other file
        {
            sourceDir = Path.GetDirectoryName(path); // just use whatever folder its in
        }

        // if we couldnt figure out a directory or it doesnt exist, show error
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            AppendLog("❌  Could not determine project directory from the dropped path.\n");
            return;
        }

        // now try to find the actual project layout (source + test projects)
        (sourceDir, testProj) = ResolveProjectLayout(sourceDir!);

        if (string.IsNullOrEmpty(sourceDir)) // no .csproj found at all
        {
            AppendLog("❌  No C# project (.csproj) found in the dropped folder.\n");
            return;
        }

        // store the resolved paths in our fields
        _sourceDir = sourceDir;
        _testProject = testProj;

        // update the UI to show what we found
        DropIcon.Text = "✅";                          // change folder emoji to checkmark
        DropText.Text = Path.GetFileName(sourceDir);   // show just the folder name in the drop zone
        InfoPanel.Visibility = Visibility.Visible;     // reveal the info panel
        LblSourceDir.Text = sourceDir;                 // show source path
        UpdateTestProjectUI();                         // update test project label and enable/disable run button
        HidePatchDecisionButtons();                    // make sure apply/reject buttons are hidden

        AppendLog($"📂  Project loaded: {sourceDir}\n"); // log it
        if (testProj != null)
        {
            AppendLog($"🧪  Test project: {testProj}\n"); // log the test project if we found one
        }
        else
        {
            // warn the user they need a test project for the pipeline to work
            AppendLog("⚠   No test project auto-detected.\n");
            AppendLog("     The pipeline requires a test project to generate runtime traces.\n");
            AppendLog("     Use the Browse… button next to Tests to select one.\n");
        }
    }

    // tries to find the source project and test project from a given directory
    // returns a tuple of (sourceDir, testProjectPath)
    private (string? sourceDir, string? testProject) ResolveProjectLayout(string dir)
    {
        var localProjects = Directory.GetFiles(dir, "*.csproj"); // first check if there are .csproj files right in this folder
        if (localProjects.Length > 0) // found at least one project file here
        {
            var testProj = DetectTestProject(dir); // try to find a matching test project nearby
            return (dir, testProj); // return this dir as the source and whatever test project we found
        }

        // no .csproj directly in the folder, so look one level down in subdirectories
        var subProjects = Directory.GetDirectories(dir)
            .SelectMany(d => Directory.GetFiles(d, "*.csproj")) // get all .csproj files in immediate subdirs
            .ToList();

        if (subProjects.Count == 0) // still nothing? search recursively through all subdirs
        {
            subProjects = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\")) // skip build output folders
                .ToList();
        }

        if (subProjects.Count == 0) // truly no projects found anywhere
            return (null, null);

        // split projects into test projects and source projects
        var testProjects = subProjects.Where(p => IsTestProject(p)).ToList();
        var sourceProjects = subProjects.Where(p => !IsTestProject(p)).ToList();

        string? sourceDir2 = null;
        string? testProj2 = testProjects.FirstOrDefault(); // grab the first test project if any

        if (sourceProjects.Count > 0)
            sourceDir2 = Path.GetDirectoryName(sourceProjects[0]); // use the first source project's folder
        else
            sourceDir2 = Path.GetDirectoryName(subProjects[0]); // fallback to whatever we found first

        return (sourceDir2, testProj2);
    }

    // checks if a .csproj file is a test project
    // looks at the name first, then checks inside the file for test framework references
    private static bool IsTestProject(string csprojPath)
    {
        var name = Path.GetFileNameWithoutExtension(csprojPath); // get the project name without .csproj
        // check common naming patterns for test projects
        if (name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase))
            return true; // name looks like a test project

        try
        {
            var content = File.ReadAllText(csprojPath); // read the whole .csproj file
            // look for NuGet package references that test projects typically have
            if (content.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("MSTest", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("NUnit", StringComparison.OrdinalIgnoreCase))
                return true; // it references a test framework, so its a test project
        }
        catch { } // if we cant read the file just ignore it

        return false; // doesnt look like a test project
    }

    // tries to find a test project that goes with the given source directory
    // checks common patterns like MyProject.Tests folder next to MyProject folder
    private static string? DetectTestProject(string sourceDir)
    {
        var dirInfo = new DirectoryInfo(sourceDir); // wrap in DirectoryInfo to get parent
        var parentDir = dirInfo.Parent;             // need the parent to look for sibling folders
        if (parentDir == null) return null;         // if theres no parent (root drive) give up

        var dirName = dirInfo.Name; // name of the source folder

        // look for a sibling folder named <project>.Tests
        var testsDir = Path.Combine(parentDir.FullName, dirName + ".Tests");
        if (Directory.Exists(testsDir)) // found it!
        {
            var csproj = Directory.GetFiles(testsDir, "*.csproj").FirstOrDefault(); // grab the .csproj in there
            if (csproj != null) return csproj;
        }

        // look for any sibling folder with "Test" in its name
        foreach (var d in parentDir.GetDirectories("*Test*"))
        {
            var csproj = Directory.GetFiles(d.FullName, "*.csproj").FirstOrDefault();
            if (csproj != null) return csproj; // return the first one we find
        }

        // last resort: search recursively inside the source dir for *.Tests.csproj
        foreach (var csproj in Directory.GetFiles(sourceDir, "*.Tests.csproj", SearchOption.AllDirectories))
        {
            return csproj; // just return the first match
        }

        return null; // couldnt find any test project
    }

    // handler for the Browse button next to the test project label
    // lets the user manually pick a test .csproj if auto-detection failed
    private void BtnBrowseTests_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog // standard windows file picker
        {
            Title = "Select the test project (.csproj)",
            Filter = "C# Test Project (*.csproj)|*.csproj" // only show .csproj files
        };

        // try to start the dialog in a sensible folder (parent of the source dir)
        if (_sourceDir != null)
        {
            var parent = Directory.GetParent(_sourceDir);
            if (parent != null) dlg.InitialDirectory = parent.FullName; // start browsing from the parent folder
        }

        if (dlg.ShowDialog() == true) // user picked a file
        {
            _testProject = dlg.FileName;    // save the selected test project path
            UpdateTestProjectUI();          // refresh the label and run button state
            AppendLog($"🧪  Test project set: {_testProject}\n");
        }
    }

    // updates the test project label text and enables/disables the run button
    private void UpdateTestProjectUI()
    {
        if (_testProject != null) // we have a test project set
        {
            LblTestProject.Text = _testProject; // show the path
            LblTestProject.Foreground = FindResource("TextBrush") as SolidColorBrush; // normal text color
            BtnRun.IsEnabled = true; // enable the run button since we have everything we need
        }
        else // no test project
        {
            LblTestProject.Text = "⚠ No test project found — click Browse… to select one"; // warning message
            LblTestProject.Foreground = FindResource("YellowBrush") as SolidColorBrush; // yellow to show warning
            BtnRun.IsEnabled = false; // cant run without a test project
        }
    }

    // the main handler for the Run Pipeline button
    // this kicks off the whole analysis pipeline (steps 1-3 in dry run mode)
    // marked async because the pipeline runs in the background
    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceDir == null) return; // shouldnt happen but just in case

        if (_testProject == null) // double check we have a test project
        {
            AppendLog("❌  Cannot run pipeline without a test project. Use Browse… to select one.\n");
            return;
        }

        // set up the UI for running state
        BtnRun.IsEnabled = false;            // disable run button while pipeline is going
        BtnCancel.IsEnabled = true;          // enable cancel button
        HidePatchDecisionButtons();          // hide apply/reject from any previous run
        ResetSteps();                        // set all step indicators back to "Waiting"
        PatchList.ItemsSource = null;        // clear any old patches from the review tab
        PatchSummary.Text = "No patches generated yet.";

        _cts = new CancellationTokenSource(); // create a fresh token source for this run

        // find the AutoRepair root folder (where the pipeline scripts live)
        _autoRepairRoot = FindAutoRepairRoot();
        if (_autoRepairRoot == null) // couldnt find it
        {
            AppendLog("❌  Cannot locate AutoRepair folder. Ensure the app is inside the Patching workspace.\n");
            BtnRun.IsEnabled = true;   // re-enable so they can try again
            BtnCancel.IsEnabled = false;
            return;
        }

        // create the runner that will orchestrate the pipeline steps
        _runner = new PipelineRunner(_autoRepairRoot, _sourceDir, _testProject);
        // wire up events: when the runner produces output, add it to our log
        // Dispatcher.Invoke is needed because the events come from background threads
        _runner.OutputReceived += (_, msg) => Dispatcher.Invoke(() => AppendLog(msg));
        // when a step changes status, update the indicator cards
        _runner.StepChanged += (_, info) => Dispatcher.Invoke(() => UpdateStep(info.Step, info.Status));

        try
        {
            // run all three steps in dry-run mode (doesnt actually apply patches yet)
            var result = await _runner.RunAnalysisAsync(_cts.Token);

            if (!result.Success) // something went wrong during analysis
            {
                AppendLog("\n⛔  Pipeline finished with errors. Check the log above.\n");
                BtnRun.IsEnabled = true;
                BtnCancel.IsEnabled = false;
                return;
            }

            // try to load the patches that step 3 generated
            var patches = LoadPatches(result.PatchesJsonPath);

            if (patches.Count == 0) // no patches means no violations were found
            {
                AppendLog("\nℹ   No patches were generated — no actionable violations found.\n");
                UpdateStep(3, StepStatus.Success);
                BtnRun.IsEnabled = true;
                BtnCancel.IsEnabled = false;
                return;
            }

            // we got patches! show them in the review tab
            ShowPatchReview(patches);
            AppendLog($"\n🔍  {patches.Count} patch(es) generated. Switch to the Patch Review tab to inspect them.\n");
            AppendLog("     Click ✔ Apply Patches to apply, or ✘ Reject Patches to discard.\n");
        }
        catch (OperationCanceledException) // user clicked Cancel
        {
            AppendLog("\n🛑  Pipeline cancelled by user.\n");
            BtnRun.IsEnabled = true;
            BtnCancel.IsEnabled = false;
        }
        catch (Exception ex) // something unexpected blew up
        {
            AppendLog($"\n❌  Unexpected error: {ex.Message}\n");
            BtnRun.IsEnabled = true;
            BtnCancel.IsEnabled = false;
        }
    }

    // handler for the Apply Patches button
    // this actually writes the patches to the source files and runs tests to verify
    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_autoRepairRoot == null || _sourceDir == null || _runner == null) return; // safety checks

        HidePatchDecisionButtons();    // hide apply/reject since user already chose
        BtnCancel.IsEnabled = true;    // let them cancel during apply
        // print a big header in the log so its obvious whats happening
        AppendLog("\n═══════════════════════════════════════════════════\n");
        AppendLog("   APPLYING PATCHES\n");
        AppendLog("═══════════════════════════════════════════════════\n\n");

        _cts = new CancellationTokenSource(); // new token source for this phase

        try
        {
            // this restores clean source, applies patches, then runs dotnet test
            var success = await _runner.ApplyPatchesAndVerifyAsync(_cts.Token);

            if (success) // all tests passed after patching
            {
                UpdateStep(3, StepStatus.Success);
                AppendLog("\n🎉  Patches applied and verified successfully!\n");
            }
            else // patches applied but some tests still fail
            {
                AppendLog("\n⚠   Patches applied but some tests may still fail.\n");
            }
        }
        catch (OperationCanceledException) // user cancelled
        {
            AppendLog("\n🛑  Cancelled by user.\n");
        }
        catch (Exception ex) // something went wrong
        {
            AppendLog($"\n❌  Error applying patches: {ex.Message}\n");
        }
        finally // always runs, even if there was an exception
        {
            BtnRun.IsEnabled = true;    // re-enable run button
            BtnCancel.IsEnabled = false; // disable cancel since nothing is running
            _cts = null;                 // clear the token source
        }
    }

    // handler for the Reject Patches button
    // undoes any instrumentation and restores original source files
    private async void BtnReject_Click(object sender, RoutedEventArgs e)
    {
        HidePatchDecisionButtons(); // hide the apply/reject buttons
        AppendLog("\n✘  Patches rejected by user.\n");

        // try to restore the source files back to their original state from backups
        if (_autoRepairRoot != null && _sourceDir != null)
        {
            var restoreScript = Path.Combine(_autoRepairRoot, "Step1_Tracing", "restore_project.ps1");
            if (File.Exists(restoreScript) && _runner != null) // make sure the restore script exists
            {
                AppendLog("Restoring original source files...\n");
                _cts = new CancellationTokenSource();
                try
                {
                    await _runner.RestoreSourceAsync(_cts.Token); // run the restore script
                    AppendLog("✅  Source files restored to original state.\n");
                }
                catch (Exception ex)
                {
                    AppendLog($"⚠   Restore error: {ex.Message}\n"); // log it but dont crash
                }
                finally { _cts = null; }
            }
        }

        BtnRun.IsEnabled = true;    // re-enable run button
        BtnCancel.IsEnabled = false; // nothing running anymore
    }

    // cancel button handler, cancels the token and kills the running process
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();    // signal cancellation through the token
        _runner?.Kill();   // forcefully kill the child process if one is running
    }

    // clear log button handler, resets everything back to clean state
    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        OutputLog.Text = "";                          // wipe the log text
        ResetSteps();                                 // reset step indicators to "Waiting"
        PatchList.ItemsSource = null;                 // clear patch cards
        PatchSummary.Text = "No patches generated yet."; // reset summary text
        HidePatchDecisionButtons();                   // hide apply/reject buttons
    }

    // reads patches.json and turns it into a list of PatchDisplayItem objects
    // for showing in the patch review list
    private List<PatchDisplayItem> LoadPatches(string? patchesJsonPath)
    {
        // if path is null/empty or file doesnt exist, return empty list
        if (string.IsNullOrEmpty(patchesJsonPath) || !File.Exists(patchesJsonPath))
            return [];

        try
        {
            var json = File.ReadAllText(patchesJsonPath);   // read the whole json file
            using var doc = JsonDocument.Parse(json);        // parse it into a json document
            var root = doc.RootElement;                      // grab the root element

            // the json should have a "patches" array at the top level
            if (!root.TryGetProperty("patches", out var patchesArray))
                return []; // no patches property found, return empty

            var items = new List<PatchDisplayItem>(); // list to collect each patch
            foreach (var p in patchesArray.EnumerateArray()) // loop through each patch in the array
            {
                // pull out each field from the json, using empty defaults if missing
                items.Add(new PatchDisplayItem
                {
                    File = p.TryGetProperty("file", out var f) ? f.GetString() ?? "" : "",
                    Line = p.TryGetProperty("line", out var l) ? l.GetInt32() : 0,
                    RuleName = p.TryGetProperty("ruleName", out var r) ? r.GetString() ?? "" : "",
                    Description = p.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    OriginalCode = p.TryGetProperty("originalCode", out var o) ? o.GetString() ?? "" : "",
                    PatchedCode = p.TryGetProperty("patchedCode", out var pc) ? pc.GetString() ?? "" : "",
                });
            }
            return items; // return all the patches we found
        }
        catch (Exception ex)
        {
            AppendLog($"⚠   Could not parse patches.json: {ex.Message}\n"); // log parse errors
            return []; // return empty on failure
        }
    }

    // shows the patch review tab with the list of patches and reveals the apply/reject buttons
    private void ShowPatchReview(List<PatchDisplayItem> patches)
    {
        PatchList.ItemsSource = patches; // bind the list to the ui
        // update the summary text with how many patches we got
        PatchSummary.Text = $"🔧  {patches.Count} patch(es) ready for review.  Review the changes below, then Apply or Reject.";

        MainTabs.SelectedItem = TabPatchReview; // switch to the patch review tab so user sees patches

        // show the apply and reject buttons, disable run/cancel since were waiting for user decision
        BtnApply.Visibility = Visibility.Visible;
        BtnReject.Visibility = Visibility.Visible;
        BtnRun.IsEnabled = false;
        BtnCancel.IsEnabled = false;
    }

    // hides the apply and reject buttons (used after user makes a decision or on clear)
    private void HidePatchDecisionButtons()
    {
        BtnApply.Visibility = Visibility.Collapsed;  // collapsed means it takes no space
        BtnReject.Visibility = Visibility.Collapsed;
    }

    // appends text to the output log and scrolls to bottom so latest output is visible
    private void AppendLog(string text)
    {
        OutputLog.AppendText(text);       // add text to the textbox
        OutputLog.ScrollToEnd();          // scroll the textbox itself
        OutputScroller.ScrollToEnd();     // also scroll the parent scrollviewer
    }

    // resets all three step indicators back to their default "Waiting" state
    private void ResetSteps()
    {
        foreach (var (icon, status, border) in GetStepControls()) // loop through all 3 steps
        {
            icon.Text = "○";                                                   // empty circle icon
            icon.Foreground = FindResource("SubtextBrush") as SolidColorBrush; // dim color
            status.Text = "Waiting";                                           // reset label
            status.Foreground = FindResource("SubtextBrush") as SolidColorBrush;
            border.BorderBrush = FindResource("Surface2Brush") as SolidColorBrush; // neutral border
        }
    }

    // updates a specific step indicator (1, 2, or 3) to show running, success, or failed
    private void UpdateStep(int step, StepStatus status)
    {
        var controls = GetStepControls();              // get all step control tuples
        if (step < 1 || step > controls.Count) return; // bounds check
        var (icon, lbl, border) = controls[step - 1];  // grab the right one (step is 1-based)

        switch (status)
        {
            case StepStatus.Running: // step is currently executing
                icon.Text = "⏳";
                icon.Foreground = FindResource("YellowBrush") as SolidColorBrush;
                lbl.Text = "Running…";
                lbl.Foreground = FindResource("YellowBrush") as SolidColorBrush;
                border.BorderBrush = FindResource("YellowBrush") as SolidColorBrush;
                break;
            case StepStatus.Success: // step finished successfully
                icon.Text = "✔";
                icon.Foreground = FindResource("GreenBrush") as SolidColorBrush;
                lbl.Text = "Complete";
                lbl.Foreground = FindResource("GreenBrush") as SolidColorBrush;
                border.BorderBrush = FindResource("GreenBrush") as SolidColorBrush;
                break;
            case StepStatus.Failed: // step hit an error
                icon.Text = "✘";
                icon.Foreground = FindResource("RedBrush") as SolidColorBrush;
                lbl.Text = "Failed";
                lbl.Foreground = FindResource("RedBrush") as SolidColorBrush;
                border.BorderBrush = FindResource("RedBrush") as SolidColorBrush;
                break;
        }
    }

    // returns a list of (icon, statusLabel, border) tuples for each of the 3 steps
    // makes it easy to loop through or index into them
    private List<(TextBlock icon, TextBlock status, Border border)> GetStepControls() =>
    [
        (Step1Icon, Step1Status, Step1Border), // tracing step
        (Step2Icon, Step2Status, Step2Border), // constraints step
        (Step3Icon, Step3Status, Step3Border), // repair step
    ];

    // walks up the directory tree looking for the AutoRepair folder
    // starts from the exe location, then falls back to the source project dir
    private string? FindAutoRepairRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory; // start from where the exe lives
        for (int i = 0; i < 10; i++) // walk up at most 10 levels
        {
            var candidate = Path.Combine(dir, "AutoRepair"); // check if AutoRepair exists here
            // make sure it has all 3 step folders so we know its the right one
            if (Directory.Exists(candidate) &&
                Directory.Exists(Path.Combine(candidate, "Step1_Tracing")) &&
                Directory.Exists(Path.Combine(candidate, "Step2_Constraints")) &&
                Directory.Exists(Path.Combine(candidate, "Step3_Repair")))
            {
                return candidate; // found it
            }
            var parent = Directory.GetParent(dir); // go up one level
            if (parent == null) break;              // hit the filesystem root
            dir = parent.FullName;
        }

        // if we didnt find it from exe location, try from the source project directory
        if (_sourceDir != null)
        {
            dir = _sourceDir;
            for (int i = 0; i < 10; i++) // same walk-up approach
            {
                var candidate = Path.Combine(dir, "AutoRepair");
                if (Directory.Exists(candidate)) return candidate; // found it
                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
        }

        return null; // couldnt find it anywhere
    }
}

// simple data class that holds one patch for display in the list
// each property maps to a field in the patches.json file
public class PatchDisplayItem
{
    public string File { get; set; } = "";           // which source file the patch is for
    public int Line { get; set; }                     // line number of the change
    public string RuleName { get; set; } = "";        // name of the constraint rule that triggered this
    public string Description { get; set; } = "";     // human readable description of the fix
    public string OriginalCode { get; set; } = "";    // the original code before patching
    public string PatchedCode { get; set; } = "";     // the new code after patching
    public string LineLabel => $"Line {Line}";        // formatted label for the ui
}

// enum for the three possible states of a pipeline step
public enum StepStatus { Running, Success, Failed }
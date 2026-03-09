# AutoRepair GUI

A WPF desktop application that provides a graphical interface for running the AutoRepair pipeline (Tracing → Constraints → Repair) on C# projects.

## Prerequisites

- **Windows 10/11** (WPF is Windows-only)
- **.NET 10 SDK** — download from [https://dotnet.microsoft.com](https://dotnet.microsoft.com)
  - Verify with: `dotnet --version` (should print `10.x.x`)

> **`dotnet` not recognized?** The SDK may be installed but not on your PATH.  
> Common install locations:
> - `C:\Users\<you>\AppData\Local\Microsoft\dotnet\dotnet.exe`
> - `C:\Program Files\dotnet\dotnet.exe`
>
> **Quick fix** — add it to your current PowerShell session:
> ```powershell
> $env:PATH = "C:\Users\$env:USERNAME\AppData\Local\Microsoft\dotnet;" + $env:PATH
> ```
> **Permanent fix** — add the folder to your system PATH via  
> Settings → System → About → Advanced system settings → Environment Variables → Path → Edit.

## Project Structure

```
GUI/
  GUI/
    App.xaml              — WPF application entry point
    App.xaml.cs           — startup code-behind
    MainWindow.xaml       — all the UI layout (dark Catppuccin Mocha theme)
    MainWindow.xaml.cs    — event handlers, pipeline execution logic
    PipelineRunner.cs     — runs the 3 pipeline steps as child processes
    GUI.csproj            — project file (targets net10.0-windows, WPF enabled)
    Properties/
      AssemblyInfo.cs     — assembly metadata
```

## Build

From the repo root:

```powershell
dotnet build AutoRepair/GUI/GUI/GUI.csproj
```

Or from inside the `GUI/GUI/` folder:

```powershell
dotnet build
```

The compiled output goes to `bin/Debug/net10.0-windows/`.

## Run

```powershell
dotnet run --project AutoRepair/GUI/GUI/GUI.csproj
```

Or from inside `GUI/GUI/`:

```powershell
dotnet run
```

This opens the AutoRepair GUI window.

## How to Use

1. **Load a project** — Drag and drop a C# project folder (or `.csproj` file) onto the drop zone, or click the drop zone to browse.
2. The GUI auto-detects the source directory and test project. You can change the test project with the **Browse** button if needed.
3. Click **Run Pipeline** to execute all three steps in sequence:
   - **Step 1 — Tracing**: instruments the code and runs the test suite to produce `trace_logs.jsonl`
   - **Step 2 — Constraints**: analyzes the trace to find constraint violations
   - **Step 3 — Repair**: generates source-level patches for the violations
4. Watch progress in the **Output Log** tab. Step cards at the top show status (waiting / running / passed / failed).
5. When done, switch to the **Patch Review** tab to inspect each proposed fix (original vs patched code).
6. Click **Apply Patches** to write changes to disk, or **Reject Patches** to discard them.

## Publish (optional)

To create a self-contained executable that doesn't require the .NET SDK on the target machine:

```powershell
dotnet publish AutoRepair/GUI/GUI/GUI.csproj -c Release -r win-x64 --self-contained
```

The output will be in `bin/Release/net10.0-windows/win-x64/publish/`.

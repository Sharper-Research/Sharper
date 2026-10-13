# Microsoft Bug Demo: Null Dereference

**Bug:** [dotnet/runtime#120561](https://github.com/dotnet/runtime/issues/120561) (Oct 2025, Open)  
**Type:** NullReferenceException  
**Source:** `ReflectionXmlSerializationWriter.cs`. `BindingFlags.DeclaredOnly` skips base-class methods, `GetMethod()` returns null.

## How to Run

From the repo root (`Patching/`):

```powershell
# Step 1: Instrument & trace
.\AutoRepair\Step1_Tracing\generate_trace.ps1 -TargetSourceDir "MicrosoftBugDemo\RuntimeCode" -TestProject "MicrosoftBugDemo\RuntimeCode.Tests\RuntimeCode.Tests.csproj"

# Step 2: Analyze trace & detect violations
.\AutoRepair\Step2_Constraints\analyze_trace.ps1

# View results
Get-Content .\AutoRepair\Step2_Constraints\reports\violations.json

# Restore source files
.\AutoRepair\Step1_Tracing\restore_project.ps1 "MicrosoftBugDemo\RuntimeCode"
```

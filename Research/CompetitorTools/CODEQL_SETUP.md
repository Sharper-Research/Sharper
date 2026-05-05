# CodeQL Setup Guide for C# Projects

CodeQL is GitHub's free static analysis engine. It converts your code into a
queryable database and runs security and quality queries against it. It is
particularly strong at data-flow and taint analysis.

---

## 1. Prerequisites

- Windows 10/11
- .NET SDK installed
- A C# project that builds successfully
- ~3 GB of free disk space for the CLI bundle

---

## 2. Download the CodeQL CLI

**Step 1 — Create the folder:**
```powershell
mkdir C:\codeql
```

**Step 2 — Download the bundle (~2.5 GB):**
```powershell
Invoke-WebRequest -Uri "https://github.com/github/codeql-action/releases/latest/download/codeql-bundle-win64.tar.gz" -OutFile "C:\codeql\codeql-bundle-win64.tar.gz"
```

**Step 3 — Extract it:**
```powershell
tar -xzf "C:\codeql\codeql-bundle-win64.tar.gz" -C "C:\codeql"
```

You should now have: `C:\codeql\codeql\codeql.exe`

---

## 3. Add CodeQL to PATH

Run this once in PowerShell to make `codeql` available in every terminal:
```powershell
[System.Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\codeql\codeql", "User")
```

Then add it to the current session:
```powershell
$env:PATH += ";C:\codeql\codeql"
```

Verify it works:
```powershell
codeql --version
```

Expected output:
```
CodeQL command-line toolchain release 2.25.2.
Copyright (C) 2019-2026 GitHub, Inc.
```

---

## 4. Create a CodeQL Database

CodeQL first compiles your project into a queryable database, then runs queries
against it.

**Standard command:**
```powershell
codeql database create "C:\codeql\microsoftbugdemo-db" `
  --language=csharp `
  --command="dotnet build C:\Patching\Patching\MicrosoftBugDemo\RuntimeCode\RuntimeCode.csproj" `
  --overwrite
```

### Troubleshooting: .NET 10 Compatibility

CodeQL's C# extractor may not fully support .NET 10 yet. If you see:

```
CodeQL detected code written in C# but could not process any of it.
```

Use build-mode none instead (no build required):
```powershell
codeql database create "C:\codeql\microsoftbugdemo-db" `
  --language=csharp `
  --build-mode=none `
  --source-root="C:\Patching\Patching\MicrosoftBugDemo" `
  --overwrite
```

This mode extracts the source files directly without running a build command
and works around the .NET 10 extractor limitation.

---

## 5. Run the Analysis

```powershell
codeql database analyze "C:\codeql\microsoftbugdemo-db" `
  csharp-code-scanning.qls `
  --format=sarif-latest `
  --output="C:\Patching\Patching\codeql-results.sarif"
```

`csharp-code-scanning.qls` is the standard C# query suite included in the
bundle — no separate download needed.

---

## 6. View the Results

**Option A — VS Code SARIF Viewer (Recommended)**
1. Install the extension: search "SARIF Viewer" by Microsoft in VS Code extensions
2. Open `C:\Patching\Patching\codeql-results.sarif` in VS Code
3. A panel shows all findings — click any to jump to the code location

**Option B — Read the JSON directly**

Open `codeql-results.sarif` in any text editor and search for `"ruleId"` to
find individual findings.

---

## 7. Full Command Reference

| Task | Command |
|---|---|
| Check version | `codeql --version` |
| Create database (standard) | `codeql database create ./db --language=csharp --command="dotnet build MyProject.csproj"` |
| Create database (.NET 10 workaround) | `codeql database create ./db --language=csharp --build-mode=none --source-root="C:\path\to\project"` |
| Run analysis | `codeql database analyze ./db csharp-code-scanning.qls --format=sarif-latest --output=results.sarif` |
| List available query suites | `codeql resolve queries csharp-code-scanning.qls` |
| Upgrade database after CLI update | `codeql database upgrade ./db` |

---

## 8. What CodeQL Can and Cannot Tell You

| What CodeQL reports | Example |
|---|---|
| File and line number | `MemberMethodResolver.cs:18` |
| Rule ID with CWE mapping | `CWE-476 — Null dereference` |
| Severity | `error / warning` |
| Data-flow path (for taint bugs) | Source → sink path diagram |

| What CodeQL cannot report | Why |
|---|---|
| The actual variable value at runtime | Static analysis only — never runs the program |
| Which specific input triggered the bug | Cannot reason about runtime inputs |
| The full execution trace to the crash | No dynamic instrumentation |
| Why GetMethod() returned null | Cannot observe runtime reflection behaviour |

---

## 9. Results on MicrosoftBugDemo

The `MicrosoftBugDemo` project contains a known `NullReferenceException` crash
on `MemberMethodResolver.cs:18`. CodeQL returned:

```
No issues found
```

This is consistent with SonarQube, PVS-Studio, and Semgrep on the same project.

The bug only manifests at runtime when `GetMethod()` is called with
`BindingFlags.DeclaredOnly` on an inherited member — a condition that static
analysis cannot observe without executing the program.

The Autorepair tool caught it by instrumenting the running program and recording
the exact null assignment, the input that caused it, and the full execution
trace leading to the crash.


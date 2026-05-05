# InferSharp Setup Guide for C# Projects

InferSharp is Microsoft's open-source static analysis tool built on top of Facebook's
Infer engine. It analyses compiled .NET binaries (not source code) and detects null
dereferences, resource leaks, and thread-safety violations.

---

## 1. Prerequisites

- Windows 10/11 with WSL2 enabled
- .NET SDK installed (to build your project)
- ~500 MB of free disk space for the InferSharp distro

**Check if WSL2 is already on your machine:**
```powershell
wsl --list --verbose
```

If you see any distro with VERSION 2, WSL2 is ready. If the list is empty, enable
WSL2 first by following Microsoft's official WSL installation guide.

---

## 2. Download the InferSharp WSL Distro

InferSharp runs inside a custom Ubuntu WSL2 distribution provided by Microsoft.

**Step 1 — Create a folder for the distro:**
```powershell
mkdir C:\wslDistroStorage\infersharp1.4
```

**Step 2 — Download the distro (~495 MB):**
```powershell
Invoke-WebRequest -Uri "https://github.com/microsoft/infersharp/releases/download/v1.4/infersharp-wsl-distro-v1.4.tar.gz" -OutFile "C:\wslDistroStorage\infersharp-wsl-distro-v1.4.tar.gz"
```

---

## 3. Import the Distro into WSL

Run this in **PowerShell as Administrator** (right-click PowerShell → Run as administrator):

```powershell
wsl --import infersharp1.4 C:\wslDistroStorage\infersharp1.4 C:\wslDistroStorage\infersharp-wsl-distro-v1.4.tar.gz
```

Verify it registered:
```powershell
wsl --list --verbose
```

You should see `infersharp1.4` in the list with VERSION 2.

---

## 4. Build Your C# Project First

InferSharp analyses compiled binaries, not source code. Build your project before running:

```powershell
dotnet build "C:\path\to\your\project.csproj"
```

Example:
```powershell
dotnet build "C:\Patching\Patching\MicrosoftBugDemo\RuntimeCode\RuntimeCode.csproj"
```

The compiled output will be in the `bin\Debug\net*\` folder.

---

## 5. Run InferSharp

**Step 1 — Launch the InferSharp WSL distro:**
```powershell
wsl ~ -d infersharp1.4
```

You will see an Ubuntu terminal prompt.

**Step 2 — Run the analyser (inside WSL):**
```bash
cd infersharp && ./run_infersharp.sh /mnt/c/Patching/Patching/MicrosoftBugDemo/RuntimeCode/bin/Debug/net10.0/
```

The path format uses WSL's mount convention:
- Windows path `C:\Patching\Patching\...` becomes `/mnt/c/Patching/Patching/...`
- Always point to the folder containing the compiled `.dll` files

**Step 3 — Exit WSL when done:**
```bash
exit
```

---

## 6. Understanding the Output

A typical run looks like this:

```
Processing {/mnt/c/.../bin/Debug/net10.0/}
Copying binaries to a staging folder...

Code translation started...
Translation stage 1/3: Loading binaries.
Translation stage 2/3: Computing type environment.
Translation stage 3/3: Computing control-flow graph.

Coverage Statistics:
Method successfully translated: 1 (100%)
Instructions translated: 22 (100%)

Code translation completed. Analyzing...
1/1 [##########] 100%

  No issues found
```

If issues are found, they appear as:
```
file.cs:18: error: NULL_DEREFERENCE
  object `method` last assigned on line 13 could be null and is dereferenced at line 18.
```

---

## 7. Full Command Reference

| Task | Command |
|---|---|
| Check WSL distros | `wsl --list --verbose` |
| Launch InferSharp | `wsl ~ -d infersharp1.4` |
| Run analysis (inside WSL) | `./run_infersharp.sh /mnt/c/path/to/bin/folder/` |
| Exit WSL | `exit` |

---

## 8. What InferSharp Can and Cannot Tell You

| What InferSharp reports | Example |
|---|---|
| File and line number | `MemberMethodResolver.cs:18` |
| Bug type | `NULL_DEREFERENCE` |
| Which variable was null | `object 'method' could be null` |
| Last assignment line | `last assigned on line 13` |

| What InferSharp cannot report | Why |
|---|---|
| The actual value at runtime | It analyses binaries statically, never runs the program |
| Which specific input triggered the bug | No runtime instrumentation |
| The full execution trace to the crash | No dynamic observation |
| Why GetMethod() returned null | Cannot reason about specific input values |

---

## 9. Results on MicrosoftBugDemo

The `MicrosoftBugDemo` project contains a known `NullReferenceException` crash
on `MemberMethodResolver.cs:18`. InferSharp analysed the compiled binary and
returned:

```
No issues found
```

This is the same result as SonarQube, PVS-Studio, and Semgrep on the same project.

All four tools perform static analysis and missed the bug because it only occurs
at runtime with a specific input (`BaseProp` looked up on a `Test` object with
`BindingFlags.DeclaredOnly`).

The Autorepair tool caught it by instrumenting the running program and observing
the actual null assignment at the moment of failure.

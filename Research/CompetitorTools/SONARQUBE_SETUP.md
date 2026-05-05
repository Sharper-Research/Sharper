# SonarQube Setup Guide for C# Projects

SonarQube is a self-hosted web platform that analyses your code and displays
bugs, code smells, and vulnerabilities in a browser dashboard. It is the most
widely recognised static analysis tool in enterprise software.

---

## 1. Prerequisites

- Windows 10/11
- Docker Desktop installed (recommended method)
- .NET SDK installed
- A C# project that builds successfully

---

## 2. Start the SonarQube Server

**Step 1 — Install Docker Desktop**

Download from: https://www.docker.com/products/docker-desktop/
Install it, start it, and wait for it to say "Engine running".

**Step 2 — Start the SonarQube server:**
```powershell
docker run -d --name sonarqube -p 9000:9000 sonarqube:community
```

This downloads the SonarQube Community image (~600 MB, one-time) and starts it.
Wait about 60 seconds for it to fully start.

**Step 3 — Open the dashboard:**

Go to: http://localhost:9000 in your browser.
Default login: `admin` / `admin`
It will ask you to change the password on first login.

---

## 3. Create a Project

1. Click **Create a local project**
2. Enter a Project Key (e.g. `microsoftbugdemo`) and a Display Name
3. Choose **Use the global setting** for the quality gate
4. Click **Set up**
5. Choose **Locally** as the analysis method
6. Generate a token — copy it, you will need it in the next step

---

## 4. Install the SonarScanner for .NET

Run this once in any terminal:
```powershell
dotnet tool install --global dotnet-sonarscanner
```

Verify:
```powershell
dotnet sonarscanner --version
```

---

## 5. Run the Analysis

Navigate to your project folder and run these three commands in order:

**Command 1 — Begin analysis:**
```powershell
dotnet sonarscanner begin /k:"microsoftbugdemo" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="YOUR_TOKEN_HERE"
```

**Command 2 — Build:**
```powershell
dotnet build "C:\Patching\Patching\MicrosoftBugDemo\RuntimeCode\RuntimeCode.csproj"
```

**Command 3 — Upload results:**
```powershell
dotnet sonarscanner end /d:sonar.token="YOUR_TOKEN_HERE"
```

Replace `YOUR_TOKEN_HERE` with the token you copied in Step 3.

---

## 6. Quick Re-run on MicrosoftBugDemo

If SonarQube is already set up, use these exact commands:

**Start SonarQube if not running:**
```powershell
docker start sonarqube
```

Wait ~60 seconds, then check http://localhost:9000 is up.

**Command 1:**
```powershell
cd C:\Patching\Patching\MicrosoftBugDemo
```

**Command 2:**
```powershell
dotnet sonarscanner begin /k:"throttledebounce" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="YOUR_TOKEN_HERE"
```

**Command 3:**
```powershell
dotnet build MicrosoftBugDemo.slnx
```

**Command 4:**
```powershell
dotnet sonarscanner end /d:sonar.token="YOUR_TOKEN_HERE"
```

Then open http://localhost:9000 → Projects → ThrottleDebounce to see results.

**Stop SonarQube when done:**
```powershell
docker stop sonarqube
```

---

## 7. Full Command Reference

| Task | Command |
|---|---|
| Start SonarQube | `docker run -d --name sonarqube -p 9000:9000 sonarqube:community` |
| Start existing container | `docker start sonarqube` |
| Stop container | `docker stop sonarqube` |
| Install scanner | `dotnet tool install --global dotnet-sonarscanner` |
| Begin scan | `dotnet sonarscanner begin /k:"KEY" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="TOKEN"` |
| Build | `dotnet build YourSolution.sln` |
| Upload results | `dotnet sonarscanner end /d:sonar.token="TOKEN"` |

---

## 8. What SonarQube Can and Cannot Tell You

| What SonarQube reports | Example |
|---|---|
| File and line number | `MemberMethodResolver.cs:13` |
| Rule ID | `S2259 — null dereference` |
| Severity | `Blocker / Critical / Major / Minor` |
| Brief explanation of the pattern | "Variable may be null here" |

| What SonarQube cannot report | Why |
|---|---|
| The actual variable value at runtime | Static analysis only — never runs the program |
| Which specific input triggered the bug | Cannot reason about runtime inputs |
| The full execution trace to the crash | No dynamic instrumentation |
| Why GetMethod() returned null | Cannot observe runtime reflection behaviour |

---

## 9. Results on MicrosoftBugDemo

The `MicrosoftBugDemo` project contains a known `NullReferenceException` crash
on `MemberMethodResolver.cs:18`. SonarQube scanned the project with 470+ rules
and returned:

```
0 issues found
```

The bug only manifests at runtime when `GetMethod()` is called with
`BindingFlags.DeclaredOnly` on an inherited member — a condition that static
analysis cannot observe without executing the program.

You can verify the real crash exists by running:
```powershell
dotnet test "C:\Patching\Patching\MicrosoftBugDemo\MicrosoftBugDemo.slnx" --logger "console;verbosity=detailed"
```

This will show the `NullReferenceException` on `MemberMethodResolver.cs:18`
that SonarQube completely missed.

The Autorepair tool caught it by instrumenting the running program and recording
the exact null assignment, the input that caused it, and the full execution
trace leading to the crash.

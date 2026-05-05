# Semgrep Setup Guide for C# Projects

Semgrep is a free, open-source static analysis tool. It scans your source code
for bug patterns without running the program. This guide covers installation,
running built-in rules, and writing your own custom rule.

---

## 1. Install Semgrep

**Prerequisites:** Python 3.8+ must be installed.
Check with:
```
python --version
```

**Install Semgrep via pip:**
```
pip install semgrep
```

**Verify installation:**
```
semgrep --version
```

If `semgrep` is not recognised after install, add the Python Scripts folder to PATH.
Run this once in PowerShell, then open a new terminal:
```
[System.Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\Users\YourName\AppData\Local\Python\pythoncore-3.14-64\Scripts", "User")
```
Replace `YourName` with your Windows username.

---

## 2. Run Built-in C# Rules

Semgrep has a registry of community rules. Run the C# rule pack against your project:
```
semgrep scan --config p/csharp "C:\path\to\your\project"
```

Example output:
```
Scanning 3 files with 27 csharp rules.
Ran 27 rules on 3 files: 0 findings.
```

If it finds nothing, it means the built-in rules did not match any known patterns
in your code. This is common for logic bugs and reflection-based null dereferences —
the built-in rules focus on security patterns.

---

## 3. Write a Custom Rule (YAML)

Semgrep rules are written in YAML. Each rule has:
- `id` — a unique name for the rule
- `pattern` — the code pattern to search for
- `message` — what to tell the developer when it matches
- `languages` — which language to apply it to
- `severity` — ERROR, WARNING, or INFO

**Example rule file: `semgrep-nullref.yaml`**

```yaml
rules:
  - id: csharp-getmethod-null-dereference
    patterns:
      - pattern: |
          $METHOD = $OBJ.GetType().GetMethod(...);
          ...
          $METHOD.Invoke(...);
    message: >
      '$METHOD' is assigned from GetMethod() which returns null when no matching
      method is found. Calling .Invoke() on a null MethodInfo throws
      NullReferenceException at runtime. Add a null check before invoking.
    languages: [csharp]
    severity: ERROR
    metadata:
      category: correctness
      cwe: CWE-476

  - id: csharp-reflection-null-forgiving
    pattern: |
      MethodInfo $M = $X.GetType().GetMethod(..., ...)!;
    message: >
      The null-forgiving operator '!' suppresses the compiler warning but does
      not prevent a NullReferenceException at runtime if GetMethod() returns
      null. Remove '!' and add an explicit null check.
    languages: [csharp]
    severity: WARNING
    metadata:
      category: correctness
      cwe: CWE-476
```

**How the pattern works:**
- `$METHOD`, `$OBJ`, `$M`, `$X` are metavariables — they match any identifier
- `...` means "any code in between"
- `(...)` means "any arguments"
- Semgrep matches this pattern across your entire codebase

---

## 4. Run Your Custom Rule

```
semgrep scan --config "C:\Patching\Patching\semgrep-nullref.yaml" "C:\path\to\your\project"
```

Save results to JSON for comparison:
```
semgrep scan --config "C:\Patching\Patching\semgrep-nullref.yaml" "C:\path\to\your\project" --json > semgrep-results.json
```

---

## 5. Full Command Reference

| Task | Command |
|---|---|
| Check version | `semgrep --version` |
| Scan with built-in C# rules | `semgrep scan --config p/csharp "C:\your\project"` |
| Scan with custom rule file | `semgrep scan --config semgrep-nullref.yaml "C:\your\project"` |
| Save output as JSON | `semgrep scan --config p/csharp "C:\your\project" --json > results.json` |
| Verbose output (see skipped files) | `semgrep scan --config p/csharp "C:\your\project" --verbose` |

---

## 6. What Semgrep Can and Cannot Tell You

| What Semgrep reports | Example |
|---|---|
| File and line number of the match | `MemberMethodResolver.cs:13` |
| The matched code pattern | `GetMethod()` result used without null check |
| Rule ID and severity | `csharp-getmethod-null-dereference — ERROR` |

| What Semgrep cannot report | Why |
|---|---|
| The actual value that was null at runtime | It never runs the program |
| Which specific input triggered the bug | Static analysis only sees code patterns |
| The full execution trace leading to the crash | No runtime instrumentation |

This is the key difference from a dynamic analyser like the Autorepair tool,
which observes the program running and reports the exact variable value, origin
line, and execution path that caused the failure.

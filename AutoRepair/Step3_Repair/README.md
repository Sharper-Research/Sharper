# Step 3: Patch Generation

> **Status: Done.** The repair engine reads Step 2's `violations.json`, generates source-level patches using Roslyn, and can optionally apply them to the target project.

This step reads constraint violations produced by Step 2, maps each violation back to the original source code, selects an appropriate repair strategy, and generates (or applies) source-level patches.

---

## How It Works

1. **Violation ingestion**: `Program.cs` reads `violations.json` via `System.CommandLine` options.
2. **Source resolution**: `PatchEngine` locates the original source files (preferring `.bak` backups from Step 1 to work on uninstrumented code).
3. **Strategy dispatch**: Each violation is matched to a `IRepairStrategy` implementation. Strategies are tried in priority order (most specific first).
4. **Roslyn analysis**: Each strategy parses the source with Roslyn, finds the exact syntax node at the violation line, and generates a patched replacement.
5. **Patch application**: In `--apply` mode, patches are written back to the source files. In dry-run mode (default), only the report is generated.
6. **Reporting**: A full `patches.json` report is written with original code, patched code, and metadata for every patch.

---

## Repair Strategies

| Strategy | Handles | Repair |
| :--- | :--- | :--- |
| `OffByOneRepairStrategy` | `OffByOne(...)` | Rewrites `<=` loop conditions to `<`, or adds bounds guards. |
| `BufferBoundsRepairStrategy` | `BufferInBounds(...)` | Wraps array accesses in `if (index >= 0 && index < array.Length)` guards. |
| `NullDereferenceRepairStrategy` | `NotNull(...)` | Inserts `if (receiver is not null)` guards before member accesses. |
| `InvalidCastRepairStrategy` | `ValidCast(...)` | Replaces `(T)expr` with `is T` pattern matches; flags unsafe `as` casts. |
| `IntegerOverflowRepairStrategy` | `NoOverflow(...)` | Wraps arithmetic in `checked { }` blocks to make overflow explicit. |
| `ExceptionRepairStrategy` | `NoUnhandledException(...)` | Wraps statements in `try-catch` with diagnostic logging. |

Strategies are evaluated in priority order — `OffByOneRepairStrategy` runs before `BufferBoundsRepairStrategy` so that the more specific loop-condition fix is preferred over a generic bounds guard.

### Example Patch (from `patches.json`)

```json
{
  "file": "Worker.cs",
  "line": 14,
  "ruleName": "BufferBoundsRepair",
  "description": "Added bounds check guard for array access",
  "originalCode": "buffer[i] = i + 1;",
  "patchedCode": "if (i >= 0 && i < buffer.Length)\n{\n    buffer[i] = i + 1;\n}",
  "violation": { ... }
}
```

---

## Usage

```powershell
# From repo root — dry-run (report only)
.\AutoRepair\Step3_Repair\generate_patches.ps1

# Apply patches to source files
.\AutoRepair\Step3_Repair\generate_patches.ps1 -Apply

# Custom paths
.\AutoRepair\Step3_Repair\generate_patches.ps1 `
    -ViolationsPath "path\to\violations.json" `
    -TargetSourceDir "path\to\source" `
    -Apply
```

> **Prerequisite:** Run Steps 1 and 2 first so `violations.json` exists.

---

## Modes

| Mode | Flag | Behaviour |
| :--- | :--- | :--- |
| **Dry-run** | _(default)_ | Generates `patches.json` with all proposed changes. Source files are NOT modified. |
| **Apply** | `-Apply` | Generates report AND writes patches to source files. |

---

## Layout

```text
AutoRepair/Step3_Repair/
├── README.md                           # You are here
├── generate_patches.ps1                # Builds & runs the patch generator
├── reports/
│   └── patches.json                    # (Generated) patch report
└── Repair/                             # Patch generator source code (C# console app)
    ├── Repair.csproj                   # Targets net10.0, uses Roslyn + System.CommandLine
    ├── Program.cs                      # Entry point, CLI wiring
    ├── Models/
    │   ├── ConstraintViolation.cs      # Mirrors Step 2's violation shape
    │   ├── PatchResult.cs              # Single patch record
    │   └── PatchReport.cs              # Full report envelope
    ├── Core/
    │   └── PatchEngine.cs              # Orchestrates resolution + strategy dispatch
    └── Strategies/
        ├── IRepairStrategy.cs          # Strategy contract
        ├── OffByOneRepairStrategy.cs   # <= → < loop fixes + bounds guards
        ├── BufferBoundsRepairStrategy.cs  # Array bounds checking
        ├── NullDereferenceRepairStrategy.cs  # Null guards
        ├── InvalidCastRepairStrategy.cs    # Safe cast patterns
        ├── IntegerOverflowRepairStrategy.cs  # Checked arithmetic
        └── ExceptionRepairStrategy.cs  # Try-catch wrapping
```

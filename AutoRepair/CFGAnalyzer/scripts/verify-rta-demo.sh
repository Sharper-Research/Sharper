#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

DEMO_CSPROJ="$REPO_ROOT/samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj"
PROBE_CSPROJ="$REPO_ROOT/samples/RtaNarrowingDemo/EdgeProbe/EdgeProbe.csproj"

TMP_OUTPUT="$(mktemp)"
trap 'rm -f "$TMP_OUTPUT"' EXIT

dotnet build "$DEMO_CSPROJ" >/dev/null
dotnet run --project "$PROBE_CSPROJ" -- "$DEMO_CSPROJ" > "$TMP_OUTPUT"

grep -F "RtaNarrowingDemo.WorkerA.Run()" "$TMP_OUTPUT" >/dev/null
grep -F "RtaNarrowingDemo.WorkerA.Tick()" "$TMP_OUTPUT" >/dev/null

if grep -F "RtaNarrowingDemo.WorkerB.Run()" "$TMP_OUTPUT" >/dev/null; then
  echo "FAIL: unexpected WorkerB.Run edge found"
  cat "$TMP_OUTPUT"
  exit 1
fi

if grep -F "RtaNarrowingDemo.WorkerB.Tick()" "$TMP_OUTPUT" >/dev/null; then
  echo "FAIL: unexpected WorkerB.Tick edge found"
  cat "$TMP_OUTPUT"
  exit 1
fi

echo "PASS: RTA demo edges are narrowed to WorkerA"
cat "$TMP_OUTPUT"


#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

DEMO_CSPROJ="$REPO_ROOT/samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj"
LAMBDA_DEMO_CSPROJ="$REPO_ROOT/samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj"
PROBE_CSPROJ="$REPO_ROOT/samples/RtaNarrowingDemo/EdgeProbe/EdgeProbe.csproj"

TMP_RTA_OUTPUT="$(mktemp)"
TMP_LAMBDA_OUTPUT="$(mktemp)"
trap 'rm -f "$TMP_RTA_OUTPUT" "$TMP_LAMBDA_OUTPUT"' EXIT

dotnet build "$DEMO_CSPROJ" >/dev/null
dotnet run --project "$PROBE_CSPROJ" -- "$DEMO_CSPROJ" > "$TMP_RTA_OUTPUT"

grep -F "RtaNarrowingDemo.WorkerA.Run()" "$TMP_RTA_OUTPUT" >/dev/null
grep -F "RtaNarrowingDemo.WorkerA.Tick()" "$TMP_RTA_OUTPUT" >/dev/null

if grep -F "RtaNarrowingDemo.WorkerB.Run()" "$TMP_RTA_OUTPUT" >/dev/null; then
  echo "FAIL: unexpected WorkerB.Run edge found"
  cat "$TMP_RTA_OUTPUT"
  exit 1
fi

if grep -F "RtaNarrowingDemo.WorkerB.Tick()" "$TMP_RTA_OUTPUT" >/dev/null; then
  echo "FAIL: unexpected WorkerB.Tick edge found"
  cat "$TMP_RTA_OUTPUT"
  exit 1
fi

dotnet build "$LAMBDA_DEMO_CSPROJ" >/dev/null
dotnet run --project "$PROBE_CSPROJ" -- "$LAMBDA_DEMO_CSPROJ" > "$TMP_LAMBDA_OUTPUT"

grep -F "LocalWork()" "$TMP_LAMBDA_OUTPUT" >/dev/null
grep -F "lambda expression" "$TMP_LAMBDA_OUTPUT" >/dev/null

if grep -F "NeverCalled" "$TMP_LAMBDA_OUTPUT" >/dev/null; then
  echo "FAIL: unexpected NeverCalled edge found"
  cat "$TMP_LAMBDA_OUTPUT"
  exit 1
fi

echo "PASS: RTA demo edges are narrowed to WorkerA"
cat "$TMP_RTA_OUTPUT"
echo "PASS: Lambda/local-function demo resolution is present"
cat "$TMP_LAMBDA_OUTPUT"


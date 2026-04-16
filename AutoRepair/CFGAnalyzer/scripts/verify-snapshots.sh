#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TEST_PROJECT="$REPO_ROOT/CFGAnalyzer.Tests/CFGAnalyzer.Tests.csproj"
SNAPSHOT_DIR="$REPO_ROOT/CFGAnalyzer.Tests/Snapshots"

find "$SNAPSHOT_DIR" -name '*.received.json' -delete

dotnet test "$TEST_PROJECT" --filter "FullyQualifiedName~CallGraphSnapshotTests"

if find "$SNAPSHOT_DIR" -name '*.received.json' -print -quit | grep -q .; then
  echo "FAIL: snapshot mismatches were written to .received.json files"
  exit 1
fi

echo "PASS: callgraph snapshots match approved outputs"


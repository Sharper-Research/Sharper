@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"
set "TEST_PROJECT=%REPO_ROOT%\CFGAnalyzer.Tests\CFGAnalyzer.Tests.csproj"
set "SNAPSHOT_DIR=%REPO_ROOT%\CFGAnalyzer.Tests\Snapshots"

del /q "%SNAPSHOT_DIR%\*.received.json" >nul 2>&1

dotnet test "%TEST_PROJECT%" --filter "FullyQualifiedName~CallGraphSnapshotTests"
if errorlevel 1 goto :fail

dir /b "%SNAPSHOT_DIR%\*.received.json" >nul 2>&1
if not errorlevel 1 goto :mismatch

echo PASS: callgraph snapshots match approved outputs
exit /b 0

:mismatch
echo FAIL: snapshot mismatches were written to .received.json files
exit /b 1

:fail
echo FAIL: snapshot verification failed
exit /b 1


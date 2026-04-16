@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"

set "DEMO_CSPROJ=%REPO_ROOT%\samples\RtaNarrowingDemo\RtaNarrowingDemo.csproj"
set "PROBE_CSPROJ=%REPO_ROOT%\samples\RtaNarrowingDemo\EdgeProbe\EdgeProbe.csproj"
set "TMP_OUTPUT=%TEMP%\rta-demo-%RANDOM%.txt"

dotnet build "%DEMO_CSPROJ%" >nul
if errorlevel 1 goto :fail

dotnet run --project "%PROBE_CSPROJ%" -- "%DEMO_CSPROJ%" > "%TMP_OUTPUT%"
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerA.Run()" "%TMP_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerA.Tick()" "%TMP_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerB.Run()" "%TMP_OUTPUT%" >nul
if not errorlevel 1 goto :unexpected_b

findstr /C:"RtaNarrowingDemo.WorkerB.Tick()" "%TMP_OUTPUT%" >nul
if not errorlevel 1 goto :unexpected_b

echo PASS: RTA demo edges are narrowed to WorkerA
type "%TMP_OUTPUT%"
del "%TMP_OUTPUT%" >nul 2>&1
exit /b 0

:unexpected_b
echo FAIL: unexpected WorkerB edge found
type "%TMP_OUTPUT%"
del "%TMP_OUTPUT%" >nul 2>&1
exit /b 1

:fail
echo FAIL: demo verification failed
del "%TMP_OUTPUT%" >nul 2>&1
exit /b 1


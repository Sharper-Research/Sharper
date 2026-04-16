@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"

set "DEMO_CSPROJ=%REPO_ROOT%\samples\RtaNarrowingDemo\RtaNarrowingDemo.csproj"
set "LAMBDA_DEMO_CSPROJ=%REPO_ROOT%\samples\LambdaLocalFunctionDemo\LambdaLocalFunctionDemo.csproj"
set "PROBE_CSPROJ=%REPO_ROOT%\samples\RtaNarrowingDemo\EdgeProbe\EdgeProbe.csproj"
set "TMP_RTA_OUTPUT=%TEMP%\rta-demo-%RANDOM%.txt"
set "TMP_LAMBDA_OUTPUT=%TEMP%\lambda-demo-%RANDOM%.txt"

dotnet build "%DEMO_CSPROJ%" >nul
if errorlevel 1 goto :fail

dotnet run --project "%PROBE_CSPROJ%" -- "%DEMO_CSPROJ%" > "%TMP_RTA_OUTPUT%"
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerA.Run()" "%TMP_RTA_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerA.Tick()" "%TMP_RTA_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"RtaNarrowingDemo.WorkerB.Run()" "%TMP_RTA_OUTPUT%" >nul
if not errorlevel 1 goto :unexpected_b

findstr /C:"RtaNarrowingDemo.WorkerB.Tick()" "%TMP_RTA_OUTPUT%" >nul
if not errorlevel 1 goto :unexpected_b

dotnet build "%LAMBDA_DEMO_CSPROJ%" >nul
if errorlevel 1 goto :fail

dotnet run --project "%PROBE_CSPROJ%" -- "%LAMBDA_DEMO_CSPROJ%" > "%TMP_LAMBDA_OUTPUT%"
if errorlevel 1 goto :fail

findstr /C:"LocalWork()" "%TMP_LAMBDA_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"lambda expression" "%TMP_LAMBDA_OUTPUT%" >nul
if errorlevel 1 goto :fail

findstr /C:"NeverCalled" "%TMP_LAMBDA_OUTPUT%" >nul
if not errorlevel 1 goto :unexpected_lambda

echo PASS: RTA demo edges are narrowed to WorkerA
type "%TMP_RTA_OUTPUT%"
echo PASS: Lambda/local-function demo resolution is present
type "%TMP_LAMBDA_OUTPUT%"
del "%TMP_RTA_OUTPUT%" >nul 2>&1
del "%TMP_LAMBDA_OUTPUT%" >nul 2>&1
exit /b 0

:unexpected_b
echo FAIL: unexpected WorkerB edge found
type "%TMP_RTA_OUTPUT%"
del "%TMP_RTA_OUTPUT%" >nul 2>&1
del "%TMP_LAMBDA_OUTPUT%" >nul 2>&1
exit /b 1

:unexpected_lambda
echo FAIL: unexpected NeverCalled edge found
type "%TMP_LAMBDA_OUTPUT%"
del "%TMP_RTA_OUTPUT%" >nul 2>&1
del "%TMP_LAMBDA_OUTPUT%" >nul 2>&1
exit /b 1

:fail
echo FAIL: demo verification failed
del "%TMP_RTA_OUTPUT%" >nul 2>&1
del "%TMP_LAMBDA_OUTPUT%" >nul 2>&1
exit /b 1


@echo off
REM Runs unit tests for changed projects

setlocal enabledelayedexpansion

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Running unit tests for changed projects...

REM Run only tests that correspond to changed files
REM For now, run all tests - can be optimized to only changed projects

dotnet test "!RepoRoot!" --no-build -q --logger "console;verbosity=minimal"

if errorlevel 1 (
    echo.
    echo Error: Tests failed
    exit /b 1
)

echo.
echo Success: All tests passed
exit /b 0

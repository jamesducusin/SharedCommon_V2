@echo off
REM Validates the architecture of the solution using architecture tests

setlocal enabledelayedexpansion

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Running architecture validation tests...
dotnet test "!RepoRoot!\tests\SharedCommon.ArchitectureTests" --no-build -q

if errorlevel 1 (
    echo.
    echo Error: Architecture validation failed
    exit /b 1
)

echo Success: Architecture validation passed
exit /b 0

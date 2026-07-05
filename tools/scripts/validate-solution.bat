@echo off
REM Validates the entire solution compiles and tests pass

setlocal enabledelayedexpansion

for %%A in ("%~dp0..") do set "RepoRoot=%%~fA"

echo === Validating Solution ===
echo.

REM Step 1: Restore
echo 1. Restoring packages...
dotnet restore "!RepoRoot!" >nul 2>&1
if errorlevel 1 (
    echo Error: Package restore failed
    exit /b 1
)
echo    + Restore successful

REM Step 2: Build
echo.
echo 2. Building solution...
dotnet build "!RepoRoot!" --no-restore -q >nul 2>&1
if errorlevel 1 (
    echo Error: Build failed
    exit /b 1
)
echo    + Build successful

REM Step 3: Run tests
echo.
echo 3. Running unit tests...
dotnet test "!RepoRoot!" --no-build -q --logger "console;verbosity=minimal" >nul 2>&1
if errorlevel 1 (
    echo Error: Tests failed
    exit /b 1
)
echo    + Tests passed

REM Step 4: Architecture tests
echo.
echo 4. Running architecture validation...
dotnet test "!RepoRoot!\tests\SharedCommon.ArchitectureTests" --no-build -q >nul 2>&1
if errorlevel 1 (
    echo Error: Architecture validation failed
    exit /b 1
)
echo    + Architecture validation passed

echo.
echo === Solution Validation Complete ===
echo.

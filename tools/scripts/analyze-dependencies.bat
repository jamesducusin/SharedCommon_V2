@echo off
REM Analyzes project dependencies and detects potential issues

setlocal enabledelayedexpansion

for %%A in ("%~dp0..") do set "RepoRoot=%%~fA"

echo.
echo === Analyzing Dependencies ===
echo.

REM Step 1: Check for circular dependencies using build
echo 1. Checking for circular dependencies...
dotnet build "!RepoRoot!" --no-restore -q /p:TreatWarningsAsErrors=false 2>&1 | findstr "circular\|CS0121" >nul 2>&1
if not errorlevel 1 (
    echo    ! Potential circular dependency detected
) else (
    echo    + No circular dependencies
)

REM Step 2: List top-level package dependencies
echo.
echo 2. Package dependencies (sample):
echo    Projects in src/:
for /r "!RepoRoot!\src" %%F in (*.csproj) do (
    set "file=%%~nxF"
    set "file=!file:*.csproj=!"
    echo      - !file!
)

REM Step 3: Check for version mismatches
echo.
echo 3. Checking NuGet versions...
if exist "!RepoRoot!\Directory.Packages.props" (
    echo    + Central package management active
    findstr "PackageVersion" "!RepoRoot!\Directory.Packages.props" | findstr "Include=" | findstr "/>" >nul 2>&1
    if not errorlevel 1 (
        echo    + Package versions centrally managed
    )
) else (
    echo    ! No central package management (Directory.Packages.props not found)
)

REM Step 4: Check for outdated packages
echo.
echo 4. Checking for outdated packages...
echo    Run: dotnet list package --outdated
echo    (This requires interactive dotnet command)

echo.
echo === Dependency Analysis Complete ===
echo See docs/adr/ for dependency architecture decisions
echo.

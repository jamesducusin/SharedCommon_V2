@echo off
REM Validates no forbidden dependency relationships exist

setlocal enabledelayedexpansion

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Analyzing dependency graph...

REM Build and check for errors
dotnet build "!RepoRoot!" --no-restore /p:TreatWarningsAsErrors=true >nul 2>&1

if errorlevel 1 (
    echo.
    echo Error: Dependency validation failed
    echo See: docs/architecture/dependency-rules.md
    exit /b 1
)

REM Check for forbidden cross-references in csproj files
set "ForbiddenCount=0"

for /r "!RepoRoot!\src" %%F in (*.csproj) do (
    set "file=%%F"
    set "filename=%%~nF"
    
    REM Check if SharedCommon.Core references forbidden packages
    if "!filename:~0,18!"=="SharedCommon.Core" (
        findstr "SharedCommon.Infrastructure" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Error: SharedCommon.Core cannot reference SharedCommon.Infrastructure
            set /a "ForbiddenCount+=1"
        )
        
        findstr "SharedCommon.Auth" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Error: SharedCommon.Core cannot reference SharedCommon.Auth
            set /a "ForbiddenCount+=1"
        )
        
        findstr "SharedCommon.Caching" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Error: SharedCommon.Core cannot reference SharedCommon.Caching
            set /a "ForbiddenCount+=1"
        )
    )
)

if !ForbiddenCount! gtr 0 (
    echo.
    echo Error: Found !ForbiddenCount! forbidden dependency violation(s)
    exit /b 1
)

echo Success: Dependency check passed
exit /b 0

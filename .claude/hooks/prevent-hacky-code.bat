@echo off
REM Prevents common anti-patterns and code smells

setlocal enabledelayedexpansion

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Checking for code anti-patterns...

set "ErrorCount=0"

REM Check 1: No TODO comments in production code
for /r "!RepoRoot!\src" %%F in (*.cs) do (
    findstr /I "TODO\|HACK\|FIXME" "%%F" >nul 2>&1
    if not errorlevel 1 (
        echo Warning: Found TODO/HACK/FIXME in %%~nxF
        set /a "ErrorCount+=1"
    )
)

REM Check 2: No Thread.Sleep in production code (use await/Task.Delay)
for /r "!RepoRoot!\src" %%F in (*.cs) do (
    findstr "Thread\.Sleep" "%%F" >nul 2>&1
    if not errorlevel 1 (
        echo Error: Thread.Sleep found in %%~nxF - use await Task.Delay instead
        set /a "ErrorCount+=1"
    )
)

REM Check 3: No hardcoded secrets
for /r "!RepoRoot!\src" %%F in (*.cs) do (
    findstr /I "password.*=.*\"" "%%F" >nul 2>&1
    if not errorlevel 1 (
        echo Error: Hardcoded password found in %%~nxF
        set /a "ErrorCount+=1"
    )
)

REM Check 4: No catch-all exceptions without logging
REM This is complex to detect with batch, so we'll skip for now

if !ErrorCount! gtr 0 (
    echo.
    echo Error: Found !ErrorCount! code smell(s)
    exit /b 1
)

echo Success: Code quality check passed
exit /b 0

@echo off
REM Validates and applies code formatting

setlocal enabledelayedexpansion

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Validating code format...

REM Check if formatting is needed
dotnet format "!RepoRoot!" --verify-no-changes --verbosity quiet >nul 2>&1

if errorlevel 1 (
    echo.
    echo Error: Code is not properly formatted
    echo.
    echo Run: dotnet format
    echo.
    exit /b 1
)

echo Success: Code formatting is correct
exit /b 0

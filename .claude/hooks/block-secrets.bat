@echo off
REM Blocks commits containing secrets (API keys, connection strings, tokens)

setlocal enabledelayedexpansion
setlocal

for %%A in ("%~dp0..\..\..") do set "RepoRoot=%%~fA"

echo Scanning for secrets...

set "SecretCount=0"

REM Get all staged files
for /f "delims=" %%F in ('git diff --cached --name-only 2^>nul') do (
    set "file=!RepoRoot!\%%F"
    
    if exist "!file!" (
        REM Look for common secret patterns
        findstr /I "password.*=.*['\"]" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Found password string in %%F
            set /a "SecretCount+=1"
        )
        
        findstr /I "api_key.*=.*['\"]" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Found API key in %%F
            set /a "SecretCount+=1"
        )
        
        findstr /I "aws_secret" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Found AWS secret in %%F
            set /a "SecretCount+=1"
        )
        
        findstr /I "BEGIN PRIVATE KEY" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Found private key in %%F
            set /a "SecretCount+=1"
        )
        
        findstr /I "AKIAIOSFODNN7EXAMPLE" "!file!" >nul 2>&1
        if not errorlevel 1 (
            echo Found credential example in %%F
            set /a "SecretCount+=1"
        )
    )
)

if !SecretCount! gtr 0 (
    echo.
    echo Error: Found !SecretCount! potential secret(s)
    echo Use environment variables or configuration files instead
    exit /b 1
)

echo Success: No secrets detected
exit /b 0

@echo off
REM Publishes all NuGet packages

setlocal enabledelayedexpansion

for %%A in ("%~dp0..") do set "RepoRoot=%%~fA"

if "%1"=="" (
    echo Usage: publish-all.bat [Release^|Debug]
    echo Default: Release
    set "Configuration=Release"
) else (
    set "Configuration=%1"
)

echo.
echo === Publishing NuGet Packages ===
echo Configuration: !Configuration!
echo.

REM Step 1: Clean previous builds
echo 1. Cleaning...
dotnet clean "!RepoRoot!" -c !Configuration! >nul 2>&1

REM Step 2: Build
echo.
echo 2. Building...
dotnet build "!RepoRoot!" -c !Configuration! --no-restore -q >nul 2>&1
if errorlevel 1 (
    echo Error: Build failed
    exit /b 1
)

REM Step 3: Pack packages
echo.
echo 3. Creating packages...
dotnet pack "!RepoRoot!" -c !Configuration! --no-build --output "!RepoRoot!\build\nupkg" -q >nul 2>&1
if errorlevel 1 (
    echo Error: Pack failed
    exit /b 1
)

REM Step 4: List created packages
echo.
echo Created packages:
for %%F in ("!RepoRoot!\build\nupkg\*.nupkg") do (
    echo    - %%~nxF
)

echo.
echo === Packages ready for publishing ===
echo To publish, use: dotnet nuget push build/nupkg/*.nupkg -k [API_KEY] -s https://api.nuget.org/v3/index.json
echo.

@echo off
REM First-time developer environment bootstrap
REM Sets up .NET SDK, Docker, infrastructure, and runs validation tests

setlocal enabledelayedexpansion

REM Get repo root (2 levels up from tools/scripts)
for %%A in ("%~dp0..\..") do set "RepoRoot=%%~fA"

echo.
echo === SharedCommon Dev Bootstrap ===
echo.

REM Step 1: Verify .NET SDK
echo 1. Verifying .NET SDK...
for /f "tokens=*" %%A in ('dotnet --version 2^>^&1') do set "DotNetVersion=%%A"
echo Found: !DotNetVersion!

echo !DotNetVersion! | findstr "^8\." >nul 2>&1
if errorlevel 1 (
    echo Error: .NET 8 SDK required. Install from https://dot.net
    exit /b 1
)
echo    + .NET !DotNetVersion!

REM Step 2: Verify Docker
echo.
echo 2. Verifying Docker...
docker info >nul 2>&1
if errorlevel 1 (
    echo    ! Warning: Docker not running. Start Docker Desktop before running integration tests.
) else (
    echo    + Docker running
)

REM Step 3: Start infrastructure
echo.
echo 3. Starting infrastructure services...
if exist "!RepoRoot!\infra\docker\docker-compose.yml" (
    docker-compose -f "!RepoRoot!\infra\docker\docker-compose.yml" up -d >nul 2>&1
    echo    + Infrastructure started (Redis, Kafka, Jaeger)
) else (
    echo    ! docker-compose.yml not found at infra/docker/
)

REM Step 4: Restore packages
echo.
echo 4. Restoring NuGet packages...
dotnet restore "!RepoRoot!" >nul 2>&1
if errorlevel 1 (
    echo Error: Package restore failed
    exit /b 1
)
echo    + Packages restored

REM Step 5: Build
echo.
echo 5. Building solution...
dotnet build "!RepoRoot!" -c Debug --no-restore -q >nul 2>&1
if errorlevel 1 (
    echo Error: Build failed
    exit /b 1
)
echo    + Build succeeded

REM Step 6: Run architecture tests
echo.
echo 6. Running architecture tests...
dotnet test "!RepoRoot!\tests\SharedCommon.ArchitectureTests" --no-build -q >nul 2>&1
if errorlevel 1 (
    echo Error: Architecture tests failed
    exit /b 1
)
echo    + Architecture tests passed

echo.
echo === Bootstrap complete! ===
echo Next: Read CLAUDE.md, then docs/guides/first-time-setup.md
echo.
endlocal

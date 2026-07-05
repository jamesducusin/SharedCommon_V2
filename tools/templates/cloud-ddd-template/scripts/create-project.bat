@echo off
REM Creates a new project from the Cloud-Ready DDD template
REM Usage: create-project.bat ProjectName [OutputPath]
REM Example: create-project.bat OrderService
REM Example: create-project.bat UserManagement C:\Projects

setlocal enabledelayedexpansion
set "ProjectName=%1"
set "OutputPath=%2"

if "!ProjectName!"=="" (
    echo Usage: create-project.bat ProjectName [OutputPath]
    echo Example: create-project.bat OrderService
    exit /b 1
)

if "!OutputPath!"=="" (
    set "OutputPath=."
)

REM Get script directory
set "ScriptDir=%~dp0"
for %%A in ("!ScriptDir!\.") do set "TemplateDir=%%~dpA"
for %%A in ("!TemplateDir!\.") do set "TemplateRootDir=%%~dpA"
set "TemplatePath=!TemplateRootDir!cloud-ddd-template"

if not exist "!TemplatePath!" (
    echo Error: Template directory not found: !TemplatePath!
    exit /b 1
)

set "ProjectPath=!OutputPath!\!ProjectName!"
if exist "!ProjectPath!" (
    echo Error: Project directory already exists: !ProjectPath!
    exit /b 1
)

echo.
echo Creating Cloud-Ready DDD Project: !ProjectName!
echo Location: !ProjectPath!
echo.

REM Step 1: Copy template
echo Copying template files...
if not exist "!ProjectPath!" mkdir "!ProjectPath!"
xcopy /E /I /Y "!TemplatePath!" "!ProjectPath!" >nul 2>&1
if errorlevel 1 (
    echo Error: Failed to copy template
    exit /b 1
)
echo Template copied

REM Step 2: Replace project names in files
echo Updating project names...
setlocal enabledelayedexpansion

for /r "!ProjectPath!" %%F in (*.cs *.csproj *.json *.sln *.md *.xml) do (
    set "file=%%F"
    
    REM Replace Templates with ProjectName
    (
        for /f "delims=" %%A in ('type "!file!"') do (
            set "line=%%A"
            setlocal enabledelayedexpansion
            set "line=!line:Templates=!ProjectName!"
            echo !line!
            endlocal
        )
    ) > "!file!.tmp"
    move /Y "!file!.tmp" "!file!" >nul 2>&1
)

echo Updated project files

REM Step 3: Rename directories
echo Renaming directories...
for /d /r "!ProjectPath!" %%D in (Templates.Domain) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.Domain"
        echo %%D renamed to !ProjectName!.Domain
    )
)
for /d /r "!ProjectPath!" %%D in (Templates.Application) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.Application"
        echo %%D renamed to !ProjectName!.Application
    )
)
for /d /r "!ProjectPath!" %%D in (Templates.Infrastructure) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.Infrastructure"
        echo %%D renamed to !ProjectName!.Infrastructure
    )
)
for /d /r "!ProjectPath!" %%D in (Templates.Api) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.Api"
        echo %%D renamed to !ProjectName!.Api
    )
)
for /d /r "!ProjectPath!" %%D in (Templates.UnitTests) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.UnitTests"
        echo %%D renamed to !ProjectName!.UnitTests
    )
)
for /d /r "!ProjectPath!" %%D in (Templates.IntegrationTests) do (
    if exist "%%D" (
        ren "%%D" "!ProjectName!.IntegrationTests"
        echo %%D renamed to !ProjectName!.IntegrationTests
    )
)

REM Step 4: Validate
echo.
echo Validating project structure...
set "ValidationPass=1"

if not exist "!ProjectPath!\src\!ProjectName!.Api\!ProjectName!.Api.csproj" (
    echo Missing: src\!ProjectName!.Api\!ProjectName!.Api.csproj
    set "ValidationPass=0"
) else (
    echo Found: src\!ProjectName!.Api\!ProjectName!.Api.csproj
)

if not exist "!ProjectPath!\src\!ProjectName!.Application\!ProjectName!.Application.csproj" (
    echo Missing: src\!ProjectName!.Application\!ProjectName!.Application.csproj
    set "ValidationPass=0"
) else (
    echo Found: src\!ProjectName!.Application\!ProjectName!.Application.csproj
)

if not exist "!ProjectPath!\src\!ProjectName!.Domain\!ProjectName!.Domain.csproj" (
    echo Missing: src\!ProjectName!.Domain\!ProjectName!.Domain.csproj
    set "ValidationPass=0"
) else (
    echo Found: src\!ProjectName!.Domain\!ProjectName!.Domain.csproj
)

if not exist "!ProjectPath!\src\!ProjectName!.Infrastructure\!ProjectName!.Infrastructure.csproj" (
    echo Missing: src\!ProjectName!.Infrastructure\!ProjectName!.Infrastructure.csproj
    set "ValidationPass=0"
) else (
    echo Found: src\!ProjectName!.Infrastructure\!ProjectName!.Infrastructure.csproj
)

if "!ValidationPass!"=="0" (
    echo.
    echo Error: Project validation failed. Some files are missing.
    exit /b 1
)

echo.
echo Success! Project created.
echo.
echo Next steps:
echo   1. cd !ProjectPath!
echo   2. dotnet restore
echo   3. dotnet build
echo.
endlocal

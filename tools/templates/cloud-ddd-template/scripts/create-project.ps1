#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a new project from the Cloud-Ready DDD template.

.DESCRIPTION
    Scaffolds a new microservice project with Clean Architecture,
    Domain-Driven Design, and Vertical Slice Architecture.
    Replaces all template names with the provided project name.

.PARAMETER ProjectName
    The name of the new project (e.g., "OrderService", "UserManagement").
    Must start with a letter and contain only alphanumeric characters.

.PARAMETER OutputPath
    The directory where the project will be created.
    Defaults to the current directory.

.EXAMPLE
    .\create-project.ps1 -ProjectName "OrderService"
    .\create-project.ps1 -ProjectName "UserManagement" -OutputPath "C:\Projects"

.NOTES
    Requires PowerShell 7.0+ (or Windows PowerShell 5.1+)
#>

param(
    [Parameter(Mandatory = $true, HelpMessage = "Project name (e.g., OrderService)")]
    [ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
    [string]$ProjectName,

    [Parameter(HelpMessage = "Output directory (defaults to current directory)")]
    [string]$OutputPath = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TemplateDir = Split-Path -Parent $ScriptDir
$TemplatePath = Join-Path $TemplateDir "cloud-ddd-template"

# Validate inputs
if (-not (Test-Path $TemplatePath)) {
    Write-Error "Template directory not found: $TemplatePath"
    exit 1
}

$ProjectPath = Join-Path $OutputPath $ProjectName
if (Test-Path $ProjectPath) {
    Write-Error "Project directory already exists: $ProjectPath"
    exit 1
}

Write-Host "🚀 Creating Cloud-Ready DDD Project: $ProjectName" -ForegroundColor Cyan
Write-Host "📁 Location: $ProjectPath" -ForegroundColor Cyan
Write-Host ""

try {
    # Step 1: Copy template
    Write-Host "📋 Copying template files..." -ForegroundColor Yellow
    Copy-Item -Path $TemplatePath -Destination $ProjectPath -Recurse -Force
    Write-Host "✓ Template copied" -ForegroundColor Green

    # Step 2: Replace project names in files
    Write-Host "🔄 Updating project names..." -ForegroundColor Yellow

    $ReplacementPairs = @(
        @{ Old = "Templates"; New = $ProjectName }
        @{ Old = "templates"; New = $ProjectName.ToLower() }
    )

    # Find all text files
    $FileExtensions = @("*.cs", "*.csproj", "*.json", "*.sln", "*.md", "*.xml")
    $FilesToUpdate = Get-ChildItem -Path $ProjectPath -Recurse -Include $FileExtensions -File

    $UpdateCount = 0
    foreach ($File in $FilesToUpdate) {
        $Content = Get-Content -Path $File.FullName -Raw -Encoding UTF8

        foreach ($Pair in $ReplacementPairs) {
            if ($Content -like "*$($Pair.Old)*") {
                $Content = $Content -replace $Pair.Old, $Pair.New
                $UpdateCount++
            }
        }

        Set-Content -Path $File.FullName -Value $Content -Encoding UTF8
    }

    Write-Host "✓ Updated $UpdateCount files" -ForegroundColor Green

    # Step 3: Rename directories
    Write-Host "📂 Renaming directories..." -ForegroundColor Yellow

    $DirectoriesToRename = @(
        @{ Old = "Templates.Domain"; New = "$ProjectName.Domain" }
        @{ Old = "Templates.Application"; New = "$ProjectName.Application" }
        @{ Old = "Templates.Infrastructure"; New = "$ProjectName.Infrastructure" }
        @{ Old = "Templates.Api"; New = "$ProjectName.Api" }
        @{ Old = "Templates.UnitTests"; New = "$ProjectName.UnitTests" }
        @{ Old = "Templates.IntegrationTests"; New = "$ProjectName.IntegrationTests" }
    )

    foreach ($DirPair in $DirectoriesToRename) {
        $OldPath = Get-ChildItem -Path $ProjectPath -Recurse -Directory -Filter $DirPair.Old -ErrorAction SilentlyContinue
        if ($OldPath) {
            $NewPath = Join-Path $OldPath.Parent.FullName $DirPair.New
            Rename-Item -Path $OldPath.FullName -NewName $DirPair.New -Force
            Write-Host "  ✓ $($DirPair.Old) → $($DirPair.New)" -ForegroundColor Cyan
        }
    }

    # Step 4: Validate
    Write-Host "🔍 Validating project structure..." -ForegroundColor Yellow

    $RequiredFiles = @(
        "src/$ProjectName.Api/$ProjectName.Api.csproj"
        "src/$ProjectName.Application/$ProjectName.Application.csproj"
        "src/$ProjectName.Domain/$ProjectName.Domain.csproj"
        "src/$ProjectName.Infrastructure/$ProjectName.Infrastructure.csproj"
        "src/$ProjectName.Api/Program.cs"
        "src/$ProjectName.Api/appsettings.json"
    )

    $AllValid = $true
    foreach ($File in $RequiredFiles) {
        $FilePath = Join-Path $ProjectPath $File
        if (Test-Path $FilePath) {
            Write-Host "  ✓ $File" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ $File (missing)" -ForegroundColor Red
            $AllValid = $false
        }
    }

    if (-not $AllValid) {
        Write-Error "Project validation failed. Some files are missing."
        exit 1
    }

    Write-Host ""
    Write-Host "✅ Project created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📌 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. cd $ProjectPath"
    Write-Host "  2. dotnet restore"
    Write-Host "  3. dotnet build"
    Write-Host "  4. Update appsettings.json with your database connection string"
    Write-Host "  5. cd src/$ProjectName.Infrastructure"
    Write-Host "  6. dotnet ef database update"
    Write-Host "  7. cd ../src/$ProjectName.Api"
    Write-Host "  8. dotnet run"
    Write-Host ""
    Write-Host "📖 For detailed setup instructions, see GETTING_STARTED.md" -ForegroundColor Cyan
    Write-Host ""

}
catch {
    Write-Error "Error creating project: $_"
    
    # Cleanup on error
    if (Test-Path $ProjectPath) {
        Remove-Item -Path $ProjectPath -Recurse -Force
        Write-Host "🧹 Cleaned up incomplete project" -ForegroundColor Yellow
    }
    exit 1
}

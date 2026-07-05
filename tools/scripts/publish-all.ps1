# Publish all SharedCommon packages to NuGet

param(
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$RepoRoot = (Get-Location).Path,
    [switch]$DryRun
)

if (-not $ApiKey -and -not $DryRun) {
    Write-Error "NUGET_API_KEY not set. Pass -ApiKey or set the environment variable."
    exit 1
}

# Run full validation before publishing
Write-Host "Running pre-publish validation..." -ForegroundColor Cyan
& "$RepoRoot\tools\scripts\validate-solution.ps1" -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) {
    Write-Error "Validation failed. Aborting publish."
    exit 1
}

# Pack all projects
Write-Host "`nPacking packages..." -ForegroundColor Cyan
dotnet pack "$RepoRoot" -c Release --no-build -o "$RepoRoot/.nupkg"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Pack failed."
    exit 1
}

# Publish each package
$packages = Get-ChildItem -Path "$RepoRoot/.nupkg" -Filter "SharedCommon.*.nupkg" | Where-Object { $_.Name -notmatch "symbols" }
Write-Host "`nFound $($packages.Count) packages to publish:" -ForegroundColor Cyan
$packages | ForEach-Object { Write-Host "  - $($_.Name)" }

if ($DryRun) {
    Write-Host "`n[DRY RUN] Would publish $($packages.Count) packages to $Source" -ForegroundColor Yellow
    exit 0
}

foreach ($package in $packages) {
    Write-Host "`nPublishing $($package.Name)..." -ForegroundColor Cyan
    dotnet nuget push $package.FullName --api-key $ApiKey --source $Source --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to publish $($package.Name)"
        exit 1
    }
}

Write-Host "`n✅ All packages published successfully." -ForegroundColor Green

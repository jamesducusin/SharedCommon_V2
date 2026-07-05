# Validates no forbidden dependency relationships exist

param(
    [string]$RepoRoot = (git rev-parse --show-toplevel)
)

$errors = @()

Write-Host "Analyzing dependency graph..."
$output = & dotnet build "$RepoRoot" --no-restore /p:TreatWarningsAsErrors=true 2>&1

# Check for known forbidden cross-references
$forbiddenEdges = @(
    @{ From = "SharedCommon.Core"; To = "SharedCommon.Infrastructure" },
    @{ From = "SharedCommon.Core"; To = "SharedCommon.Auth" },
    @{ From = "SharedCommon.Core"; To = "SharedCommon.Caching" }
)

$csprojFiles = Get-ChildItem -Path "$RepoRoot/src" -Filter "*.csproj" -Recurse
foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj.FullName -Raw
    $packageName = $csproj.BaseName

    foreach ($edge in $forbiddenEdges) {
        if ($packageName -eq $edge.From -and $content -match $edge.To) {
            $errors += "❌ Forbidden dependency: $($edge.From) → $($edge.To)"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Dependency validation failed:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" }
    Write-Host ""
    Write-Host "See: docs/architecture/dependency-rules.md"
    exit 1
}

Write-Host "✅ Dependency check passed" -ForegroundColor Green
exit 0

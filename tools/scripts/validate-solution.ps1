# Full solution validation suite

param(
    [string]$RepoRoot = (Get-Location).Path
)

$errors = @()
$warnings = @()

function Step($name, $block) {
    Write-Host "`n▶ $name..." -ForegroundColor Cyan
    & $block
    if ($LASTEXITCODE -ne 0) {
        $script:errors += "❌ $name failed"
        return $false
    }
    Write-Host "  ✅ $name passed" -ForegroundColor Green
    return $true
}

Step "Build (TreatWarningsAsErrors)" {
    dotnet build "$RepoRoot" -c Release /p:TreatWarningsAsErrors=true --no-restore -q
}

Step "Unit Tests" {
    dotnet test "$RepoRoot/tests/SharedCommon.UnitTests" --no-build -q
}

Step "Architecture Tests" {
    dotnet test "$RepoRoot/tests/SharedCommon.ArchitectureTests" --no-build -q
}

Step "Security Tests" {
    dotnet test "$RepoRoot/tests/SharedCommon.SecurityTests" --no-build -q
}

Step "Integration Tests" {
    dotnet test "$RepoRoot/tests/SharedCommon.IntegrationTests" --no-build -q
}

Step "Format Check" {
    dotnet format "$RepoRoot" --verify-no-changes -v quiet
}

Step "Vulnerability Scan" {
    dotnet list "$RepoRoot" package --vulnerable --include-transitive 2>&1 | Where-Object { $_ -match "has the following" }
}

Write-Host "`n═══════════════════════════════" -ForegroundColor White
if ($errors.Count -gt 0) {
    Write-Host "VALIDATION FAILED" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "ALL CHECKS PASSED ✅" -ForegroundColor Green
exit 0

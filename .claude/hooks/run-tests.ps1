# Runs tests relevant to staged changes before commit

param(
    [string]$RepoRoot = (git rev-parse --show-toplevel)
)

$stagedFiles = git diff --cached --name-only

# Determine which test projects are relevant
$testProjects = @()

if ($stagedFiles -match "SharedCommon\.Core") { $testProjects += "tests/SharedCommon.UnitTests" }
if ($stagedFiles -match "SharedCommon\.Logging") { $testProjects += "tests/SharedCommon.UnitTests" }
if ($stagedFiles -match "SharedCommon\.Security") { $testProjects += "tests/SharedCommon.SecurityTests" }
if ($stagedFiles -match ".*") { $testProjects += "tests/SharedCommon.ArchitectureTests" }

$testProjects = $testProjects | Sort-Object -Unique

foreach ($project in $testProjects) {
    Write-Host "Running $project..."
    & dotnet test "$RepoRoot/$project" --no-build -q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed: $project" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ All tests passed" -ForegroundColor Green
exit 0

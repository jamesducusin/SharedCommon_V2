# Prevents architectural violations before code enters repository

param(
    [string]$RepoRoot = (git rev-parse --show-toplevel)
)

$errors = @()

# Check 1: No circular dependencies
Write-Host "Checking for circular dependencies..."
$output = & dotnet build $RepoRoot --no-restore /p:TreatWarningsAsErrors=true 2>&1
if ($output -match "circular") {
    $errors += "❌ Circular dependencies detected"
}

# Check 2: Architecture layering
Write-Host "Running architecture tests..."
$archTests = & dotnet test "$RepoRoot/tests/SharedCommon.ArchitectureTests" --no-build -q
if ($LASTEXITCODE -ne 0) {
    $errors += "❌ Architecture tests failed"
}

# Check 3: No hardcoded secrets
Write-Host "Scanning for hardcoded secrets..."
$secrets = git diff --cached | Select-String -Pattern "(password|secret|api[_-]?key|token)" -ErrorAction SilentlyContinue
if ($secrets) {
    $errors += "❌ Potential hardcoded secrets detected"
    $secrets | ForEach-Object { Write-Host "   $_" }
}

# Check 4: Forbidden patterns
Write-Host "Checking for forbidden patterns..."
$stagedFiles = git diff --cached --name-only | Where-Object { $_ -like "*.cs" }
foreach ($file in $stagedFiles) {
    $content = git show ":$file"

    if ($content -match "Console\.WriteLine") {
        $errors += "❌ Console.WriteLine found in $file"
    }
    if ($content -match "catch\s*\(\s*\)\s*\{" -or $content -match "catch.*\{\s*\}") {
        $errors += "❌ Empty catch block found in $file"
    }
    if ($content -match "TODO.*hack|temporary fix|quick fix") {
        $errors += "❌ Hack comment found in $file"
    }
}

# Exit with failure if any errors
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Pre-commit validation failed:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" }
    Write-Host ""
    exit 1
}

Write-Host "✅ All checks passed" -ForegroundColor Green
exit 0

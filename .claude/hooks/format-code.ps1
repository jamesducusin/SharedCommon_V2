# Auto-formats staged .cs files before commit

param(
    [string]$RepoRoot = (git rev-parse --show-toplevel)
)

Write-Host "Formatting code..."
& dotnet format "$RepoRoot" --include (git diff --cached --name-only | Where-Object { $_ -like "*.cs" })

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ dotnet format failed" -ForegroundColor Red
    exit 1
}

# Re-stage any files that were reformatted
git diff --name-only | Where-Object { $_ -like "*.cs" } | ForEach-Object {
    git add $_
    Write-Host "  Re-staged: $_"
}

Write-Host "✅ Code formatted" -ForegroundColor Green
exit 0

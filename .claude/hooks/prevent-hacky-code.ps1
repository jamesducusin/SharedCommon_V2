# Rejects commits containing anti-patterns

$antiPatterns = @(
    @{ Pattern = 'Console\.Write(Line)?'; Message = "Console.Write* found — use ILogger" },
    @{ Pattern = 'Thread\.Sleep\('; Message = "Thread.Sleep found — use Task.Delay or proper async" },
    @{ Pattern = '\.Result\b'; Message = ".Result blocking call found — use await" },
    @{ Pattern = '\.Wait\(\)'; Message = ".Wait() blocking call found — use await" },
    @{ Pattern = 'catch\s*\{\s*\}'; Message = "Empty catch block found — handle or log the exception" },
    @{ Pattern = 'catch\s*\(Exception\)\s*\{\s*\}'; Message = "Swallowed exception — handle or log" },
    @{ Pattern = '// TODO.*hack'; Message = "Hack TODO comment — fix before committing" },
    @{ Pattern = '// FIXME'; Message = "FIXME comment — fix before committing" },
    @{ Pattern = 'static\s+\w+\s+\w+\s*=\s*new'; Message = "Static mutable state suspected — use DI" },
    @{ Pattern = 'ServiceLocator\.'; Message = "Service locator pattern — use DI instead" }
)

$errors = @()
$stagedFiles = git diff --cached --name-only | Where-Object { $_ -like "*.cs" }

foreach ($file in $stagedFiles) {
    $content = git show ":$file" 2>$null
    if (-not $content) { continue }

    foreach ($ap in $antiPatterns) {
        if ($content -match $ap.Pattern) {
            $errors += "❌ $($ap.Message) in $file"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Anti-pattern check failed:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" }
    Write-Host ""
    Write-Host "See: docs/standards/coding-standards.md"
    exit 1
}

Write-Host "✅ No anti-patterns detected" -ForegroundColor Green
exit 0

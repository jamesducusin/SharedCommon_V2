# Blocks commits containing hardcoded secrets

$secretPatterns = @(
    'password\s*=\s*"[^"]+"',
    'apikey\s*=\s*"[^"]+"',
    'api_key\s*=\s*"[^"]+"',
    'secret\s*=\s*"[^"]+"',
    'connectionstring\s*=\s*"[^"]+"',
    'bearer\s+[a-zA-Z0-9\-_\.]+',
    'eyJ[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+'  # JWT pattern
)

$errors = @()

$diff = git diff --cached
foreach ($pattern in $secretPatterns) {
    $matches = $diff | Select-String -Pattern $pattern -CaseSensitive:$false -ErrorAction SilentlyContinue
    if ($matches) {
        $errors += "❌ Potential secret detected (pattern: $pattern)"
        $matches | Select-Object -First 3 | ForEach-Object {
            Write-Host "   Line: $($_.Line.Trim())" -ForegroundColor Yellow
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Secret detection failed:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" }
    Write-Host ""
    Write-Host "Use User Secrets (dev) or environment variables (prod)."
    Write-Host "See: docs/standards/security-guidelines.md"
    exit 1
}

Write-Host "✅ No secrets detected" -ForegroundColor Green
exit 0

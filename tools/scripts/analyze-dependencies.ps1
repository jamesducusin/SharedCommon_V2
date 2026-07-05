# Analyzes and visualizes package dependency tree

param(
    [string]$RepoRoot = (Get-Location).Path,
    [switch]$OutputDot  # Output Graphviz DOT format
)

Write-Host "Analyzing SharedCommon dependency graph..." -ForegroundColor Cyan

$packages = Get-ChildItem -Path "$RepoRoot\src" -Filter "*.csproj" -Recurse

$edges = @()

foreach ($csproj in $packages) {
    $packageName = $csproj.BaseName
    $content = Get-Content $csproj.FullName -Raw

    $refs = [regex]::Matches($content, '<ProjectReference Include="[^"]*\\(SharedCommon\.[^\\]+)\\')
    foreach ($ref in $refs) {
        $dep = $ref.Groups[1].Value
        $edges += @{ From = $packageName; To = $dep }
    }
}

if ($OutputDot) {
    Write-Host "`ndigraph SharedCommon {"
    Write-Host "  rankdir=BT;"
    foreach ($edge in $edges) {
        Write-Host "  `"$($edge.From)`" -> `"$($edge.To)`";"
    }
    Write-Host "}"
} else {
    Write-Host "`nDependency edges:"
    foreach ($edge in $edges | Sort-Object From) {
        Write-Host "  $($edge.From) → $($edge.To)"
    }

    # Check for potential violations against dependency-rules.md
    $violations = $edges | Where-Object {
        ($_.From -eq "SharedCommon.Core" -and $_.To -ne "") -or
        ($_.From -eq "SharedCommon.Auth" -and $_.To -eq "SharedCommon.Caching")
    }

    if ($violations) {
        Write-Host "`n❌ Dependency violations detected:" -ForegroundColor Red
        $violations | ForEach-Object { Write-Host "  $($_.From) → $($_.To)" }
        Write-Host "See: docs/architecture/dependency-rules.md"
    } else {
        Write-Host "`n✅ No dependency violations found." -ForegroundColor Green
    }
}

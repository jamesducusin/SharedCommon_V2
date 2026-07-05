# First-time developer environment bootstrap

$RepoRoot = $PSScriptRoot | Split-Path | Split-Path

Write-Host "═══ SharedCommon Dev Bootstrap ═══" -ForegroundColor Cyan

# Step 1: Verify .NET SDK
Write-Host "`n1. Verifying .NET SDK..."
$dotnetVersion = (dotnet --version 2>&1)
if ($dotnetVersion -notmatch "^8\.") {
    Write-Error ".NET 8 SDK required. Install from https://dot.net"
    exit 1
}
Write-Host "   ✅ .NET $dotnetVersion"

# Step 2: Verify Docker
Write-Host "`n2. Verifying Docker..."
docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Docker not running. Start Docker Desktop before running integration tests."
} else {
    Write-Host "   ✅ Docker running"
}

# Step 3: Start infrastructure
Write-Host "`n3. Starting infrastructure services..."
if (Test-Path "$RepoRoot\infra\docker\docker-compose.yml") {
    docker-compose -f "$RepoRoot\infra\docker\docker-compose.yml" up -d 2>&1
    Write-Host "   ✅ Infrastructure started (Redis, Kafka, Jaeger)"
} else {
    Write-Warning "docker-compose.yml not found at infra/docker/"
}

# Step 4: Restore packages
Write-Host "`n4. Restoring NuGet packages..."
dotnet restore "$RepoRoot"
if ($LASTEXITCODE -ne 0) { Write-Error "Restore failed"; exit 1 }
Write-Host "   ✅ Packages restored"

# Step 5: Build
Write-Host "`n5. Building solution..."
dotnet build "$RepoRoot" -c Debug --no-restore -q
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }
Write-Host "   ✅ Build succeeded"

# Step 6: Run architecture tests
Write-Host "`n6. Running architecture tests..."
dotnet test "$RepoRoot\tests\SharedCommon.ArchitectureTests" --no-build -q
if ($LASTEXITCODE -ne 0) { Write-Error "Architecture tests failed"; exit 1 }
Write-Host "   ✅ Architecture tests passed"

Write-Host "`n═══ Bootstrap complete! ═══" -ForegroundColor Green
Write-Host "Next: Read CLAUDE.md, then docs/guides/first-time-setup.md"

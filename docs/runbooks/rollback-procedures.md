# Rollback Procedures

## Package Rollback

If a published package causes issues in consuming services:

```powershell
# 1. Unlist the bad version (it becomes invisible but existing consumers still work)
dotnet nuget delete SharedCommon.Caching 2.1.0 --source https://api.nuget.org/v3/index.json --non-interactive

# 2. Consuming services: pin to previous known-good version
# In Directory.Packages.props:
<PackageVersion Include="SharedCommon.Caching" Version="2.0.3" />

# 3. Publish hotfix patch
# Bump to 2.1.1 with fix, follow publishing runbook
```

## Git Rollback

If a bad commit reached main but no package was published:

```bash
# Find the last good commit
git log --oneline -20

# Create revert commit (safe — preserves history)
git revert <bad-commit-sha>
git push origin main
```

Do NOT use `git reset --hard` on shared branches.

## Configuration Rollback

If a configuration change caused the issue:

1. Identify the config change in git history
2. Revert the `appsettings.json` change
3. Redeploy configuration (Kubernetes ConfigMap update or environment variable change)
4. Verify service health checks recover

## Decision Tree

```
Is bad code in a published NuGet package?
  Yes → Unlist + pin consuming services + publish patch

Is bad code merged to main but not published?
  Yes → git revert + test + push

Is bad code in a feature branch?
  Yes → Delete branch, rework

Is bad configuration deployed?
  Yes → Revert config, redeploy
```

## After Rollback

- Always document what was rolled back and why in CHANGELOG.md
- Create a GitHub issue to track the root cause fix
- Do not re-publish the bad version number — use a new patch version

# Incident Response

## Severity Definitions

| Severity | Description | Response Time |
|----------|-------------|--------------|
| P1 | Service down, data loss | Immediate |
| P2 | Major feature broken | 1 hour |
| P3 | Minor degradation | Same day |
| P4 | Low-impact issue | Next sprint |

## Response Steps

### 1. Triage

```
- What is the user impact?
- Which packages/services are affected?
- What changed recently? (git log --since="2 hours ago")
- Check Grafana dashboards for anomalies
- Check Jaeger for trace errors
```

### 2. Contain

```
- Can we rollback to the previous package version?
- Can we disable the affected feature via config?
- Is a hotfix faster than a rollback?
```

### 3. Diagnose

```powershell
# Check recent logs with CorrelationId
# Search: level:Error AND correlationId:<id>

# Check which version is deployed
dotnet list package --include-transitive | grep SharedCommon
```

### 4. Fix and Verify

- Apply fix in a branch
- Run full test suite before deploying
- Monitor metrics for 15 minutes after deploy

### 5. Post-Incident

- Document what happened in CHANGELOG.md
- Create GitHub issue if caused by a bug
- If architectural — create ADR
- Update runbooks if response steps were unclear

## Useful Queries

```
# All errors in last hour
level:Error AND @timestamp:[now-1h TO now]

# Errors for specific correlation
correlationId:"abc-123" AND level:Error

# Slow operations (>500ms)
ElapsedMs:>500 AND level:Information
```

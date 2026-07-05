# Health Check Tuning & Best Practices Guide

## Overview

Health checks are critical for Kubernetes deployments, service discovery, and monitoring. This guide covers tuning health checks for different scenarios.

## Health Check Types

### 1. Liveness Probe (`/health/live`)

**Purpose**: Kubernetes restarts the pod if this fails
**Use Case**: Detect if the process is hung or crashed

**Configuration**:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 15     # Wait 15s for app to start
  periodSeconds: 10           # Check every 10s
  timeoutSeconds: 5           # Response must come in 5s
  failureThreshold: 3         # Restart after 3 failures
```

**Response**: Always returns 200 (unless process is hung/dead)

### 2. Readiness Probe (`/health/ready`)

**Purpose**: Kubernetes removes from load balancer if this fails
**Use Case**: Detect if app is not ready to serve traffic

**Configuration**:
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10     # Wait 10s for migrations/init
  periodSeconds: 5            # Check every 5s
  timeoutSeconds: 3           # Response must come in 3s
  failureThreshold: 3         # Remove from LB after 3 failures
```

**Response**: 200 if ready, 503 if not ready

### 3. Startup Probe (`/health/live`)

**Purpose**: Kubernetes waits before starting liveness checks
**Use Case**: Allow time for database migrations on first start

**Configuration**:
```yaml
startupProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 0
  periodSeconds: 10
  failureThreshold: 30        # Allow up to 5 minutes (30 * 10s)
```

---

## Tuning by Deployment Type

### Development (Single Pod)

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5      # Faster startup
  periodSeconds: 30           # Less frequent
  timeoutSeconds: 5
  failureThreshold: 5         # More lenient

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 2
  periodSeconds: 10
  timeoutSeconds: 3
  failureThreshold: 3
```

### Staging (3-5 Replicas)

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 15
  timeoutSeconds: 5
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

### Production (5-20+ Replicas)

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 20     # Allow full startup
  periodSeconds: 10           # Frequent checks
  timeoutSeconds: 5
  failureThreshold: 3         # Aggressive restart

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 15     # Enough for migrations
  periodSeconds: 5            # Very frequent
  timeoutSeconds: 5
  failureThreshold: 2         # Quick removal from LB
```

---

## Health Check Response Tuning

### Fast vs Thorough Checks

**Fast (liveness)**:
```csharp
public async Task<bool> IsLiveAsync()
{
    // Just check if process is responding
    // ~1ms
    return true;  // Process is alive
}
```

**Medium (readiness)**:
```csharp
public async Task<bool> IsReadyAsync()
{
    // Check database connectivity
    // ~10-50ms
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _dbConnection.OpenAsync(cts.Token);
        return true;
    }
    catch
    {
        return false;
    }
}
```

**Thorough (detailed)**:
```csharp
public async Task<HealthCheckResponse> CheckHealthAsync()
{
    // Check all dependencies
    // ~50-150ms
    var checks = new Dictionary<string, DependencyHealthStatus>();
    
    checks["database"] = await CheckDatabaseAsync();
    checks["cache"] = await CheckCacheAsync();
    checks["messaging"] = await CheckMessagingAsync();
    
    return new HealthCheckResponse(...)
}
```

---

## Database Connection Tuning

### Connection Pool Configuration

```csharp
// In appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Min Pool Size=5;Max Pool Size=100;"
  }
}
```

**Development**:
- Min Pool Size: 1
- Max Pool Size: 10
- Timeout: 30s

**Staging**:
- Min Pool Size: 5
- Max Pool Size: 50
- Timeout: 30s

**Production**:
- Min Pool Size: 10
- Max Pool Size: 100
- Timeout: 30s

### Health Check SQL Timeout

```csharp
// In HealthCheckService
private async Task<DependencyHealthStatus> CheckDatabaseAsync()
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        using var cts = new CancellationTokenSource(
            _configuration.GetValue<TimeSpan>("HealthChecks:DatabaseTimeout", 
                TimeSpan.FromSeconds(5)));
        
        await using var connection = new SqlConnection(_connectionString);
        connection.Open();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        cmd.CommandTimeout = 5;  // SQL Server timeout
        
        await cmd.ExecuteScalarAsync(cts.Token);
        
        sw.Stop();
        return new DependencyHealthStatus(
            "healthy",
            $"Connected to SQL Server ({sw.ElapsedMilliseconds}ms)",
            sw.ElapsedMilliseconds,
            null);
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new DependencyHealthStatus(
            "unhealthy",
            $"Database check failed: {ex.Message}",
            sw.ElapsedMilliseconds,
            new Dictionary<string, object> { { "error", ex.GetType().Name } });
    }
}
```

---

## Common Issues & Fixes

### Issue: Health Check Returns 503 on Startup

**Problem**: Database not ready when app starts

**Solution**: Increase `initialDelaySeconds` in readiness probe
```yaml
readinessProbe:
  initialDelaySeconds: 30  # Give migrations more time
```

### Issue: Too Many Restarts

**Problem**: Liveness probe too aggressive

**Solution**: Increase `failureThreshold` or `periodSeconds`
```yaml
livenessProbe:
  periodSeconds: 15        # Check less frequently
  failureThreshold: 5      # More failures allowed
```

### Issue: Health Check Timeout

**Problem**: Check takes longer than timeout

**Solution**: Increase timeout and reduce detail
```yaml
readinessProbe:
  timeoutSeconds: 10       # Increase from 3s
  periodSeconds: 10        # Check less frequently
```

### Issue: Database Connection Limit Exceeded

**Problem**: Health checks creating too many connections

**Solution**: Reuse connection from pool
```csharp
// Good: Use connection pool
using var connection = new SqlConnection(_connectionString);
await connection.OpenAsync(cts.Token);

// Avoid: Creating new connection each time
var connection = new SqlConnection("new-connection-string");
```

---

## Monitoring Health Check Metrics

### Prometheus Queries

```promql
# Health check success rate
rate(http_requests_total{endpoint="/health/live",status="200"}[5m])

# Health check latency (p95)
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{endpoint="/health/ready"}[5m]))

# Database connection pool usage
sql_connection_pool_usage{instance="templates-api"}

# Pod restart rate
rate(kube_pod_container_status_restarts_total[15m])
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "Health Checks",
    "panels": [
      {
        "title": "Liveness Success Rate",
        "targets": [
          {
            "expr": "rate(http_requests_total{endpoint=\"/health/live\",status=\"200\"}[5m])"
          }
        ]
      },
      {
        "title": "Readiness Check Latency",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{endpoint=\"/health/ready\"}[5m]))"
          }
        ]
      },
      {
        "title": "Database Connection Check Status",
        "targets": [
          {
            "expr": "templates_health_check_database_status"
          }
        ]
      }
    ]
  }
}
```

---

## Best Practices

1. **Keep liveness fast**: <5ms response time
   - Don't check external dependencies
   - Just verify process is alive

2. **Make readiness depend on critical resources only**:
   - Check database (required for all requests)
   - Skip optional services (cache, messaging)

3. **Use detailed health for monitoring only**:
   - Check all dependencies
   - Use for debugging issues
   - Don't use for Kubernetes probes

4. **Set appropriate timeouts**:
   - Liveness: 5s timeout
   - Readiness: 5s timeout
   - Detailed: 30s timeout

5. **Allow startup time**:
   - `initialDelaySeconds`: 15-30s
   - Database migrations need time
   - Caching frameworks initialize

6. **Tune failure thresholds**:
   - Liveness: 3 failures (aggressive restart)
   - Readiness: 2-3 failures (quick removal from LB)
   - Don't be too lenient (will miss problems)

7. **Use separate endpoints**:
   - `/health/live` - Liveness
   - `/health/ready` - Readiness
   - `/health/detailed` - Monitoring only

8. **Monitor the monitors**:
   - Track how often probes fail
   - Alert if restart rate too high
   - Alert if readiness check slow

---

## Configuration Examples

### appsettings.json

```json
{
  "HealthChecks": {
    "DatabaseTimeout": "00:00:05",
    "CacheTimeout": "00:00:03",
    "MessagingTimeout": "00:00:05",
    "UnhealthyThreshold": 2,
    "DegradedThreshold": 1,
    "DetailedHealthEnabled": true,
    "EnableDetailedDependencyInfo": false
  }
}
```

### appsettings.Production.json

```json
{
  "HealthChecks": {
    "DatabaseTimeout": "00:00:10",
    "CacheTimeout": "00:00:05",
    "MessagingTimeout": "00:00:10",
    "UnhealthyThreshold": 1,
    "DegradedThreshold": 0,
    "DetailedHealthEnabled": true,
    "EnableDetailedDependencyInfo": true
  }
}
```

---

## Health Score Calculation

```csharp
public int CalculateHealthScore(Dictionary<string, DependencyHealthStatus> checks)
{
    if (checks == null || checks.Count == 0)
        return 100;

    // Score: 100 = healthy, 50 = degraded, 0 = unhealthy
    var scores = checks.Values.Select(c => c.Status switch
    {
        "healthy" => 100,
        "degraded" => 50,
        "unhealthy" => 0,
        _ => 100
    }).ToList();

    return (int)scores.Average();
}
```

---

## Alerting Rules (Prometheus)

```yaml
groups:
- name: health_checks
  rules:
  - alert: HighHealthCheckFailureRate
    expr: rate(http_requests_total{endpoint="/health/live",status!="200"}[5m]) > 0.1
    for: 1m
    annotations:
      summary: "High health check failure rate"

  - alert: HealthyStatusFlapping
    expr: changes(templates_health_check_status[5m]) > 10
    for: 5m
    annotations:
      summary: "Health status changing too frequently"

  - alert: SlowReadinessCheck
    expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{endpoint="/health/ready"}[5m])) > 5
    for: 5m
    annotations:
      summary: "Readiness check is slow"
```

---

*Last Updated: 2024-01-15*
*Version: 1.0.0*

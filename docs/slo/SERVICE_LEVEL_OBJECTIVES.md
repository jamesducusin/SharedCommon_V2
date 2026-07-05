# SLO Definition: Templates API

## Service Level Objectives

**Service:** Templates API  
**Stakeholders:** Engineering, Product, Business  
**Review Cadence:** Monthly  
**Last Updated:** May 30, 2026

---

## Availability SLO

### Target: 99.9% (Four 9s)

**Definition:** Percentage of requests that receive a successful response (2xx/3xx HTTP status codes)

**Error Budget (Monthly):**
- **Total:** 43.2 minutes
- **Weekly:** ~10 minutes
- **Daily:** ~86 seconds

**How It's Measured:**
```promql
# Monthly availability
sum(increase(http_requests_total{status=~"2..|3.."}[30d]))
/
sum(increase(http_requests_total[30d]))
```

**Alert Thresholds:**
```yaml
- alert: AvailabilityWarning
  expr: (rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m])) > 0.001
  for: 5m
  
- alert: AvailabilityCritical
  expr: (rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m])) > 0.01
  for: 1m
```

---

## Latency SLOs

### P99 Latency: < 500ms

**Definition:** 99% of requests complete within 500ms

**How It's Measured:**
```promql
histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))
```

**Breakdown by Endpoint:**
| Endpoint | P99 Target | P95 Target |
|----------|-----------|-----------|
| GET /api/orders | 200ms | 100ms |
| POST /api/orders | 500ms | 300ms |
| GET /api/orders/{id} | 100ms | 50ms |
| DELETE /api/orders/{id} | 300ms | 150ms |

**Alert Thresholds:**
```yaml
- alert: HighLatencyWarning
  expr: histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m])) > 0.5
  for: 10m

- alert: HighLatencyCritical
  expr: histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m])) > 1.0
  for: 5m
```

---

## Error Rate SLO

### Target: < 0.1% errors

**Definition:** Errors / Total Requests < 0.1%

**How It's Measured:**
```promql
rate(http_requests_total{status=~"5.."}[5m])
/
rate(http_requests_total[5m])
```

**Alert:**
```yaml
- alert: ErrorRateHigh
  expr: (rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m])) > 0.001
  for: 5m
  annotations:
    summary: "Error rate {{ $value | humanizePercentage }} for {{ $labels.service }}"
```

---

## Dependency SLOs

### Database

**Availability:** 99.95%  
**P99 Query Latency:** < 100ms  
**Max Connection Pool:** 100  
**How Measured:** Health check queries every 10s

### Cache (Redis)

**Availability:** 99.9%  
**P99 Operation Latency:** < 5ms  
**Hit Rate Target:** > 80%  
**How Measured:** Cache metrics + Redis PING

### Message Queue (RabbitMQ)

**Availability:** 99.9%  
**Message Processing Latency:** < 1s  
**Queue Depth:** < 1000 messages  
**How Measured:** RabbitMQ metrics endpoint

---

## Service Level Indicators (SLIs)

### Request Success Rate

```
SLI = (Successful Requests) / (Total Requests)
Target: > 99.9%
```

**Implementation:**
```csharp
_telemetry.RecordMetric("http_requests_total", 1, new()
{
    { "method", request.Method },
    { "path", request.Path },
    { "status", response.StatusCode }
});

_telemetry.RecordMetric("http_request_duration_seconds", latencyMs / 1000.0, new()
{
    { "method", request.Method },
    { "path", request.Path },
    { "status", response.StatusCode }
});
```

### Request Latency Distribution

```
SLI = Latency Percentiles (P50, P95, P99)
Target: P99 < 500ms
```

**Measurement:**
```promql
histogram_quantile(0.50, rate(http_request_duration_seconds_bucket[5m]))  # P50
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))  # P95
histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))  # P99
```

### Dependency Health

```
SLI = (Dependencies Available) / (Total Dependencies)
Target: 100%
```

**Components:**
- Database: /health/detailed check every 10s
- Cache: Connectivity test on startup
- Message Queue: Consumer lag monitoring
- External APIs: Synthetic health checks

---

## Error Budget Policy

### Monthly Error Budget: 43.2 minutes (0.1% of 30 days)

**Consumption Rate:**

```
Available Errors = 43.2 * 60 = 2,592 seconds
Burn Rate = (Current Error Rate) / (0.1%)

If Burn Rate > 1.0: Error budget depleting faster than planned
If Burn Rate > 5.0: CRITICAL - take action immediately
```

**Budget Tracking:**

```promql
# Days remaining
(43.2 * 60 - on(job) increase(errors_total[30d]))
/
(43.2 * 60)
* 30
```

**Error Budget Actions:**

| Condition | Action | Timeline |
|-----------|--------|----------|
| Budget: 90-100% remaining | Normal operations | Continuous |
| Budget: 50-90% remaining | Increase testing | Ongoing |
| Budget: 10-50% remaining | Freeze features, focus on stability | This sprint |
| Budget: 0-10% remaining | DEFCON 1 - bugs only | Immediate |
| Budget: < 0 (overdrawn) | Post-mortem, preventive measures | After incident |

---

## Reporting

### Weekly SLO Review

```
Monday 9 AM standup:
- Availability: X.XX%
- P99 Latency: Xms
- Error Rate: X.XX%
- Error Budget: X% remaining
- Incidents: N incidents, N hours total downtime
- Next week focus: [if budget < 50%] Focus on stability
```

### Monthly SLO Report

```
1. SLO Performance Summary
   - Met/Miss each SLO
   - Error budget remaining
   
2. Incidents
   - Count, severity, MTTR
   - Root causes
   
3. Dependency Health
   - Database uptime
   - Cache performance
   - Message queue metrics
   
4. Recommendations
   - Optimization opportunities
   - Process improvements
```

---

## Escalation Policy

**P99 Latency > 1 second for 5+ minutes:**
- Notify on-call engineer (via PagerDuty)
- Page database team if DB latency
- Start investigation in Slack #incidents

**Error Rate > 1% for 5+ minutes:**
- Trigger SEV-1 (PAGE)
- Consider rollback vs hotfix
- War room in Zoom

**Availability < 99% for 1+ hour:**
- CEO notification
- Status page update
- Post-incident mandatory (after resolution)

---

## Review & Update Schedule

| Frequency | Review | Owner |
|-----------|--------|-------|
| Weekly | Error budget consumption | On-call engineer |
| Monthly | Full SLO performance | Engineering lead + Product |
| Quarterly | SLO targets appropriateness | CTO + Product leadership |
| Yearly | Complete SLO framework audit | Platform engineering |


# Phase 3: Production Deployment Roadmap

## Overview

Phase 3 focuses on containerization, orchestration, and production hardening. This document outlines what will be implemented.

## Phase 3 Scope

### 1. Docker Containerization (15% of Phase 3)

**Objective**: Create production-grade Docker image

**Deliverables**:
- `Dockerfile` - Multi-stage build (dev and prod)
- `.dockerignore` - Exclude unnecessary files
- Docker compose for local development
- Health check configuration in container
- Non-root user for security

**Features**:
- Runtime: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Base image: `mcr.microsoft.com/dotnet/sdk:8.0` for build
- Health check: `/health/live` endpoint
- Graceful shutdown handling (SIGTERM)
- OpenTelemetry endpoint exposure

**Example**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN useradd -m -s /bin/bash appuser
COPY --from=builder /app .
RUN chown -R appuser:appuser /app
USER appuser
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1
EXPOSE 8080
ENTRYPOINT ["dotnet", "Templates.Api.dll"]
```

**docker-compose.yml**:
```yaml
services:
  app:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      - sqlserver
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
```

### 2. Kubernetes Manifests (25% of Phase 3)

**Objective**: Production-ready Kubernetes deployment

**Deliverables**:
- `k8s/namespace.yaml` - Isolated namespace
- `k8s/configmap.yaml` - Configuration per environment
- `k8s/secret.yaml` - Secrets management guide
- `k8s/deployment.yaml` - Pod specification with probes
- `k8s/service.yaml` - ClusterIP or LoadBalancer
- `k8s/ingress.yaml` - HTTPS termination, routing
- `k8s/hpa.yaml` - Horizontal pod autoscaling

**Key Features**:
- Liveness probe: `GET /health/live` (10s period, 3 failures)
- Readiness probe: `GET /health/ready` (5s period, 3 failures)
- Resource requests/limits (CPU, memory)
- Graceful shutdown (terminationGracePeriodSeconds: 30)
- Health check startup probe
- Rolling deployment strategy

**Example Deployment**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: templates-api
  namespace: templates
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    spec:
      containers:
      - name: api
        image: templates-api:1.0.0
        ports:
        - containerPort: 8080
        
        # Liveness (restart if unhealthy)
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
          failureThreshold: 3
        
        # Readiness (remove from LB if not ready)
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
          failureThreshold: 3
        
        resources:
          requests:
            cpu: 100m
            memory: 128Mi
          limits:
            cpu: 500m
            memory: 512Mi
        
        env:
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: templates-config
              key: environment
        
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: templates-secrets
              key: db-connection
        
        - name: OTEL_EXPORTER_OTLP_ENDPOINT
          value: http://otel-collector:4317
```

### 3. Helm Charts (20% of Phase 3)

**Objective**: Templated deployment across environments

**Deliverables**:
- `helm/Chart.yaml` - Chart metadata
- `helm/values.yaml` - Default values
- `helm/values-dev.yaml` - Development overrides
- `helm/values-staging.yaml` - Staging overrides
- `helm/values-prod.yaml` - Production overrides
- `helm/templates/deployment.yaml` - Templated deployment
- `helm/templates/service.yaml` - Templated service
- `helm/templates/configmap.yaml` - Environment config
- Installation guide and validation

**Usage**:
```bash
# Development
helm install templates ./helm -f ./helm/values-dev.yaml -n dev

# Production
helm install templates ./helm -f ./helm/values-prod.yaml -n prod

# Upgrade
helm upgrade templates ./helm -f ./helm/values-prod.yaml -n prod

# Rollback
helm rollback templates 1 -n prod
```

### 4. OpenTelemetry Collector (20% of Phase 3)

**Objective**: Centralized trace, metric, and log collection

**Deliverables**:
- `infra/otel/otel-collector-config.yaml` - Collector configuration
- `infra/otel/Dockerfile` - Collector container
- `infra/otel/docker-compose.yml` - Local stack (collector + Jaeger)
- Jaeger UI for trace visualization
- Prometheus scrape configuration
- Documentation for deployment

**Architecture**:
```
Application → OpenTelemetry SDK
                    ↓
         OpenTelemetry Collector
                    ↓
    ┌───────────────┼───────────────┐
    ↓               ↓               ↓
 Jaeger         Prometheus       Logs (Loki)
 (Tracing)      (Metrics)        (Aggregation)
    ↓               ↓               ↓
   UI          Grafana Dashboards  ELK
```

**Example collector-config.yaml**:
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317

processors:
  batch:
    send_batch_size: 1024
    timeout: 10s

exporters:
  jaeger:
    endpoint: jaeger:14250
  prometheus:
    endpoint: 0.0.0.0:8888
  logging:
    loglevel: debug

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 5. Monitoring & Observability (15% of Phase 3)

**Objective**: Production monitoring stack

**Deliverables**:
- `infra/prometheus/prometheus.yml` - Scrape configs
- `infra/grafana/dashboards/*.json` - Dashboard definitions
- `infra/grafana/provisioning/*.yaml` - Automated provisioning
- Alert rules (for key SLIs)
- Runbooks for common issues

**Dashboards**:
1. **Service Health Dashboard**
   - Request latency (p50, p95, p99)
   - Error rate by endpoint
   - Health check status
   - Pod restarts

2. **Infrastructure Dashboard**
   - CPU/Memory usage
   - Database connection pool
   - Network I/O
   - Disk usage

3. **Business Metrics Dashboard**
   - Orders created per minute
   - Customer registrations
   - Revenue by feature
   - Error rate by business domain

4. **Dependency Status Dashboard**
   - Database health
   - Cache hit rate
   - External API response times
   - Circuit breaker state

### 6. Load Testing (10% of Phase 3)

**Objective**: Performance baseline and resilience validation

**Deliverables**:
- `tools/load-tests/k6-script.js` - k6 load test
- `tools/load-tests/load-test-guide.md` - How to run tests
- `tools/load-tests/scenarios.yaml` - Multiple scenarios
- Performance baseline documentation
- Capacity planning recommendations

**Example k6 script**:
```javascript
import http from 'k6/http';
import { check, group, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 20 },  // Ramp up
    { duration: '1m30s', target: 100 }, // Stay at 100
    { duration: '20s', target: 0 }    // Ramp down
  ]
};

export default function () {
  group('Health Checks', function () {
    let res = http.get('http://localhost:5000/health/live');
    check(res, { 'status is 200': (r) => r.status === 200 });
    sleep(1);
  });

  group('Order Operations', function () {
    let res = http.post('http://localhost:5000/api/v1/orders', {
      customerId: '550e8400-e29b-41d4-a716-446655440000',
      items: [{ productId: '...', quantity: 2, unitPrice: 99.99 }]
    });
    check(res, { 'status is 201': (r) => r.status === 201 });
    sleep(1);
  });
}
```

### 7. Production Hardening (10% of Phase 3)

**Objective**: Security and reliability improvements

**Deliverables**:
- Connection pooling optimization guide
- Database query timeout configuration
- Circuit breaker state persistence (optional)
- Graceful shutdown implementation
- Deployment validation checklist
- Runbooks for incident response

**Topics Covered**:
- Pod disruption budgets (PDB)
- Network policies (ingress/egress)
- Resource quotas per namespace
- RBAC for service accounts
- Secrets rotation procedures
- Database backup strategies
- Log retention policies

## Implementation Timeline

| Phase 3 Component | Effort | Priority | Dependencies |
|------------------|--------|----------|--------------|
| Docker | 1 week | HIGH | None |
| Kubernetes | 2 weeks | HIGH | Docker |
| Helm Charts | 1 week | HIGH | Kubernetes |
| OTEL Collector | 1 week | MEDIUM | None |
| Monitoring | 1 week | MEDIUM | OTEL, Prometheus |
| Load Testing | 1 week | LOW | Kubernetes |
| Production Hardening | 1 week | HIGH | All |

**Total**: 8 weeks (2 months)

## Success Criteria

- [ ] Application deployable to Docker in <1 minute
- [ ] Kubernetes deployment stable for 24 hours with no errors
- [ ] Health checks responsive (<100ms for live, <500ms for ready)
- [ ] 99.9% uptime SLO achievable (9 hours downtime/year)
- [ ] All requests traced and observable in Jaeger
- [ ] Graceful shutdown within 30 seconds
- [ ] Database migrations zero-downtime
- [ ] Load test shows <100ms p95 latency under normal load
- [ ] Circuit breaker activates on 3 failures within 30s
- [ ] Horizontal scaling: 2-10 replicas without errors
- [ ] Zero hardcoded secrets in images
- [ ] All configuration via environment variables or ConfigMaps

## Phase 4+ Future Work

### Phase 4: Advanced Deployment Patterns
- Blue-green deployments
- Canary releases
- Feature flags for gradual rollout
- A/B testing infrastructure
- Database migration automation

### Phase 5: Advanced Features
- CQRS event sourcing
- Saga pattern for transactions
- GraphQL federation
- gRPC service communication
- API gateway patterns

### Phase 6: Operations at Scale
- Multi-region deployment
- Disaster recovery
- Data consistency patterns
- Performance optimization
- Cost optimization

## Getting Started with Phase 3

**When Phase 2 is approved (current state)**:

1. ✅ Phase 1 complete (security) → 0 errors
2. ✅ Phase 2 complete (observability) → 0 errors
3. ⬜ Phase 3: Production Deployment (next sprint)

**Phase 3 Kickoff**:
- Review Kubernetes concepts if unfamiliar
- Set up local Docker environment
- Install Minikube or Docker Desktop Kubernetes
- Review Dockerfile and docker-compose patterns
- Plan environment configuration strategy

**Phase 3 Development Environment**:
```bash
# Prerequisites
docker --version  # 20.10+
kubectl version   # 1.27+
helm version      # 3.12+
k6 --version      # Latest

# Local Kubernetes (choose one)
minikube start --cpus 4 --memory 8192
# or
docker desktop → enable Kubernetes

# Verify
kubectl cluster-info
docker ps
helm version
```

## Documentation Structure for Phase 3

```
docs/
├── deployment/
│   ├── DOCKER-SETUP.md          # Containerization guide
│   ├── KUBERNETES-DEPLOYMENT.md # K8s configuration
│   ├── HELM-CHARTS.md           # Helm templating
│   └── PRODUCTION-CHECKLIST.md  # Deployment validation
├── operations/
│   ├── MONITORING-SETUP.md      # Prometheus & Grafana
│   ├── OTEL-COLLECTOR.md        # Trace collection
│   ├── LOAD-TESTING.md          # k6 performance testing
│   └── RUNBOOKS.md              # Incident response
├── infrastructure/
│   ├── docker/
│   │   ├── Dockerfile
│   │   └── docker-compose.yml
│   ├── kubernetes/
│   │   ├── deployment.yaml
│   │   ├── service.yaml
│   │   └── ingress.yaml
│   ├── helm/
│   │   ├── Chart.yaml
│   │   └── templates/
│   └── observability/
│       ├── otel-collector-config.yaml
│       └── prometheus.yml
└── tools/
    └── load-tests/
        ├── k6-script.js
        └── load-test-guide.md
```

## Approval & Continuation

**Current Status**: Phase 2 ✅ Complete (100%)

**Next Action**: Await approval to proceed with Phase 3

**Phase 3 Start**: Upon confirmation "YES LETS START PHASE 3"

---

**Created**: 2024-01-15
**Status**: Planning
**Next Phase**: Phase 3 - Production Deployment
**Estimated Duration**: 8 weeks

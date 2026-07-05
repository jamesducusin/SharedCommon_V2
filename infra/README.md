# Infrastructure

## Local Development

```powershell
# Start all services
docker-compose -f docker/docker-compose.yml up -d

# Services:
# Redis:        localhost:6379
# Kafka:        localhost:9092
# Jaeger UI:    http://localhost:16686
# Prometheus:   http://localhost:9090
```

## Kubernetes

Reference manifests in `kubernetes/` demonstrate production deployment patterns:
- Resource limits and requests sized for SharedCommon packages
- Liveness/readiness probes via `/health/live` and `/health/ready`
- Secret references (never hardcoded values)
- OTLP collector sidecar configuration

## Observability Stack

- **prometheus.yml** — scrape config for SharedCommon metrics endpoints
- **grafana/** — dashboard JSON exports for SharedCommon packages
- **jaeger.yml** — Jaeger all-in-one config for local tracing

## Terraform

`terraform/` contains cloud infrastructure-as-code:
- `main.tf` — resource definitions
- `variables.tf` — input variables (no defaults for secrets)
- `outputs.tf` — outputs consumed by CI/CD

See: docs/architecture/deployment.md

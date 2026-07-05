# Phase 3: Deployment Guide

Complete walkthrough for building, containerizing, and deploying the SharedCommon platform to production.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Docker Build & Push](#docker-build--push)
4. [Kubernetes Deployment](#kubernetes-deployment)
5. [Helm Deployment](#helm-deployment)
6. [Observability Stack](#observability-stack)
7. [Troubleshooting](#troubleshooting)
8. [Production Checklist](#production-checklist)

---

## Prerequisites

### Required Tools

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0+ | Build & runtime |
| Docker | 20.10+ | Containerization |
| kubectl | 1.28+ | K8s management |
| Helm | 3.13+ | K8s templating |
| Docker Compose | 2.0+ | Local orchestration |

### Installation

**macOS:**
```bash
brew install dotnet docker kubectl helm
```

**Windows (with WSL2):**
```powershell
# Install via chocolatey or direct downloads
choco install dotnet-sdk docker kubernetes-cli helm
```

**Linux (Ubuntu):**
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 docker.io kubectl helm
sudo usermod -aG docker $USER
```

### Verify Installation

```bash
dotnet --version          # Should show 8.0.x
docker --version          # Should show 20.10+
kubectl version --client  # Should show 1.28+
helm version              # Should show 3.13+
```

---

## Local Development

### Setup

```bash
cd ~/workspace/Cerberus

# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test

# Check formatting
dotnet format --verify-no-changes --verbosity diagnostic
```

### Database Setup (SQL Server)

```bash
# Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# Update connection string in appsettings.Development.json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=TemplatesDb;User Id=sa;Password=YourPassword123!"
}

# Run migrations
dotnet ef database update --project src/Templates.Infrastructure
```

### Local Orchestration with Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f api

# Health check
curl http://localhost:8080/health/live

# Tear down
docker-compose down -v
```

**Services Available:**
- API: http://localhost:8080
- SQL Server: localhost:1433
- Redis: localhost:6379
- RabbitMQ: http://localhost:15672 (guest/guest)
- Jaeger UI: http://localhost:16686
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

---

## Docker Build & Push

### Build Locally

```bash
# Build image with tag
docker build -t templates-api:1.0.0 \
  --build-arg DOTNET_VERSION=8.0 \
  .

# Verify image
docker images | grep templates-api
docker inspect templates-api:1.0.0 | grep -E "Cmd|Env"

# Test locally
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  templates-api:1.0.0

curl http://localhost:8080/health/live
```

### Multi-Stage Build Details

```dockerfile
# Stage 1: Builder
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
  # Builds: bin/Release/net8.0/publish/

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
  # Copies from builder
  # Creates non-root user (appuser)
  # Exposes port 8080
  # Health check: curl -f http://localhost:8080/health/live
```

**Image Size:** ~280MB (final runtime image)

### Push to Registry

```bash
# Login
docker login -u <username> -p <token> <registry>

# For Docker Hub
docker login

# For Azure Container Registry
az acr login --name <registry-name>

# Tag for registry
docker tag templates-api:1.0.0 \
  myregistry.azurecr.io/templates-api:1.0.0

# Push
docker push myregistry.azurecr.io/templates-api:1.0.0

# List images in registry
az acr repository list --name myregistry
```

### Build Optimization

```bash
# Use BuildKit for faster builds
DOCKER_BUILDKIT=1 docker build -t templates-api:1.0.0 .

# Build without cache if changes not detected
docker build --no-cache -t templates-api:1.0.0 .

# Check image layers
docker history templates-api:1.0.0
```

---

## Kubernetes Deployment

### Prerequisites

**Kubernetes Cluster:**
- Local: Docker Desktop K8s, Minikube, Kind
- Cloud: AKS (Azure), EKS (AWS), GKE (Google)

### Single Manifest Deployment

```bash
# Create namespace
kubectl create namespace templates
kubectl config set-context --current --namespace=templates

# Apply manifest
kubectl apply -f infra/kubernetes/templates-api-deployment.yaml

# Verify deployment
kubectl get deployments
kubectl get pods
kubectl get services

# Check readiness
kubectl wait --for=condition=ready pod \
  -l app=templates-api \
  --timeout=300s

# Logs
kubectl logs -f deployment/templates-api

# Port forward for local access
kubectl port-forward svc/templates-api 8080:80
curl http://localhost:8080/health/ready
```

### Ingress Setup

```bash
# Install Nginx Ingress Controller
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm install ingress-nginx ingress-nginx/ingress-nginx \
  -n ingress-nginx --create-namespace

# Install cert-manager (for HTTPS)
helm repo add jetstack https://charts.jetstack.io
helm install cert-manager jetstack/cert-manager \
  -n cert-manager --create-namespace \
  --set installCRDs=true

# Apply ClusterIssuer for Let's Encrypt
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF

# Apply ingress
kubectl apply -f infra/kubernetes/ingress.yaml

# Monitor certificate
kubectl get certificate
kubectl describe certificate templates-api-cert
```

### Scaling & Autoscaling

```bash
# Manual scaling
kubectl scale deployment templates-api --replicas=5

# Check HPA status
kubectl get hpa
kubectl describe hpa templates-api-hpa

# Monitor metrics (requires metrics-server)
kubectl top nodes
kubectl top pods
```

### Health Checks

```bash
# Test liveness (restart if fails)
kubectl describe pod <pod-name> | grep -A 5 "Liveness"

# Test readiness (remove from load balancer if fails)
kubectl describe pod <pod-name> | grep -A 5 "Readiness"

# Manual health probe test
kubectl exec -it <pod-name> -- curl -s http://localhost:8080/health/live | jq
```

---

## Helm Deployment

### Chart Structure

```
helm/
├── Chart.yaml              # Chart metadata
├── values.yaml             # Default values
├── values-staging.yaml     # Staging overrides
├── values-prod.yaml        # Production overrides
└── templates/
    ├── deployment.yaml
    ├── service.yaml
    ├── ingress.yaml
    └── ...
```

### Deployment Commands

```bash
# Dry run to see generated manifests
helm template templates-api helm/ \
  --values helm/values.yaml

# Install chart to cluster
helm install templates-api helm/ \
  --namespace templates \
  --create-namespace \
  --values helm/values.yaml

# Deploy to staging
helm install templates-api helm/ \
  --namespace staging \
  --create-namespace \
  --values helm/values-staging.yaml \
  --set image.tag=1.0.0-staging

# Deploy to production
helm install templates-api helm/ \
  --namespace production \
  --create-namespace \
  --values helm/values-prod.yaml \
  --set image.repository=myregistry.azurecr.io/templates-api \
  --set image.tag=1.0.0

# Upgrade existing release
helm upgrade templates-api helm/ \
  --namespace production \
  --values helm/values-prod.yaml \
  --values helm/values-prod-custom.yaml

# Rollback to previous version
helm rollback templates-api 1 --namespace production

# List releases
helm list --all-namespaces
```

### Configuration Management

```bash
# Override specific values
helm install templates-api helm/ \
  --set replicas=5 \
  --set resources.requests.memory=512Mi \
  --set autoscaling.enabled=true \
  --set autoscaling.maxReplicas=10

# Use custom values file
helm install templates-api helm/ \
  -f helm/values-prod.yaml \
  -f helm/values-prod-customizations.yaml

# Set from command line (takes precedence)
helm install templates-api helm/ \
  -f helm/values-prod.yaml \
  --set "config.logLevel=Debug" \
  --set "config.otel.samplingRate=0.5"
```

---

## Observability Stack

### Deploy OpenTelemetry

```bash
# Create namespace for observability
kubectl create namespace observability

# Add observability Helm chart
helm repo add open-telemetry https://open-telemetry.github.io/opentelemetry-helm-charts
helm repo update

# Deploy OTel Collector
helm install otel-collector open-telemetry/opentelemetry-collector \
  --namespace observability \
  --values infra/observability/otel-collector-values.yaml

# Deploy Jaeger (backend for traces)
helm install jaeger jaegertracing/jaeger \
  --namespace observability \
  --set collector.service.otlp.enabled=true

# Deploy Prometheus
helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace observability \
  --values infra/observability/prometheus-values.yaml

# Deploy Grafana
helm install grafana grafana/grafana \
  --namespace observability \
  --values infra/observability/grafana-values.yaml
```

### Access Observability UIs

```bash
# Jaeger (traces)
kubectl port-forward -n observability svc/jaeger 16686:16686
# Visit: http://localhost:16686

# Prometheus (metrics)
kubectl port-forward -n observability svc/prometheus-operated 9090:9090
# Visit: http://localhost:9090

# Grafana (dashboards)
kubectl port-forward -n observability svc/grafana 3000:3000
# Visit: http://localhost:3000 (admin/admin)
```

### Configure Prometheus Scraping

```yaml
# infra/observability/prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'templates-api'
    kubernetes_sd_configs:
      - role: pod
        namespaces:
          names:
            - production
    relabel_configs:
      - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
        action: keep
        regex: 'true'
      - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_path]
        action: replace
        target_label: __metrics_path__
      - source_labels: [__address__, __meta_kubernetes_pod_annotation_prometheus_io_port]
        action: replace
        regex: '([^:]+)(?::\d+)?;(\d+)'
        replacement: '$1:$2'
        target_label: __address__
```

### Create Grafana Dashboards

```bash
# Export dashboard JSON to ConfigMap
kubectl create configmap grafana-dashboards \
  --from-file=dashboards/ \
  --namespace observability

# Apply dashboard provisioning
kubectl apply -f - <<EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: grafana-dashboard-provisioning
  namespace: observability
data:
  dashboards.yaml: |
    apiVersion: 1
    providers:
      - name: 'dashboards'
        orgId: 1
        folder: ''
        type: file
        disableDeletion: false
        updateIntervalSeconds: 10
        allowUiUpdates: true
        options:
          path: /var/lib/grafana/dashboards
EOF
```

---

## Troubleshooting

### Pod Won't Start

```bash
# Check pod status
kubectl describe pod <pod-name>

# Check events
kubectl get events --sort-by='.lastTimestamp'

# Check logs
kubectl logs <pod-name>
kubectl logs <pod-name> --previous  # If crashed

# Common issues:
# 1. Image pull error
kubectl logs <pod-name> | grep -i "image"

# 2. CrashLoopBackOff
kubectl describe pod <pod-name> | grep -A 5 "Last State"

# 3. Pending (resource constraints)
kubectl describe node
kubectl top nodes
```

### High Latency

```bash
# Check pod resources
kubectl describe pod <pod-name> | grep -A 5 "Requests\|Limits"

# Check node metrics
kubectl top nodes
kubectl top pods --sort-by=memory

# Check network policies
kubectl get networkpolicies
kubectl describe networkpolicy <name>

# Increase resources if needed
kubectl set resources deployment templates-api \
  --requests=cpu=250m,memory=256Mi \
  --limits=cpu=1000m,memory=1Gi
```

### Certificate Issues

```bash
# Check cert-manager logs
kubectl logs -n cert-manager deployment/cert-manager

# Check certificate status
kubectl describe certificate templates-api-cert

# Check secret
kubectl get secret templates-api-tls -o yaml

# Manually renew
kubectl delete certificate templates-api-cert
kubectl apply -f infra/kubernetes/ingress.yaml
```

### Database Connection Issues

```bash
# Test connection from pod
kubectl run -it --rm debug --image=mcr.microsoft.com/dotnet/runtime:8.0 -- bash

# Inside pod
apt-get update && apt-get install -y sqlcmd
sqlcmd -S <db-server> -U sa -P <password>

# Check connection string
kubectl get secret templates-secrets -o jsonpath='{.data.ConnectionString}' | base64 -d
```

---

## Production Checklist

### Pre-Deployment

- [ ] Build & test Docker image locally
- [ ] Run security scan: `trivy image templates-api:1.0.0`
- [ ] Update image tag (semantic versioning)
- [ ] Push image to registry
- [ ] Verify registry access from K8s cluster
- [ ] Review Helm values for production environment
- [ ] Set resource requests/limits appropriately
- [ ] Configure secrets (DB connection, JWT key, API keys)
- [ ] Setup persistent volumes if needed
- [ ] Configure backup strategy for databases
- [ ] Setup monitoring alerts
- [ ] Test disaster recovery procedures

### Deployment

- [ ] Create production namespace
- [ ] Deploy observability stack (Prometheus, Jaeger, Grafana)
- [ ] Deploy OTel Collector
- [ ] Deploy database (or setup connection)
- [ ] Deploy cache (Redis)
- [ ] Deploy message broker (RabbitMQ)
- [ ] Deploy application with Helm
- [ ] Verify health checks passing
- [ ] Monitor logs for errors
- [ ] Run smoke tests
- [ ] Monitor resource usage
- [ ] Check Grafana dashboards

### Post-Deployment

- [ ] Verify API endpoints responding
- [ ] Check distributed traces in Jaeger
- [ ] Monitor metrics in Prometheus
- [ ] Verify alerting rules working
- [ ] Test failover/high availability
- [ ] Monitor error rates
- [ ] Check performance metrics (P99, P95, etc.)
- [ ] Review security logs
- [ ] Document configuration used
- [ ] Update runbooks with observed issues
- [ ] Schedule post-deployment review

### Monitoring & Maintenance

```bash
# Daily checks
kubectl get deployments --all-namespaces
kubectl top nodes
kubectl top pods --all-namespaces

# Weekly review
helm list --all-namespaces
kubectl get certificates --all-namespaces
# Review error rates in Grafana
# Review security logs

# Monthly
# Update dependencies
# Review performance metrics
# Update documentation
# Conduct disaster recovery drill
```

---

## Common Commands Reference

```bash
# Cluster info
kubectl cluster-info
kubectl get nodes
kubectl get namespaces

# Deployment management
kubectl apply -f manifest.yaml
kubectl delete -f manifest.yaml
kubectl rollout status deployment/templates-api
kubectl rollout undo deployment/templates-api

# Debugging
kubectl describe pod <name>
kubectl logs <pod-name> [--tail=100] [--follow]
kubectl exec -it <pod-name> -- /bin/bash
kubectl get events --sort-by='.lastTimestamp'

# Helm management
helm list
helm status <release>
helm values <release>
helm get all <release>

# Port forwarding
kubectl port-forward svc/<service> <local>:<remote>
kubectl port-forward pod/<pod> <local>:<remote>

# Resource inspection
kubectl api-resources
kubectl explain <resource>
kubectl get <resource> -o yaml | head -50
```


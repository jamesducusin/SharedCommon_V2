# Cloud-Ready DDD Template — Deployment & Operations Guide

Production deployment guide for Cloud-Ready DDD projects using the template.

## Pre-Deployment Checklist

### Code Quality
- [ ] All tests passing (`dotnet test`)
- [ ] Code coverage ≥ 80%
- [ ] No compiler warnings (`TreatWarningsAsErrors=true`)
- [ ] No hardcoded values (secrets, URLs, ports)
- [ ] Architecture tests passing (layer separation)
- [ ] Security audit passed

### Configuration
- [ ] Database migrations tested
- [ ] `appsettings.Production.json` configured
- [ ] All required environment variables documented
- [ ] Connection strings use secrets manager
- [ ] CORS origins restricted to known domains
- [ ] Logging level set to Information or Warning
- [ ] Health check endpoints configured

### Documentation
- [ ] README updated with service description
- [ ] API documented via Swagger/OpenAPI
- [ ] Database schema documented
- [ ] Architecture diagram included
- [ ] Deployment procedure documented
- [ ] Runbook for common issues created

## Docker Deployment

### Create Dockerfile

**Dockerfile** (in project root):
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and projects
COPY ["YourService.sln", "YourService.sln"]
COPY ["src/YourService.Api/", "src/YourService.Api/"]
COPY ["src/YourService.Application/", "src/YourService.Application/"]
COPY ["src/YourService.Domain/", "src/YourService.Domain/"]
COPY ["src/YourService.Infrastructure/", "src/YourService.Infrastructure/"]

# Restore and build
RUN dotnet restore "YourService.sln"
RUN dotnet build "YourService.sln" -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "src/YourService.Api/YourService.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Install timezone data
RUN apk add --no-cache tzdata

# Create non-root user
RUN addgroup -g 1000 app && adduser -D -u 1000 -G app app

COPY --from=publish /app/publish .
RUN chown -R app:app /app

USER app
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health/ready || exit 1

ENTRYPOINT ["dotnet", "YourService.Api.dll"]
```

### Build Docker Image

```bash
docker build -t yourservice:latest .
docker tag yourservice:latest yourservice:1.0.0
docker tag yourservice:latest myregistry.azurecr.io/yourservice:latest
```

### Push to Container Registry

```bash
# Azure Container Registry
az acr login --name myregistry
docker push myregistry.azurecr.io/yourservice:latest

# Docker Hub
docker login
docker push yourusername/yourservice:latest

# GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin
docker push ghcr.io/username/yourservice:latest
```

### Run Docker Container Locally

```bash
docker run \
  -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=YourServiceDb;..." \
  -e ASPNETCORE_ENVIRONMENT=Development \
  yourservice:latest
```

## Kubernetes Deployment

### Create Namespace

```bash
kubectl create namespace yourservice
kubectl config set-context --current --namespace=yourservice
```

### Create Secrets

```bash
# Database connection string
kubectl create secret generic db-secret \
  --from-literal=connection-string="Server=db-server;Database=YourServiceDb;User=sa;Password=..."

# API keys (if needed)
kubectl create secret generic api-keys \
  --from-literal=payment-api-key="xxx" \
  --from-literal=email-api-key="yyy"
```

### Deployment Manifest

**deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: yourservice
  namespace: yourservice
  labels:
    app: yourservice
    version: v1
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: yourservice
  template:
    metadata:
      labels:
        app: yourservice
        version: v1
    spec:
      serviceAccountName: yourservice
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000

      containers:
      - name: api
        image: myregistry.azurecr.io/yourservice:latest
        imagePullPolicy: IfNotPresent
        
        ports:
        - name: http
          containerPort: 5000
          protocol: TCP
        
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        
        - name: OTEL_EXPORTER_OTLP_ENDPOINT
          value: "http://otel-collector:4317"
        
        resources:
          requests:
            cpu: 100m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi
        
        livenessProbe:
          httpGet:
            path: /health/live
            port: http
            scheme: HTTP
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        readinessProbe:
          httpGet:
            path: /health/ready
            port: http
            scheme: HTTP
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        
        securityContext:
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: true
          runAsNonRoot: true
          capabilities:
            drop:
            - ALL
        
        volumeMounts:
        - name: tmp
          mountPath: /tmp
        - name: cache
          mountPath: /app/.cache
      
      volumes:
      - name: tmp
        emptyDir: {}
      - name: cache
        emptyDir: {}

---
apiVersion: v1
kind: Service
metadata:
  name: yourservice
  namespace: yourservice
  labels:
    app: yourservice
spec:
  type: ClusterIP
  selector:
    app: yourservice
  ports:
  - name: http
    port: 80
    targetPort: http
    protocol: TCP

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: yourservice-hpa
  namespace: yourservice
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: yourservice
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80

---
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: yourservice-pdb
  namespace: yourservice
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: yourservice

---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: yourservice
  namespace: yourservice
```

### Deploy to Kubernetes

```bash
# Apply manifests
kubectl apply -f deployment.yaml

# Monitor deployment
kubectl rollout status deployment/yourservice -n yourservice

# View logs
kubectl logs -f deployment/yourservice -n yourservice

# Port forward to test
kubectl port-forward svc/yourservice 5000:80 -n yourservice
```

## Database Migrations

### Pre-Deployment Testing

```bash
# Create test database
dotnet ef database update --context ApplicationDbContext

# Verify migration
dotnet ef migrations list

# Rollback if needed
dotnet ef database update --migration PreviousMigration
```

### Production Migration Strategy

**Option 1: Automatic Migration (Startup)**
```csharp
// In Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

**Option 2: Manual Migration (Safer)**
```bash
# Before deployment
dotnet ef migrations add AddNewFeature
dotnet ef database update

# Deploy
# (migrations already applied)
```

**Option 3: Blue-Green Deployment (Zero-Downtime)**
```bash
# Deploy new version (v2) alongside current (v1)
# Route traffic gradually to v2
# Once stable, remove v1
# New version applies migrations
```

## Monitoring & Observability

### Structured Logging

All logs flow to centralized logging (ELK, Splunk, Azure Monitor):
```json
{
  "timestamp": "2026-05-30T10:30:45.123Z",
  "level": "Information",
  "logger": "YourService.Application.Features.Orders.Create.CreateOrderCommandHandler",
  "correlationId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "message": "Order created successfully",
  "OrderId": "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6",
  "CustomerId": "x9y8z7w6-v5u4-t3s2-r1q0-p9o8n7m6l5k4",
  "Environment": "Production",
  "MachineName": "pod-name"
}
```

### Distributed Tracing

OpenTelemetry traces all requests:
```bash
# Configure exporter
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
export OTEL_SERVICE_NAME=yourservice
```

### Metrics

Prometheus scrapes metrics:
```yaml
# Prometheus scrape config
- job_name: 'yourservice'
  static_configs:
    - targets: ['yourservice:5000']
  metrics_path: '/metrics'
```

### Health Checks

```bash
# Liveness (is it running?)
curl http://localhost:5000/health/live

# Readiness (can it serve traffic?)
curl http://localhost:5000/health/ready

# Full health with details
curl http://localhost:5000/health
```

## Azure Deployment

### Create Azure Container Registry

```bash
az acr create --resource-group mygroup --name myregistry --sku Standard
az acr login --name myregistry
```

### Create Azure SQL Database

```bash
az sql server create \
  --name yourservice-db \
  --resource-group mygroup \
  --admin-user sqladmin \
  --admin-password MyPassword123!

az sql db create \
  --resource-group mygroup \
  --server yourservice-db \
  --name YourServiceDb
```

### Deploy to Azure Container Instances

```bash
az container create \
  --resource-group mygroup \
  --name yourservice \
  --image myregistry.azurecr.io/yourservice:latest \
  --registry-login-server myregistry.azurecr.io \
  --registry-username <username> \
  --registry-password <password> \
  --ports 5000 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    'ConnectionStrings__DefaultConnection=Server=yourservice-db.database.windows.net;Database=YourServiceDb;User=sqladmin;Password=MyPassword123!;'
```

### Deploy to Azure App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name yourservice-plan \
  --resource-group mygroup \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group mygroup \
  --plan yourservice-plan \
  --name yourservice-app \
  --deployment-container-image-name myregistry.azurecr.io/yourservice:latest

# Configure App Settings
az webapp config appsettings set \
  --resource-group mygroup \
  --name yourservice-app \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    'ConnectionStrings__DefaultConnection=...'
```

## AWS Deployment

### Push to ECR

```bash
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 123456789.dkr.ecr.us-east-1.amazonaws.com

docker tag yourservice:latest 123456789.dkr.ecr.us-east-1.amazonaws.com/yourservice:latest
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/yourservice:latest
```

### ECS Task Definition

```json
{
  "family": "yourservice",
  "containerDefinitions": [
    {
      "name": "yourservice",
      "image": "123456789.dkr.ecr.us-east-1.amazonaws.com/yourservice:latest",
      "portMappings": [
        {
          "containerPort": 5000,
          "hostPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "secrets": [
        {
          "name": "ConnectionStrings__DefaultConnection",
          "valueFrom": "arn:aws:secretsmanager:us-east-1:123456789:secret:db-connection-string"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/yourservice",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ],
  "requiresCompatibilities": ["FARGATE"],
  "networkMode": "awsvpc",
  "cpu": "256",
  "memory": "512"
}
```

### Deploy ECS Service

```bash
aws ecs create-service \
  --cluster my-cluster \
  --service-name yourservice \
  --task-definition yourservice:1 \
  --desired-count 3 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx],securityGroups=[sg-xxx],assignPublicIp=ENABLED}"
```

## GitHub Actions CI/CD Pipeline

**.github/workflows/deploy.yml**:
```yaml
name: Deploy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --configuration Release --no-build --no-restore

  build:
    needs: test
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2
    
    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=semver,pattern={{version}}
          type=semver,pattern={{major}}.{{minor}}
    
    - name: Build and push
      uses: docker/build-push-action@v4
      with:
        context: .
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max

  deploy:
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    needs: build
    runs-on: ubuntu-latest
    steps:
    - name: Deploy to production
      run: |
        echo "Deploying to production..."
        # Add your deployment script here
```

## Rollback Procedures

### Docker Container Rollback

```bash
# Switch to previous image
docker run \
  -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="..." \
  yourservice:1.0.0
```

### Kubernetes Rollback

```bash
# View rollout history
kubectl rollout history deployment/yourservice

# Rollback to previous version
kubectl rollout undo deployment/yourservice

# Rollback to specific revision
kubectl rollout undo deployment/yourservice --to-revision=2
```

### Database Rollback

```bash
# Remove failed migration
dotnet ef migrations remove

# Apply previous migration
dotnet ef database update PreviousMigration

# Redeploy code with previous logic
```

## Troubleshooting

### Application Won't Start
```bash
# Check logs
docker logs <container-id>

# Verify configuration
echo $ConnectionStrings__DefaultConnection

# Test database connectivity
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
```

### High Memory Usage
- Check for memory leaks (Profile with dotTrace)
- Review caching strategy (might be growing unbounded)
- Verify database connection pooling
- Check for event handler leaks in domain events

### Slow API Responses
- Enable query logging: `Microsoft.EntityFrameworkCore.Database.Command: Debug`
- Check database indices
- Enable caching for frequently accessed data
- Review pagination on list endpoints
- Profile with Application Insights

### Database Connection Failures
```bash
# Verify connection string
# Check firewall rules (Azure SQL requires Azure -> SQL firewall rule)
# Ensure credentials are correct
# Verify database server is accessible
```

## Security Hardening

### Pod Security Policy

```yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: yourservice-psp
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
    - ALL
  volumes:
    - 'configMap'
    - 'emptyDir'
    - 'projected'
    - 'secret'
    - 'downwardAPI'
    - 'persistentVolumeClaim'
  hostNetwork: false
  hostIPC: false
  hostPID: false
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'MustRunAs'
    seLinuxOptions:
      level: "s0:c123,c456"
  supplementalGroups:
    rule: 'RunAsAny'
  fsGroup:
    rule: 'RunAsAny'
```

### Network Policy

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: yourservice-netpol
  namespace: yourservice
spec:
  podSelector:
    matchLabels:
      app: yourservice
  policyTypes:
    - Ingress
    - Egress
  ingress:
    - from:
      - podSelector:
          matchLabels:
            app: nginx-ingress
      ports:
      - protocol: TCP
        port: 5000
  egress:
    - to:
      - podSelector:
          matchLabels:
            app: postgres
      ports:
      - protocol: TCP
        port: 5432
```

## Performance Optimization

### Response Caching

```json
{
  "Features": {
    "Caching": {
      "Enabled": true,
      "L1": {
        "MaxSizeMb": 500,
        "TtlSeconds": 600
      },
      "L2": {
        "ConnectionString": "redis:6379",
        "TtlSeconds": 3600,
        "Enabled": true
      }
    }
  }
}
```

### Database Query Optimization

```bash
# Add indices for frequently queried columns
# Use EXPLAIN to analyze query plans
# Implement pagination (no full table scans)
# Use projections (.Select()) to reduce data transfer
```

### Compression

```csharp
// In Program.cs
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/json" });
});

app.UseResponseCompression();
```

---

**Last Updated**: 2026-05-30

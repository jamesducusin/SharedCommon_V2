# Multi-stage build for production-ready containerization
# Build stage: Compile and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /build

# Copy project files
COPY *.sln .
COPY src/ src/
COPY tests/ tests/

# Restore dependencies
RUN dotnet restore

# Build and publish
RUN dotnet publish src/Templates.Api/Templates.Api.csproj \
    -c Release \
    -o /app \
    --no-restore \
    --self-contained false

# Runtime stage: Minimal production image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Create non-root user for security
RUN useradd -m -s /bin/bash appuser

# Copy compiled application from builder
COPY --from=builder /app .

# Set ownership to appuser
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_URLS=http://+:8080

# Health check: Kubernetes will use /health/live endpoint
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Run application
ENTRYPOINT ["dotnet", "Templates.Api.dll"]

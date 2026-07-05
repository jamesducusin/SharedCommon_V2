# SharedCommon.Grpc

gRPC infrastructure for ASP.NET Core services: exception-to-status mapping, correlation ID propagation, structured request logging, gRPC health check (`grpc-health-v1`), and optional server reflection.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.Grpc
```

## Registration

```csharp
// Services
builder.Services.AddSharedGrpc(builder.Configuration);
builder.Services.AddGrpc();   // still needed for your own services

// Endpoint mapping
app.MapSharedGrpc(app.Environment);   // maps health + reflection
app.MapGrpcService<MyOrderService>(); // map your own services
```

## Configuration

```json
{
  "SharedCommon": {
    "Grpc": {
      "EnableReflection": false,
      "EnableHealthCheck": true,
      "MaxReceiveMessageSizeBytes": 4194304,
      "MaxSendMessageSizeBytes": 4194304,
      "CorrelationIdHeader": "x-correlation-id"
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `EnableReflection` | `false` | Set `true` in Development for tools like Postman and grpcurl. |
| `EnableHealthCheck` | `true` | Serves the `grpc-health-v1` protocol. |
| `MaxReceiveMessageSizeBytes` | `4194304` | 4 MB. Increase for large payloads. |
| `MaxSendMessageSizeBytes` | `4194304` | 4 MB. |
| `CorrelationIdHeader` | `x-correlation-id` | gRPC metadata key for correlation ID. |

## Interceptors

Three interceptors are registered globally in order — no per-service configuration needed:

| Order | Interceptor | What it does |
|-------|-------------|-------------|
| 1 | `ExceptionInterceptor` | Catches unhandled exceptions and maps them to gRPC `StatusCode`. Never leaks stack traces. |
| 2 | `CorrelationIdInterceptor` | Extracts `x-correlation-id` from request metadata and stores it in `IRequestContext`. Writes it back to response trailers. |
| 3 | `LoggingInterceptor` | Logs method name, status, duration, and correlation ID for every call. |

### Exception → Status mapping

| Exception type | gRPC Status |
|---------------|-------------|
| `NotFoundException` (or matching message) | `NOT_FOUND` |
| `ValidationException` (FluentValidation) | `INVALID_ARGUMENT` |
| `UnauthorizedException` | `UNAUTHENTICATED` |
| `ConflictException` | `ALREADY_EXISTS` |
| `TimeoutException` | `DEADLINE_EXCEEDED` |
| Everything else | `INTERNAL` |

## Usage

### Implementing a gRPC service

Nothing changes from standard gRPC — interceptors apply automatically:

```csharp
public class OrderGrpcService(IOrderService orders, IRequestContext ctx) 
    : Orders.OrdersBase
{
    public override async Task<GetOrderResponse> GetOrder(
        GetOrderRequest request,
        ServerCallContext context)
    {
        var result = await orders.GetByIdAsync(Guid.Parse(request.OrderId), context.CancellationToken);

        return result switch
        {
            Result<Order>.Success s => new GetOrderResponse
            {
                OrderId = s.Data.Id.ToString(),
                Status  = s.Data.Status
            },
            Result<Order>.Failure { Code: "NOT_FOUND" } =>
                throw new RpcException(new Status(StatusCode.NotFound, "Order not found.")),
            _ =>
                throw new RpcException(new Status(StatusCode.Internal, "Unexpected error."))
        };
    }
}
```

### Sending the correlation ID from a client

```csharp
var channel = GrpcChannel.ForAddress("https://orders.internal");
var client  = new Orders.OrdersClient(channel);

var headers = new Metadata
{
    { "x-correlation-id", correlationId }
};

var response = await client.GetOrderAsync(
    new GetOrderRequest { OrderId = id.ToString() },
    headers);
```

### Health check (grpc-health-v1)

When `EnableHealthCheck: true`, the standard `grpc.health.v1.Health` service is available. Test it with grpcurl:

```bash
grpcurl -plaintext localhost:5000 grpc.health.v1.Health/Check
```

### Server reflection (Development only)

When `EnableReflection: true`, tools like Postman and grpcurl can discover your service definitions without the `.proto` files:

```bash
grpcurl -plaintext localhost:5000 list
grpcurl -plaintext localhost:5000 describe orders.v1.Orders
```

## What Gets Registered

| Service | Lifetime | Notes |
|---------|----------|-------|
| `ExceptionInterceptor` | Singleton | Outermost interceptor. |
| `CorrelationIdInterceptor` | Scoped | Per-call correlation ID extraction. |
| `LoggingInterceptor` | Singleton | Per-call logging. |
| `HealthServiceImpl` | Singleton | `grpc-health-v1` handler. Only when `EnableHealthCheck: true`. |

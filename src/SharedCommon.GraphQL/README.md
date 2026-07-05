# SharedCommon.GraphQL

Hot Chocolate 14 GraphQL infrastructure for ASP.NET Core services: domain error mapping, authorization integration, N+1-safe DataLoader base, Relay cursor pagination, query complexity and depth limits.

## Installation

```bash
dotnet add package SharedCommon.Core
dotnet add package SharedCommon.GraphQL
```

## Registration

```csharp
// Services — chain your schema types after AddSharedGraphQL
builder.Services
    .AddSharedGraphQL(builder.Configuration)
    .AddQueryType<QueryType>()
    .AddMutationType<MutationType>()
    .AddType<OrderType>();

// Middleware pipeline (after UseAuthentication / UseAuthorization)
app.MapSharedGraphQL(app.Environment);
```

## Configuration

```json
{
  "SharedCommon": {
    "GraphQL": {
      "MaxAllowedComplexity": 1000,
      "MaxAllowedExecutionDepth": 15,
      "EnableIntrospection": false,
      "Path": "/graphql",
      "EnableBananaCakePop": false
    }
  }
}
```

| Property | Default | Notes |
|----------|---------|-------|
| `MaxAllowedComplexity` | `1000` | Queries exceeding this are rejected before execution. |
| `MaxAllowedExecutionDepth` | `15` | Prevents arbitrarily deep nested queries. |
| `EnableIntrospection` | `false` | Enable in Development for tooling (Banana Cake Pop, Postman). |
| `Path` | `/graphql` | GraphQL endpoint path. |
| `EnableBananaCakePop` | `false` | Serve the BCP IDE. Development only. |

---

## Error Handling

`DomainErrorFilter` maps domain exceptions to structured GraphQL errors automatically. Internal exception details never reach the client.

```csharp
// In a resolver — throw domain exceptions normally
public async Task<Order> GetOrderAsync(Guid id, [Service] IOrderService svc)
{
    var result = await svc.GetByIdAsync(id, Context.RequestAborted);

    return result switch
    {
        Result<Order>.Success s  => s.Data,
        Result<Order>.Failure { Code: "NOT_FOUND" } => throw new NotFoundException($"Order {id} not found"),
        _ => throw new InvalidOperationException("Unexpected result")
    };
}
```

| Exception | GraphQL Error Code |
|-----------|-------------------|
| `NotFoundException` | `NOT_FOUND` |
| `UnauthorizedException` | `UNAUTHORIZED` |
| `ForbiddenException` | `FORBIDDEN` |
| `ConflictException` | `CONFLICT` |
| Any other exception | `INTERNAL_ERROR` (message hidden) |

---

## DataLoader (N+1 Prevention)

All relationship data **must** go through a DataLoader. Direct repository calls inside resolvers are forbidden.

```csharp
public class OrdersByCustomerDataLoader(
    IOrderRepository repo,
    IBatchScheduler scheduler,
    DataLoaderOptions options)
    : DataLoaderBase<Guid, IReadOnlyList<Order>>(scheduler, options)
{
    protected override async Task<IReadOnlyList<Result<IReadOnlyList<Order>>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        DataLoaderFetchContext<IReadOnlyList<Order>> context,
        CancellationToken ct)
    {
        var byCustomer = await repo.GetByCustomerIdsAsync(keys, ct);
        return keys
            .Select(id => Result<IReadOnlyList<Order>>.Resolve(
                byCustomer.GetValueOrDefault(id) ?? []))
            .ToArray();
    }
}

// In a resolver
public async Task<IReadOnlyList<Order>> GetOrdersAsync(
    Guid customerId,
    OrdersByCustomerDataLoader loader)
    => await loader.LoadAsync(customerId);
```

---

## Relay Cursor Pagination

Use `Connection<T>` for all list fields to enable cursor-based pagination.

```csharp
// Build a connection from a flat list
public Connection<Order> GetOrders([Service] IOrderRepository repo)
{
    var total  = repo.Count();
    var orders = repo.GetAll();
    return Connection<Order>.From(orders, totalCount: total);
}
```

The `Connection<T>` type returns:

```json
{
  "edges": [
    { "node": { "id": "...", "status": "pending" }, "cursor": "Y3Vyc29yOjA=" }
  ],
  "pageInfo": {
    "hasNextPage": true,
    "hasPreviousPage": false,
    "startCursor": "Y3Vyc29yOjA=",
    "endCursor": "Y3Vyc29yOjk="
  },
  "totalCount": 42
}
```

---

## What Gets Registered

| Service | Notes |
|---------|-------|
| `DomainErrorFilter` | Maps domain exceptions to structured GraphQL errors. |
| `services.AddAuthorization()` | ASP.NET Core auth wired for `[Authorize]` on resolvers. |
| Hot Chocolate server | Configured with complexity/depth limits and introspection control. |

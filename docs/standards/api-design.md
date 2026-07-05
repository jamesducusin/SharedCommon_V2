# API Design Standards

## REST

### URL Structure

```
GET    /resources           # list with pagination
GET    /resources/{id}      # single resource
POST   /resources           # create
PUT    /resources/{id}      # full replace
PATCH  /resources/{id}      # partial update
DELETE /resources/{id}      # delete

# Sub-resources
GET    /orders/{id}/items
POST   /orders/{id}/items
```

### Response Envelope

All responses use `ApiResponse<T>` from SharedCommon.ResponseBuilder:

```json
{
  "success": true,
  "data": { ... },
  "correlationId": "abc-123"
}
```

Error responses use `ProblemDetails` (RFC 9457):
```json
{
  "type": "https://errors.example.com/order-not-found",
  "title": "Order Not Found",
  "status": 404,
  "detail": "Order 123e4567 was not found",
  "instance": "/orders/123e4567",
  "correlationId": "abc-123"
}
```

### Pagination

```json
GET /orders?page=1&pageSize=20&sortBy=createdAt&sortDir=desc

{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

## gRPC

- Proto files in `Protos/` folder
- Service names: `{Domain}Service` (e.g., `OrderService`)
- Method names: verb + noun (`GetOrder`, `CreateOrder`, `ListOrders`)
- Use `google.protobuf.Timestamp` for dates
- Use `google.protobuf.wrappers` for nullable primitives
- Status codes: use gRPC canonical codes (NOT HTTP codes)

## GraphQL

- Use Hot Chocolate
- Types follow domain model naming
- Queries: read operations
- Mutations: write operations with input types
- Subscriptions: real-time event streams
- DataLoader for all relationship resolution (N+1 prevention)
- Relay cursor-based pagination for all list fields

## Versioning

REST APIs: URL versioning `/v1/orders`, `/v2/orders`
gRPC: package versioning in proto `package orders.v1`
GraphQL: additive changes only; deprecate fields before removal

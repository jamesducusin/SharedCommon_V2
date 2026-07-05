# SharedCommon.ResponseBuilder

Standardized HTTP response envelope builder.
Consistent API responses with CorrelationId, pagination, and error mapping.

## API Surface

- `ApiResponse<T>` — success response envelope
- `IResponseBuilder` — fluent builder for responses
- `ResponseBuilderExtensions` — extension methods on `Result<T>`
- `ProblemDetailsFactory` — RFC 9457 compliant error responses
- `AddSharedResponseBuilder()` — DI registration

## Rules

**Must:**
- All API responses use `ApiResponse<T>` envelope
- Error responses use `ProblemDetails` format (RFC 9457)
- CorrelationId included in all responses
- Status codes map correctly (200/201/400/401/403/404/409/422/500)
- `Result<T>` automatically maps to `ApiResponse<T>` via `ResponseBuilderExtensions`

**Forbidden:**
- Returning raw domain objects without envelope
- Exposing stack traces in responses
- Inconsistent status code usage across endpoints
- Null in the data field of a success response

## API Response Contract

```json
Success:
{ "success": true, "data": {...}, "correlationId": "..." }

List:
{ "success": true, "data": [...], "pagination": {...}, "correlationId": "..." }

Error:
{ "type": "...", "title": "...", "status": 404, "detail": "...", "instance": "...", "correlationId": "..." }
```

## Design Decisions

ResponseBuilder depends on Core (Result<T>) only — no auth, caching, or logging.

## Test Strategy

- Unit test mapping from Result<T> to ApiResponse<T>
- Test all error code mappings
- Test ProblemDetails format compliance

# SharedCommon.Grpc

gRPC service infrastructure: interceptors, health, reflection, error mapping.

## API Surface

- `LoggingInterceptor` — structured logging for all gRPC calls
- `CorrelationIdInterceptor` — propagate CorrelationId via metadata
- `ExceptionInterceptor` — map exceptions to gRPC StatusCodes
- `AddSharedGrpc(IConfiguration)` — register interceptors and gRPC defaults
- `StatusCodeMapper` — maps domain errors to gRPC status codes

## Rules

**Must:**
- All interceptors register via `AddSharedGrpc` (no manual registration)
- Errors mapped to appropriate `StatusCode` (not always `Internal`)
- CorrelationId propagated in gRPC metadata headers
- Health check registered via `grpc-health-v1`
- Reflection enabled in Development only

**Forbidden:**
- Business logic in interceptors
- Catching all exceptions and returning `OK` status
- Exposing stack traces in status detail

## gRPC Status Mapping

| Exception Type | gRPC Status |
|---------------|------------|
| NotFoundException | NOT_FOUND |
| ValidationException | INVALID_ARGUMENT |
| UnauthorizedException | UNAUTHENTICATED |
| ConflictException | ALREADY_EXISTS |
| TimeoutException | DEADLINE_EXCEEDED |
| Default | INTERNAL |

## Design Decisions

See: .claude/skills/grpc/SKILL.md

## Test Strategy

- Test interceptors in isolation with `ServerCallContext` fakes
- Integration tests use `GrpcChannel` with `TestServer`

# gRPC Skill

Implement gRPC services with proper patterns and observability.

## When to Use This Skill

Triggers:
- Creating a new gRPC service
- Adding proto definitions
- Implementing streaming RPCs
- Adding interceptors

Ask Claude explicitly: "Use grpc skill"

## Checklist

- [ ] `.proto` files in dedicated `Protos/` folder
- [ ] Protobuf versioning strategy documented
- [ ] Services implement generated base class
- [ ] CancellationToken propagated from `ServerCallContext`
- [ ] gRPC interceptors used for logging and correlation
- [ ] Error codes mapped to `StatusCode` (not exceptions)
- [ ] Health check registered via `Grpc.HealthCheck`
- [ ] Reflection enabled in development only
- [ ] TLS configured for production

## References

See: src/SharedCommon.Grpc/CLAUDE.md
See: samples/Sample.Grpc/
See: docs/standards/api-design.md

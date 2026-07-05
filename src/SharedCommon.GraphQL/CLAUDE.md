# SharedCommon.GraphQL

Hot Chocolate GraphQL infrastructure: error handling, auth, pagination, DataLoader base.

## API Surface

- `AddSharedGraphQL(IConfiguration)` — register Hot Chocolate with shared config
- `ErrorFilter` — maps domain errors to GraphQL errors
- `GraphQLAuthorizationHandler` — integrates with ASP.NET auth
- `DataLoaderBase<TKey, TValue>` — base class for DataLoader implementations
- `ConnectionBase<T>` — Relay cursor-based pagination base

## Rules

**Must:**
- DataLoader for ALL relationship resolution (N+1 prevention is mandatory)
- Relay cursor pagination on all list fields
- Authorization at field level (`[Authorize]` or `IsAuthenticated`)
- Error responses use union types, not exceptions propagated to client
- Introspection disabled in production
- Query complexity limits configured

**Forbidden:**
- Direct repository calls in resolvers (use application services)
- Returning `null` from non-nullable fields
- Overfetching without pagination
- Exposing internal exception messages in GraphQL errors

## Design Decisions

See: .claude/skills/graphql/SKILL.md

## Test Strategy

- Unit test resolvers with mocked services
- Integration tests use Hot Chocolate's `TestServer` integration
- Test authorization with authenticated and anonymous requests

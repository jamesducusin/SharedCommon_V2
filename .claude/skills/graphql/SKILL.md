# GraphQL Skill

Implement GraphQL APIs with Hot Chocolate best practices.

## When to Use This Skill

Triggers:
- Creating a new GraphQL type
- Adding queries, mutations, subscriptions
- Implementing DataLoader for N+1 prevention
- Adding authorization to fields

Ask Claude explicitly: "Use graphql skill"

## Checklist

- [ ] Types use `[GraphQLName]` for stable API naming
- [ ] DataLoader used for all relationship resolution
- [ ] Authorization applied at field level with `[Authorize]`
- [ ] Pagination uses Relay cursor spec
- [ ] Error handling uses union types (not exceptions)
- [ ] Subscriptions use ITopicEventReceiver
- [ ] Query complexity limits configured
- [ ] Introspection disabled in production

## References

See: src/SharedCommon.GraphQL/CLAUDE.md
See: samples/Sample.GraphQL/
See: docs/standards/api-design.md

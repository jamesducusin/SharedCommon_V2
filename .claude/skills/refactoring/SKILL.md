# Refactoring Skill

Safely restructure code without changing behavior.

## When to Use This Skill

Triggers:
- Extracting responsibility from a fat class
- Renaming for clarity
- Simplifying complex logic
- Removing duplication

Ask Claude explicitly: "Use refactoring skill"

## Checklist

- [ ] Tests exist and pass before starting
- [ ] Scope of change is clear (one concern at a time)
- [ ] Behavior preserved (no logic changes mixed in)
- [ ] New names are clearer than old names
- [ ] Dead code removed (not commented out)
- [ ] Architecture tests still pass after change
- [ ] No breaking API changes in public packages without version bump

## Safe Refactoring Sequence

```
1. Ensure tests cover the area to refactor
2. Make the change (extract, rename, simplify)
3. Run tests — must still pass
4. Run architecture tests — must still pass
5. Commit as "refactor: [description]"
```

## Common Patterns

**Extract Method:** Large method → smaller, named methods
**Extract Class:** God class → focused classes
**Introduce Parameter Object:** Long parameter list → typed DTO
**Replace Magic String:** Literal → named constant or enum
**Introduce Result<T>:** Exception-based flow → explicit Result

## Common Mistakes

❌ Mixing behavior change with refactoring in one commit
- Why: Makes bugs hard to isolate
- Fix: Separate commits — refactor first, then feature

❌ Refactoring without tests
- Why: Cannot verify behavior preserved
- Fix: Write characterization tests first

## References

See: docs/standards/coding-standards.md
See: .claude/skills/testing/SKILL.md

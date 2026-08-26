# Documentation schema

## Design principles

1. Explain behavior before inventories.
2. Organize by domain ownership and runtime flow, not namespaces alone.
3. Keep protocol evidence distinct from emulator implementation.
4. Keep live-state ownership distinct from relational persistence.
5. Prefer multiple focused diagrams over a single giant architecture diagram.
6. Link generated indexes from explanatory pages instead of duplicating exhaustive lists.

## README

`docs/codebase/README.md` should provide:

- what this documentation covers,
- documented commit SHA,
- navigation by developer task,
- major architecture diagram,
- evidence policy summary,
- explicit warning that protocol facts come from Habbo specs/confidence, not emulator behavior alone.

## Architecture pages

Architecture pages should answer:

- what responsibility belongs here,
- what explicitly does not belong here,
- dependencies in and out,
- lifecycle,
- state ownership,
- runtime call paths,
- validation/tests.

## Flow pages

Use sequence diagrams when useful. A flow page should identify:

- trigger,
- transport/input contract,
- orchestration entrypoint,
- authoritative domain mutation,
- persistence,
- outbound/result path,
- error/rejection paths,
- evidence.

## Generated indexes

Generated indexes should include a header like:

> Generated reference index. This file inventories code symbols and should not be used as the sole source for runtime semantics.

## Known unknowns

Do not bury important uncertainty in prose. Use:

```markdown
## Known unknowns
- **Unknown:** ...
  - Inspected: ...
  - Why unresolved: ...
  - What evidence would resolve it: ...
```

# Room hydration baseline (OQ-7)

Building the logic for 2,000 interactive floor items — the shape of a full room — best of 5 rounds.

| Path | ms for the room | µs per item |
|---|---:|---:|
| `ActivatorUtilities.CreateInstance` (today) | 1.39 | 0.69 |
| `ActivatorUtilities.CreateFactory` (compiled once per type) | 0.50 | 0.25 |

Difference for a full room: **0.88 ms** (2.7×).

Read it against what a player is waiting for. Room entry already costs a database read for the items themselves; a saving smaller than that read is a saving nobody can perceive, and the reflection stays because it is the version that needs no cache to invalidate when a plugin unloads.

## Measured on

- Microsoft Windows NT 10.0.26200.0
- 12 logical processors
- .NET 10.0.9

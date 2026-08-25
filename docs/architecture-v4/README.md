# V4 runtime refactor — how to pick this up

The architecture note itself is `docs/architecture/architecture-workflow.md` (V4, canonical). This
folder is its *memory*: what has been decided, what is in flight, and what a session is allowed to
assume without re-deriving it.

The Dashboard / API / Security workstream is separate and FROZEN:
`docs/architecture/dashboard-api-security.md`. Its PRs are never mixed with the runtime ones.

## Starting a session

1. Read `STATE.yaml`.
2. For each audit, run the SYNC diff it names:
   `git diff <vortex_sha>..HEAD -- <watched_paths>`.
   Nothing touched → the audit stays `valid`; do **not** re-audit it. Something touched → mark it
   `stale` and revalidate only those paths. A frontend commit never invalidates the commerce audit.
3. Read `decisions/` — an accepted ADR is binding. Contradicting one means announcing the conflict
   and proposing a new ADR, not quietly doing the opposite.
4. Write a slice contract from `plans/SLICE-TEMPLATE.md` before touching code. A slice that moves
   value fills the commerce extension too.

## What is mechanised

These are tests, not prose. They run in `VortexCloudFastCheck` with the rest of the suite.

| Guard | Test | Fails when |
|---|---|---|
| Interleaving manifest | `Vortex.Hosting.Tests/Architecture/InterleavingManifestTests.cs` | a method gains `[AlwaysInterleave]` without a manifest entry, an entry loses its attribute, or a category B body awaits before completing |
| Room concurrency | `Vortex.Hosting.Tests/Architecture/RoomGrainConcurrencyTests.cs` | `RoomGrain` becomes `[Reentrant]`, or a `lock` / `SemaphoreSlim` / `Task.Run` appears in `Vortex.Rooms` |
| Configured budgets | `Vortex.Hosting.Tests/Architecture/ConfiguredBudgetTests.cs` | a `Wired*` knob on `RoomConfig` has no runtime reader (RFW-101) |
| Single-silo debt | `Vortex.Hosting.Tests/Architecture/SingleSiloInventoryTests.cs` | a new process-local singleton grows the multi-silo debt unregistered |
| Workflow state | `Vortex.Hosting.Tests/Architecture/WorkflowStateTests.cs` | `STATE.yaml` loses a required key or names an audit path that does not exist |

## Layout

```
docs/architecture-v4/
├── README.md                     this file
├── STATE.yaml                    source of truth, schema-tested
├── interleaving-manifest.yaml    every interleaved grain method, with its category
├── decisions/                    ADR-000 is the V4 register; one ADR per decision after
├── plans/                        one active slice = one contract
└── reviews/                      reviewer output, per slice
```

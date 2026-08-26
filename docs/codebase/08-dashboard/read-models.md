# Dashboard read models

## Purpose

How the dashboard reads, what it reads from, and the three guards that keep a query from becoming an
outage.

## Two read services

| Service | Reads |
|---|---|
| `Api/DashboardApiService.cs` + 31 domain partials | the database, `AsNoTracking`, projected to anonymous objects |
| `Api/DashboardMonitoringReads.cs` | live runtime — grains, in-memory meters, the session gateway |

The split is documented in `DashboardMonitoringReads`' own doc: it was carved out because six
constructor parameters were used by exactly one partial.

## Three sources, three staleness profiles

| Source | Example | Freshness |
|---|---|---|
| **DB, `AsNoTracking`** | `/api/v1/forensics/audit`, `/api/v1/chatlogs`, `/api/v1/economy/ledger` | as of the query |
| **a live grain** | `/api/v1/directory/rooms/active`, `/api/v1/config`, `/api/v1/operations/cfh/queue` | now |
| **an in-memory meter or gateway** | `/api/v1/monitoring/room-performance`, session gauges | now, silo-local |

Notably, two "directory" **reads** are served by `DashboardOperationsService` rather than the read
service — `GET /rooms/active` and `/{roomId}/occupants` — because they are live Orleans reads
deliberately placed with the writes that need them.
→ [API architecture](api-architecture.md)

## The query guards

`DashboardApiService.QueryAsync` opens a short-lived `VortexDbContext` per call via
`IDbContextFactory` and disposes it in a `finally`. Three guards sit around it.

### 1 · A window that cannot be unbounded

`ResolveWindow` caps a span at **`MAX_WINDOW_DAYS = 366`** and throws
`DashboardQueryException("window_too_large")` past it. Default span is 30 days; an inverted pair is
swapped rather than rejected.

The economy ledger is buffered in memory to bucket it, which is precisely why the cap exists.

### 2 · A bad date is an error, not a dropped filter

`ParseDateTime` throws `invalid_date` rather than ignoring the value — because **dropping a filter
widens the query**, which is the opposite of failing safe.

Pinned by `DashboardQueryWindowTests` — 30-day default, explicit span, inverted pair swapped, >366
refused, absent value = no filter.

### 3 · Grouping happens in SQL

`DashboardEconomyQueryTranslationTests` asserts that three economy reads group and join **in SQL**
rather than in memory — a regression test against the classic "it works until the table is big"
change.

## Error shape

| Failure | Response |
|---|---|
| `DashboardQueryException` | 400 with its code (`window_too_large`, `invalid_date`, …) |
| anything else | 500 `internal_error`, logged with the request's correlation id, plus an `HttpAccess` audit row |

Rejection codes are shaped, not sentences: `IsDomainCode` requires `^[a-z][a-z0-9_]*$` and ≤64 chars,
which keeps EF's "Sequence contains no elements" off an operator's screen.
`DomainRejectionMessageTests` pins the boundary.

## Reads are audited

The HTTP access middleware writes an audit row for reads — `AuditCategory.Security`, action
`audit.viewed` — via `DashboardAuditEmitter`.

It **deliberately skips** two things: operation routes that returned 200 (the operation audited
itself), and `/api/login` / `/api/logout` (they audit themselves).

So "who looked at the chatlogs" is answerable. → [Operations](operations.md)

## Read capabilities

27 read-only capabilities, one per surface: `overview.read`, `audit.read`, `economy.read`,
`players.read`, `furniture.read`, `catalog.read`, `catalog.purchases.read`, `chatlogs.read`,
`groups.read`, `pets.read`, `cfh.read`, `wired.read`, `social.read`, `staff.read`,
`collectibles.read`, `achievements.read`, `bots.read`, `navigator.read`, `quests.read`, `polls.read`,
`prize_pools.read`, `mystery_box.read`, `targeted_offers.read`, `config.read`, `performance.read`,
`benchmark.read`, `server.console.read`.

Several read partials have **no endpoints file of their own** and are surfaced through two grouped
mappers, whose docs state the rule — *"Grouped in one mapper because none of them writes"*:

| Mapper | Serves |
|---|---|
| `DashboardEndpoints.Insights.cs` | Social, Staff, EconomyExtras, PlayerRewards, Inventory, Collectibles |
| `DashboardEndpoints.Stats.cs` | Groups, Pets, Cfh, CatalogPurchases, Wired |

## Live surfaces worth knowing

| Route | Reads |
|---|---|
| `/api/v1/monitoring/overview` | 30 s-cached `COUNT(*)`s **plus** `RoomDirectoryGrain.GetActiveRoomsAsync` |
| `/api/v1/monitoring/room-performance` | `RoomPerformanceAggregator` — the **same measurements** a Prometheus scrape returns, read back off the meter with a `MeterListener` → [Observability](../10-operations/observability.md) |
| `/api/v1/config` | `ServerConfigGrain.GetAllAsync` joined to `ConfigKeyCatalog` |
| `/api/v1/operations/console/stream` | `ServerConsoleFeed` over SSE → [Logging](../10-operations/logging.md) |
| `/api/v1/meta/endpoints` | reflects `EndpointDataSource` — the API describing itself |

## Presentation surfaces the pages must use

Not a detail — the difference between a usable page and an unusable one, and missed on every new page
so far:

- **Show the real artwork, never a bare id.** The read API adds
  `furnitureIconUrl = BuildFurniIconUrl(name)` and the page renders `<AssetImage src={...} />`. Same
  for avatars and guild badges via `DashboardAssetUrls`.
- **Never make an operator type an id.** Use `<PickerModal kind="furniture" />` or `kind="user"`,
  backed by `/api/v1/directory/furniture`.

`DashboardAssetUrls.ImgSrcOrigins` also feeds the CSP, so an asset host that is not declared there is
blocked by the browser rather than merely broken.

## Caching

Deliberately almost none. The exception is the overview's 30 s count cache — everything else is read
fresh, because an admin surface that shows stale numbers while an operator is acting on them is worse
than a slow one.

Client-side, `createResource(keyFn, loader, opts)` wraps TanStack Query; the key **is** the cache
identity and `refresh()` invalidates a whole family. Every write path calls it.
→ [API architecture](api-architecture.md)

## Sources

- `Vortex.Dashboard.API/Api/DashboardApiService.cs` — `QueryAsync`, `ResolveWindow`, `ParseDateTime`
- `Vortex.Dashboard.API/Api/DashboardApiService.{Audit,Config,Directory,Catalog}.cs`
- `Vortex.Dashboard.API/Api/DashboardMonitoringReads.cs`
- `Vortex.Dashboard.API/Hosting/DashboardEndpoints.{Insights,Stats,Rooms}.cs`
- `Vortex.Dashboard.API/Http/DashboardAuditEmitter.cs`
- `Vortex.Dashboard.API/Infrastructure/{DashboardAssetUrls,DashboardAssetStore}.cs`
- `Vortex.Dashboard.Tests/Hosting/{DashboardQueryWindowTests,DashboardEconomyQueryTranslationTests,DomainRejectionMessageTests}.cs`
- `AGENTS.md` — "Add dashboard capability or admin page"

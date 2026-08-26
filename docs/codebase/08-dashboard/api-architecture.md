# Dashboard API architecture

## Purpose

The operator dashboard is a second ASP.NET Core application inside the emulator process, with its own
DI container. That last part is a footgun that has taken the dashboard down at least once.

## Hosting

`Vortex.Dashboard.API/Hosting/DashboardWebHost.cs` — `DashboardWebHost : BackgroundService`,
constructed with the **root** `IServiceProvider`.

```
ExecuteAsync
  ├─ return immediately unless ObservabilityConfig.DashboardEnabled
  ├─ ListenerSecurity.ValidateListener   ← refuses cleartext off-box before a socket opens
  ├─ BuildApp → WebApplication.CreateSlimBuilder()   ← its OWN container
  └─ app.StartAsync
```

Registered from `DashboardApiModule.ConfigureServices`, which puts the singletons into the *parent*
container and adds the hosted service.

### Failure policy

`Fail` → `RequiredServiceGuard.ReportStartupFailure`. `DecideAction(enabled, required)`:

| State | Action |
|---|---|
| not enabled | `Ignore` |
| `DashboardRequired` | `FailHost` — `ExitCode = 1`, `StopApplication()` |
| otherwise | `Degrade` — the emulator keeps serving players, the service is listed in `DegradedServices` |

Default is degrade. A broken dashboard does not take the hotel down.

## The forwarded-services footgun

`CreateSlimBuilder()` builds an empty container. Anything the dashboard needs must be **hand-forwarded**
from the parent:

```csharp
private static readonly Type[] ForwardedServiceTypes = [ /* 12 types */ ];

// ForwardSingletons
foreach (var t in ForwardedServiceTypes)
    services.AddSingleton(t, rootServices.GetRequiredService(t));
```

Plus `IOptions<ObservabilityConfig>`, `IVortexMetrics` and `IVortexContextAccessor` forwarded
separately.

There are **three** ways to get this wrong, and only two are guarded.

### Failure mode A — an endpoint delegate parameter

Minimal APIs classify a handler parameter by asking the container whether it knows the type. **A type
it does not know is taken for the JSON request body.** On a GET, that throws *while the pipeline is
being built*:

```
Body was inferred but the method does not allow inferred body parameters
```

So one unlisted service takes down the **whole dashboard at startup** — not one route.

The real occurrence is named in the test's own doc: `IDatabaseBackupService` on
`GET /api/database/backups`. That route still injects it directly
(`DashboardEndpoints.Backup.cs` — `MapBackupReads`).

Guarded by `Vortex.Dashboard.Tests/Hosting/DashboardEndpointServiceTests.cs`, which builds the *same*
container with throwing factories and forces metadata inference over all `DataSources`.

### Failure mode B — a pipeline-resolved service

`ConfigurePipeline` resolves five types straight off the web app's own container:
`DashboardAssetUrls`, `DashboardAuditEmitter`, `IVortexContextAccessor`, `IVortexMetrics`,
`DashboardSessionStore`.

These are **not** endpoint parameters, so `DashboardEndpointServiceTests` cannot see them. An
unforwarded one throws `InvalidOperationException` from `GetRequiredService` — same outcome, dashboard
never starts.

> **This shipped broken.** At commit `a512ff95`, `ConfigurePipeline` resolved `IVortexContextAccessor`
> and `ForwardSingletons` did not forward it. The fix and its regression test landed in `e57f0be7` —
> the commit this documentation is written against.
>
> `DashboardPipelineServiceTests.EverythingThePipelineResolvesIsForwardedIntoItsContainer` reads
> `DashboardWebHost.cs` **as text**, regexes `app\.Services\.GetRequiredService<(\w+)>` against the
> forwarding sites, and fails on any gap. Its doc says: *"That is exactly how `IVortexContextAccessor`
> shipped."* Reading the source as text is what stops the allow-list going stale.

### Failure mode C — unguarded

`ForwardSingletons` calls `rootServices.GetRequiredService(serviceType)` **eagerly**. A type in the
list that the *parent* never registered — a domain module reordered or removed — throws there. Same
dashboard-down result, and **no test covers it**, because both tests substitute a throwing factory for
the parent.

## The request pipeline

Order matters and is worth knowing when adding middleware:

```
IVortexTraceScope            one correlation id per request; Response.OnStarting stamps X-Correlation-Id
  → security headers         CSP from DashboardAssetUrls.ImgSrcOrigins, exempt for /swagger
  → DashboardControlPlaneMetrics.Use   counts ≥400 on /api/, tags a 403 with the demanded capability
  → HSTS / HTTPS redirect
  → Swagger
  → UseAuthentication        dash_session cookie → DashboardPrincipal, capabilities re-resolved
  → UseAuthorization         one policy per capability
  → UseRateLimiter
  → ActorSecurityContext.Enter   ambient: account, session id, permissions, SteppedUpAtUtc, cid
  → HTTP access audit
  → endpoint
```

Certificate loading is **eager and failures propagate** (`ConfigureKestrel`) — the comment records that
a bad path used to silently skip HTTPS configuration, leaving a listener that looked like HTTPS and
was not.

Swagger is always on when the dashboard is; `ConfigureSwagger` re-registers the `regex` route
constraint that `CreateSlimBuilder()` trims.

## The per-domain file pattern

```
Api/DashboardApiService.<Domain>.cs           reads
Operations/DashboardOperationsService.<Domain>.cs   writes
Hosting/DashboardEndpoints.<Domain>.cs        routes
```

Counts: 28 endpoint files, 31 read partials (+ `DashboardMonitoringReads.cs`), 21 operations partials.
The shape is real but **not a clean 1:1:1**, and the departures are documented in-code:

| Departure | Where |
|---|---|
| Read partials with no endpoints file — surfaced through `Insights` / `Stats` | `DashboardEndpoints.Insights.cs` serves Social, Staff, EconomyExtras, PlayerRewards, Inventory, Collectibles; `Stats.cs` serves Groups, Pets, Cfh, CatalogPurchases, Wired. Both files' docs state the rule: *"Grouped in one mapper because none of them writes."* |
| Ops partials with no endpoints file | `Currency.cs` + `Vouchers.cs` routed from `Economy.cs`; `Privacy.cs` routed from `Moderation.cs` |
| A **read** served by the operations service | `DashboardEndpoints.Rooms.cs` binds `GET …/rooms/active` and `/{roomId}/occupants` to `DashboardOperationsService` — live Orleans reads, deliberately placed with the writes that need them |
| Split read service | `DashboardMonitoringReads.cs` carved out of `DashboardApiService`; its doc explains why (six constructor parameters used by one partial) |
| Write file with no read half | `Operations/DashboardOperationsService.Content.cs` — 954 lines, ~40 operations; its reads live in the domain read partials it authors |
| Auth self-service outside the pattern | `DashboardEndpoints.Account.cs` maps 5 routes with raw `app.MapPost`, bypassing `MapPost` deliberately (documented in the file header) |

## The operation envelope

Every write goes through `DashboardOperationsService.ExecuteAsync`, which:

1. reuses the request's correlation id and opens `IVortexTraceScope`
2. **arms `EntityChangeCapture.Begin()`** — the EF interceptor is inert outside a capture
3. timestamps
4. runs the work
5. records `IVortexMetrics.DashboardOperationCompleted(action, outcome, ms)`
6. emits **one `AuditEvent` on every exit path**

Rejection triage is by message shape: `IsDomainCode` requires `^[a-z][a-z0-9_]*$` and ≤64 chars, which
keeps EF's sentences ("Sequence contains no elements") off the operator's screen.

The HTTP access middleware deliberately skips operation routes that returned 200 (the operation
audited itself) and skips `/api/login` / `/api/logout` (they audit themselves).

## Dependencies

`Vortex.Dashboard.API.csproj` references **only** `Vortex.Database`, `Vortex.Observability` and
`Vortex.Primitives`.

Every domain it writes to is reached through an interface declared in `Vortex.Primitives` —
`ICatalogAdminService`, `IFurnitureAdminService`, `INavigatorAdminService`, `IStaffAdminService`,
`IContentAdminService`, `IQuestAdminService`, `IPollAdminService`, `IPrizePoolAdminService`,
`IMysteryBoxAdminService`, `ITargetedOfferAdminService`, `IQuestContentAdminService`,
`ICfhTicketService`, `IAccountMfaService`, `IAccountPasswordService`, `IConsoleCommandDispatcher`,
`ISessionGateway`, `IBenchmarkService` — or through `IDatabaseBackupService` / `IForensicsPurgeService`.

Implementations live in the domain projects and are resolved out of the parent container at
forwarding time.

## Configuration

All under `Vortex:Observability`, bound with `.ValidateOnStart()`:

`DashboardEnabled` (master switch, checked first) · `DashboardRequired` (fail vs degrade) ·
`DashboardFrontendEnabled` (default true) · `DashboardHost` (localhost) · `DashboardPort` (9000) ·
`DashboardHttpsEnabled` / `DashboardHttpsPort` (9443) / `DashboardCertificatePath` /
`DashboardCertificatePassword` / `DashboardHstsEnabled` · `DashboardAllowInsecureRemoteHttp` ·
`DashboardUseForwardedHeaders` + `DashboardKnownProxies` + `DashboardKnownNetworks` (**defaults
cleared so the allow-list is authoritative** — an untrusted `X-Forwarded-For` would let a client forge
the rate-limiter partition key) · `DashboardSessionLifetimeMinutes` (480, floored to 5) ·
`DashboardStepUpMinutes` (0 disables step-up) · `DashboardLoginRateLimit`.

## The SPA

Svelte 5 runes, 44 pages, 43 NAV routes. Two conventions worth knowing:

- **`createResource(keyFn, loader, opts)`** wraps TanStack Query. The key is a *function* and is the
  cache identity; `refresh()` invalidates a family. It exists rather than calling `createQuery`
  directly because two of the four states a dashboard page shows are not TanStack's — a capability
  refusal is not an error, and a transport failure has to become `describeApiError`'s sentence.
- **`createWriteOps(refresh)` is the only write path.** `ops.ask(...)` opens the confirm-reason modal;
  `postWithStepUp` retries **once** on `mfa_step_up_required` after collecting a code, and never
  retries `mfa_enrolment_required`.

Build: `dotnet build` runs `npm ci` (if needed) then `npm run build`, re-gathers the freshly hashed
`EmbeddedResource` items **inside the target** (top-of-file globs are evaluated before any target
runs), and `VerifyDashboardFrontendEmbedded` **errors if the item list is empty** — a blank dashboard
must not pass quietly now that `Assets/` is gitignored.

## Tests

12 files under `Vortex.Dashboard.Tests/Hosting/`. The three that matter most:

| File | Proves |
|---|---|
| `DashboardEndpointServiceTests` | every endpoint delegate parameter is a forwarded service or an allowed body |
| `DashboardPipelineServiceTests` | every type `ConfigurePipeline` resolves is forwarded — read from source text, both sides |
| `DashboardAuthorizationMatrixTests` | 4 facts — see [Capabilities](capabilities.md) |

Also `DashboardOperationReasonTests` (real requests through `TestServer`; asserts >20 routes refuse an
empty reason), `DashboardStepUpTests` (8 facts), `DashboardControlPlaneMetricsTests` (5),
`ActorSecurityContextTests` (5), `DashboardRequestValidationTests`, `DashboardQueryWindowTests`,
`DashboardEconomyQueryTranslationTests`, `DomainRejectionMessageTests`.

## Known unknowns

- **Unknown:** whether the eager-forwarding failure (mode C) is worth guarding.
  - Inspected: `ForwardSingletons`, both DI tests. Neither can see it — they replace the parent.
  - What would resolve it: a test that builds the real parent container and asserts every
    `ForwardedServiceTypes` entry resolves.

## Sources

- `Vortex.Dashboard.API/Hosting/DashboardWebHost.cs` — `ForwardedServiceTypes`, `ForwardSingletons`, `ConfigurePipeline`, `ConfigureKestrel`, `Fail`
- `Vortex.Dashboard.API/DashboardApiModule.cs`
- `Vortex.Dashboard.API/Hosting/DashboardEndpoints*.cs`
- `Vortex.Dashboard.API/Operations/DashboardOperationsService.cs` — `ExecuteAsync`, `Emit`, `IsDomainCode`
- `Vortex.Dashboard.API/Api/{DashboardApiService,DashboardMonitoringReads}.cs`
- `Vortex.Primitives/Hosting/{RequiredServiceGuard,ListenerSecurity}.cs`
- `Vortex.Dashboard.Tests/Hosting/*.cs`
- `Vortex.Dashboard.Web/src/lib/{resource,writeOps}.js`
- `docs/walkthroughs/add-a-dashboard-page.md`

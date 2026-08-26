# Deployment

## Purpose

The container image, the compose stack, and the supervisor — including what the compose stack does
*not* cover.

## The image

`Dockerfile`, two stages.

**Build** on `mcr.microsoft.com/dotnet/sdk:10.0` (a 9.x SDK "cannot satisfy that pin" — `global.json`
pins `10.0`):

- a restore layer built from `*/*.csproj` only, flattened by `COPY` then moved back into
  `<Name>/`. **This is why the `<Name>/<Name>.csproj` convention is load-bearing** — break it and the
  restore layer silently misses a project
- `dotnet restore Vortex.Main/Vortex.Main.csproj`, then `dotnet publish --no-restore`

**Runtime** on **`aspnet:10.0`, not `runtime`** — the comment explains why: `Vortex.WebApi` and
`Vortex.Dashboard.API` each build their own Kestrel `WebApplication` inside the process.
→ [Hosting](hosting.md)

Runs as the image's non-root `app` user (uid 1654), pre-creating `logs/`, `plugins/` and `assets/`
chowned to `app`.

```
EXPOSE 30000 30001 8080 9000
ENTRYPOINT dotnet Vortex.Main.dll
```

Orleans' silo (11111) and gateway (3000) ports are **deliberately not published** — a single
in-process silo has no reason to accept cluster traffic.

> `dotnet build` also runs `npm ci` + Vite for the dashboard SPA, so **Node 22 is a hard build
> prerequisite**. `Vortex.Dashboard.API/Assets/` is generated and gitignored, and
> `VerifyDashboardFrontendEmbedded` errors if the build would embed nothing.

## The compose stack

Two services.

**`mysql:8.0`** — host port **3307**, named volume `vortex-mysql-data`, and a healthcheck that
authenticates as the *application* user over TCP rather than as root over a socket.

**`vortex`** — with `depends_on: condition: service_healthy`, which is **mandatory, not tidiness**:
`ServerVersion.AutoDetect` opens a real connection *during DI configuration*, outside
`EnableRetryOnFailure`. Without the health gate the container races the database and dies.

Three details worth understanding before copying it:

| Detail | Why |
|---|---|
| `DOTNET_ENVIRONMENT: Development` | deliberate. Outside Development the host **refuses** localhost clustering + in-memory grain storage unless `AllowUnclusteredOutsideDevelopment` is set |
| Two env-var prefixes coexist | `VORTEX__` for the main host, **unprefixed** `serverOptions__…` for the SuperSocket child hosts → [Hosting](hosting.md) |
| `/metrics` enabled with a dev bearer token | a request through a published port arrives from the bridge gateway (172.x), **never 127.0.0.1** — so the loopback default is not "the developer's machine" inside a container |

## Port map

| Port | Serves |
|---|---|
| 30000 (dev 40000) | game TCP |
| 30001 (dev 40001) | game WebSocket |
| 8080 | web API, `/health`, `/metrics` |
| 9000 (HTTPS 9443) | operator dashboard |
| 5250 | supervisor, if run |

## The supervisor

`Vortex.Supervisor` is a **separate process** — `Sdk="Microsoft.NET.Sdk.Web"`, `OutputType Exe`, a
small `WebApplication.CreateSlimBuilder` host. Its class comment states the reason plainly:

> *"nothing living inside a process can restart that process"*

It references only `Vortex.Logging` and `Vortex.Primitives`, and shares the emulator's
`appsettings.json` by `<Content Include="../appsettings.json">`.

### What it does

| Concern | Mechanism |
|---|---|
| Child lifecycle | `EmulatorProcess.{StartAsync,StopAsync,RestartAsync,SendInputAsync}` behind a `SemaphoreSlim _gate` — restart holds the gate across **both** halves so two operators cannot interleave — plus a `Lock` ordering field access against the thread-pool `Exited` callback |
| Graceful shutdown | writes `GracefulShutdownCommand` (default `"quit"`) to the child's **stdin**, where `ConsoleCommandService` reads it; waits `GracefulShutdownTimeoutSeconds` (30) then `Kill()` |
| Working directory | `ResolveWorkingDirectory` anchors a relative path on `AppContext.BaseDirectory`, **not** the CWD |
| Console, both ways | child stdout → `ServerConsoleFeed` → SSE at `GET /api/console` (with `X-Accel-Buffering: no`); operator input via `POST /api/console` |
| Health | `EmulatorHealthProbe` polls `Vortex:Supervisor:HealthUrl` every `HealthPollSeconds`, summarising to `Healthy`/`Degraded`/`Unhealthy`/`Unknown` |

`OnExited` only claims the slot if `ReferenceEquals(_current, process)` — a late exit notification must
not mark the *replacement* as stopped.

### Auth, and why it is a shared secret

One token, compared in fixed time (`SupervisorAuth.TokenMatches`). `POST /api/session` exchanges it
for an HttpOnly cookie, because `EventSource` cannot send headers.

`SupervisorAuth`'s comment states why there is no login against the staff table: **this surface must
answer while the database is down.** That is the whole point of the process.

`SupervisorConfigValidator` refuses `PLACEHOLDER_TOKEN` and any `CHANGE_ME` marker, and refuses a
cleartext off-box bind unless `AllowInsecureRemoteHttp`.

The UI is one hand-written `wwwroot/index.html` with no npm step — *"the one moment this UI matters is
when everything else is down."*

### The health-URL coupling

`Vortex:Supervisor:HealthUrl` defaults to `http://localhost:8080/health`, which is a **`Vortex.WebApi`
route**.

> Setting `Vortex:WebApi:Enabled = false` therefore breaks the supervisor's health view silently — it
> reports `Unknown` and nothing explains why.

## Gaps

Two, both verified:

1. **The supervisor is not containerised.** It appears in neither `Dockerfile` nor
   `docker-compose.yml`. How it is meant to run alongside a container deployment is undocumented.
2. **`Vortex:Supervisor:Emulator:WorkingDirectory` defaults to a dev-tree path**
   (`../../../../Vortex.Main/bin/Debug/net10.0`) baked into the shared `appsettings.json`, and there
   is no `appsettings.Production.json` in the tree.

## Production checklist

Derived from the validators and gates rather than from a runbook:

- [ ] `VORTEX__Vortex__Database__ConnectionString` set — the shipped value is a rejected placeholder
- [ ] migrations applied — **the host does not apply them**; that is an operations decision
      → [Migrations](../07-database/migrations.md)
- [ ] `Vortex:Database:MySqlServerVersion` pinned, so `AutoDetect` does not open a connection during
      DI configuration
- [ ] if `DOTNET_ENVIRONMENT` is not Development: either real clustering/storage providers, or
      `AllowUnclusteredOutsideDevelopment`
- [ ] if `ClusteringProvider = adonet`: Orleans' own SQL scripts applied first
      (https://aka.ms/orleans-sql-scripts — **not vendored here**)
- [ ] `Vortex:Supervisor:Token` set to something real, and `WorkingDirectory` overridden
- [ ] dashboard behind TLS, or `DashboardAllowInsecureRemoteHttp` consciously set
- [ ] `Vortex:WebApi:MetricsToken` set if `/metrics` is reachable off-box
      → [Observability](../10-operations/observability.md)
- [ ] `SeedDashboardStaffActor` applied, or every room-scoped dashboard operation fails
- [ ] one silo only, or `MultiSiloReady` consciously set with the debt understood
      → [Orleans overview](../03-orleans/overview.md)

## Sources

- `Dockerfile`, `docker-compose.yml`, `global.json`
- `Vortex.Supervisor/Program.cs`, `Process/EmulatorProcess.cs`, `Health/EmulatorHealthProbe.cs`, `SupervisorAuth.cs`
- `Vortex.Supervisor/Configuration/{SupervisorConfig,SupervisorConfigValidator}.cs`
- `Vortex.Supervisor/wwwroot/index.html`
- `Vortex.Supervisor.Tests/**`
- `Vortex.Main/Extensions/HostApplicationBuilderExtensions.cs`
- `Vortex.Dashboard.API/Vortex.Dashboard.API.csproj`
- `README.md`

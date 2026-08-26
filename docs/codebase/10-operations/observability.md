# Observability

## Purpose

What the emulator measures, where it is exposed, and why `/metrics` is deliberately harder to reach
than `/health`.

## One meter

`Vortex.Observability/Diagnostics/VortexTelemetry.cs` — a single
`System.Diagnostics.Metrics.Meter` named **`"Vortex"`**, version `1.0.0`. The name is mirrored
contract-side in `Vortex.Primitives/Observability/VortexMeterNames.cs`
(`VORTEX = "Vortex"`, `ORLEANS = "Microsoft.Orleans"`).

## Instruments

21 in `VortexMetrics.cs`:

| Family | Instruments |
|---|---|
| Packets | `Vortex.packet.received` · `.duration` · `.failed` · `.dropped` |
| Rooms | `Vortex.room.tick.duration` · `Vortex.room.tick.step.duration` (tag `step`) · `Vortex.room.directory.call.duration` (tag `method`) |
| Wired | `Vortex.wired.chain.stopped` · `Vortex.wired.event` · `Vortex.wired.index.rebuilt` |
| Commerce | `Vortex.commerce.operation` · `Vortex.commerce.step.replayed` |
| Reference data | `Vortex.reference.published` |
| Furniture | `Vortex.furniture.logic.fallback` |
| Dashboard | `Vortex.dashboard.auth` · `.authorization.denied` · `.operation` · `.operation.duration` · `.http.error` |
| Audit | `Vortex.audit.write.failure` |

Plus observable gauges: `Vortex.sessions.active` and `Vortex.players.online` (read from
`ISessionGateway` **at scrape time**, so they cannot drift), `Vortex.club.active_subscribers`, and the
`Vortex.client.performance.*` family.

### The cardinality invariant

`VortexMetrics`' class comment: **tag only by bounded dimensions. Never tag by user id or room id.**

`ReferenceDataPublished` deliberately drops the monotonic version from its tags for the same reason.

### Metrics are read once

`RoomPerformanceAggregator` uses a **`MeterListener`** to read the three room histograms back out of
the meter, rather than adding a second write path in `RunTickStepAsync` (which runs 20 Hz per room).

So the dashboard's room-performance page and a Prometheus scrape are **literally the same
measurements** — they cannot disagree. Capped at `MaxSamplesPerSeries = 4096` per series.

`System.Diagnostics.Metrics` has no readable current value, which is why the listener exists at all.

## Tracing

`VortexTelemetry.ActivitySource = new(Name, Version)`.

`VortexContextAccessor.BeginScope` does four things at once:

1. sets the `AsyncLocal` ambient context
2. writes the correlation id into Orleans `RequestContext` under key `"vortex-cid"`
3. opens an `ILogger.BeginScope` dictionary (`CorrelationId`, `Operation`)
4. starts an `Activity` when `ObservabilityConfig.TracingEnabled`

`ObservabilityGrainCallFilter` (an `IIncomingGrainCallFilter`) rehydrates that context on the grain
side, and is a no-op when no cid is present — which is how a correlation id survives a grain hop.

> **No OTLP exporter is registered anywhere.** `TracingEnabled` starts spans with no listener by
> default. Distributed tracing is a hook, not a shipped capability.

## `/health`

`Vortex.WebApi/Hosting/WebApiEndpoints.cs`. **Anonymous.**

```json
{ "status": …, "database": …, "degradedServices": [ … ] }
```

`CanConnectAsync` failure is caught and reported as `database: "down"` rather than a 500 —
the endpoint's job is to answer, not to succeed. `Unhealthy` → 503; otherwise 200.

`degradedServices` is fed by `RequiredServiceGuard` → [Hosting](../01-runtime/hosting.md).

## `/metrics`

`Vortex.WebApi/Hosting/MetricsScrapingEndpoint.cs`. OpenTelemetry subscribes to the `Vortex` **and**
`Microsoft.Orleans` meters and exposes the Prometheus exporter at `WebApiConfig.MetricsPath`
(default `/metrics`).

### Why it is stricter than `/health`

Two mechanisms, both with the rationale written down:

1. **Opt-in.** `ConfigureServices` returns immediately unless `MetricsEnabled` — which defaults to
   **`false`** even when the web API itself is on.
2. **A guard ahead of the exporter's own middleware:**

| Condition | Response |
|---|---|
| `MetricsToken` empty, caller not loopback | **404** — not 403. *"an off-box caller learns nothing about whether the endpoint exists"* |
| token set, bearer absent or wrong | **401** with `WWW-Authenticate: Bearer`, compared with `CryptographicOperations.FixedTimeEquals` |

The stated reason: `/health` discloses three booleans; a scrape discloses **live population, active
room count, per-step room-tick timings and packet volumes** — reconnaissance material.

> **Container caveat:** a request through a published Docker port arrives from the bridge gateway
> (172.x), **never 127.0.0.1**. The loopback default is not "the developer's machine" inside a
> container — which is why `docker-compose.yml` sets a dev bearer token.

Pinned by `Vortex.WebApi.Tests/MetricsScrapingEndpointTests.cs` — 7 cases covering off, loopback,
off-box 404, 401, a custom path, and that `/health` is unaffected.

## Durable side-channels

Both are bounded-channel → single background writer, **never on the hot path**:

| Channel | Writer | Destination |
|---|---|---|
| `AuditChannel` | `AuditWriterService` | `audit_events`, `economy_ledger`, `item_events` |
| `ErrorGroupingChannel` | `ErrorGroupingWriterService` | `error_groups`, `error_occurrences` |

`AuditWriterService` batches, retries, and **dead-letters** to
`ObservabilityConfig.AuditDeadLetterPath` (default `logs/audit-dead-letter.jsonl`), counting failures
via `Vortex.audit.write.failure`.

> `ChannelAuditSink.Emit` **drops** the record on a `TryWrite` failure, with a warning and a counter.
> "Every operation emits a durable audit record" is true only while the writer keeps up.
> → [Dashboard operations](../08-dashboard/operations.md)

Retention: `ForensicsRetentionService` and `ForensicsPurgeService` sweep on a schedule, batched by
`RetentionBatchSize` and capped by `RetentionMaxRowsPerCycle`.

## Where the event bus fits

19 of the 25 `IEventHandler` files live in `Vortex.Observability/Events/` — audit, ledger and
forensics are the dominant consumer of the domain event bus.
→ [Plugins](../09-extensibility/plugins.md)

## Configuration

`Vortex:Observability` (`ObservabilityConfig`) carries ~60 keys: `MetricsEnabled` (true),
`TracingEnabled` (true), the audit channel / batch / retry / dead-letter block, five retention windows
plus sweep sizing, the entire dashboard listener + TLS + forwarded-headers + rate-limit + step-up
block, 9 image URL templates, and health/incident thresholds.

`Vortex:WebApi`: `MetricsEnabled` (**false**), `MetricsPath` (`/metrics`), `MetricsToken` (empty ⇒
loopback only).

## Known unknowns

- **Unknown:** how `AuditSeverity` feeds retention. `ForensicsRetentionService` prunes `audit_events`
  directly (`dbCtx.AuditEvents.Where(a => a.OccurredAt < cutoff)`, bounded batches), so the sweep
  exists — the link from `AuditSeverity`'s documented "retention policy" role to that cutoff was not
  traced.
- **Unverified:** whether tracing has ever been exported. No OTLP/Jaeger/Seq exporter is registered;
  the spans have no listener.

## Sources

- `Vortex.Observability/Diagnostics/VortexTelemetry.cs`
- `Vortex.Observability/Metrics/{VortexMetrics,ConnectionMetrics,ClubMetrics,ClientPerformanceMetrics}.cs`
- `Vortex.Observability/Context/VortexContextAccessor.cs`
- `Vortex.Observability/Runtime/{RoomPerformanceAggregator,ObservabilityGrainCallFilter,LiveStatsAggregator}.cs`
- `Vortex.Observability/Audit/{ChannelAuditSink,AuditWriterService}.cs`
- `Vortex.Observability/Runtime/{ForensicsRetentionService,ForensicsPurgeService}.cs`
- `Vortex.Observability/Configuration/ObservabilityConfig.cs`
- `Vortex.WebApi/Hosting/{WebApiEndpoints,MetricsScrapingEndpoint}.cs`
- `Vortex.WebApi/Configuration/WebApiConfig.cs`
- `Vortex.Primitives/Observability/VortexMeterNames.cs`
- `Vortex.WebApi.Tests/MetricsScrapingEndpointTests.cs`

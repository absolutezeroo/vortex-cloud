# Turbo Operations Center — TODO

Roadmap for the native observability / audit / dashboard system (`Turbo.Observability`).
Architecture is **event-driven**: core grains publish domain events (`Turbo.Primitives/Events`);
observability subscribes via `IEventHandler<T>` in `Turbo.Observability/Events`; sinks persist to
`audit_events` / `economy_ledger` / `item_events`. Any plugin can subscribe to the same events.

## Done
- CorrelationId per packet + Orleans propagation (RequestContext + grain-call filter).
- Structured logging scope + stable EventIds + `System.Diagnostics.Metrics` meter + ActivitySource.
- Durable audit pipeline: bounded channel → background writer → 3 tables (migrations applied).
- Event coverage: auth login ok/failed, catalog purchase, wallet movements (ledger, name from
  `currency_types`), LTD raffle entry/won, item created/placed/moved/picked-up, social
  (friend accepted/removed, user blocked/unblocked).
- Native admin dashboard (read-only HTTP API + minimal UI), token-gated, localhost.

## Item forensics — remaining lifecycle events
- [ ] **`ItemDeletedEvent` has no publisher.** No real destruction flow exists yet (no recycler /
      ecotron / staff-delete; `RemoveFurnitureAsync` is memory-only). Wire the event when one lands.
- [ ] `OwnerChanged` / `Traded` item events — depend on the trading system (below).

## Subsystems that are stubs (wire events when implemented)
- [ ] **Trading** (`Turbo.PacketHandlers/Inventory/Trading/*` are stubs): publish
      `TradeStarted/Completed/Cancelled` + item `Traded`/`OwnerChanged`; audit category `Economy`/`Item`.
- [ ] **Moderation** (ban/mute/kick/report handlers are stubs): publish + audit (category `Moderation`).
- [ ] **Staff / ranks** (rank hardcoded to Administrator at login, no rank persistence): audit
      rank/permission changes (category `Staff`).

## Security / privacy
- [x] **IP hashing**: auth events don't carry IP. Add HMAC-SHA256(IP) to `PlayerLoggedIn/Failed`
      events and populate an `ip_hash` column. Never store raw IPs.
- [x] **Audit the dashboard access itself** (`audit.viewed`) so console use is itself traceable.
- [x] **Fine-grained RBAC**: dashboard currently uses one shared token. Add roles
      (viewer / moderator / economy / admin).
- [ ] **Retention / RGPD**: scheduled purge job per table + per-player anonymization procedure
      (replace identity columns with a tombstone, keep ledger integrity).

## Reliability
- [x] **Audit writer**: on DB failure it logs + drops the batch (no retry/dead-letter). Add a bounded
      retry and/or a dead-letter file.

## Live monitoring (Phase 2 — not built)
- [x] In-memory `LiveStatsAggregator` (pps, errors/min, latency P50/P95, top-K rooms/abusers, active
      sessions/rooms) fed from the metrics, exposed via `/api/overview` (today it shows DB counts only).
- [x] Orleans/DB health → global `Healthy/Degraded/Critical` status.

## Error / Incident center (Phase 4 — not built)
- [x] Wire `EventRegistry`/`MessageRegistry` `OnHandlerInvokeError` (today no-op) + `PackageHandler`
      catch into an error-grouping service (fingerprint → `error_groups` / `error_occurrences`).
- [x] Incident detection (DB slow, error spikes, login-failed spikes, Orleans degraded).

## Dashboard (Phase 5 — minimal version shipped)
- [ ] Richer UI: Player Timeline, Room Inspector, Packet Center, Economy Center views (API exists for
      audit/economy/item/search; add room timeline + packet stats).
- [x] Pagination + time-range filters on the API.
- [ ] Remote access story: `HttpListener` binds `localhost`; for remote needs URL ACL / reverse proxy
      / TLS. Or migrate the dashboard to ASP.NET (`WebApplication`) — note this requires reworking the
      `IHostPluginModule(HostApplicationBuilder)` contract, since the host is `Host.CreateApplicationBuilder`.

## External export (Phase 6 — not built)
- [ ] OpenTelemetry wiring (the `Turbo` ActivitySource + `Turbo` Meter are already emitted): add
      `OpenTelemetry.Exporter.*` packages, config-gated, exporting to OTLP / Prometheus / Grafana /
      Tempo / Seq. Keep it optional (no hard dependency).

## Repo hygiene
- [ ] ~118 pre-existing files fail `dotnet csharpier check .` (exposed when the folder was
      un-gitignored). Run `dotnet csharpier format .` in a dedicated formatting-only commit to green
      the `TurboCloudFastCheck` gate.

## Tests
- [ ] Unit tests for the observability components (context accessor, sinks, event handlers, dashboard
      auth/queries).

# Flow: dashboard admin mutation

What happens between an operator clicking a button and the hotel actually changing — and the four
places that chain can break without anyone noticing.

## Trigger

`POST /api/v1/operations/<domain>/<action>` with a JSON body carrying a mandatory `reason`.

## The trace

```mermaid
sequenceDiagram
    autonumber
    participant UI as SPA (createWriteOps)
    participant MW as pipeline
    participant EP as DashboardEndpoints.MapPost
    participant OPS as DashboardOperationsService.ExecuteAsync
    participant T as grain or I&lt;Domain&gt;AdminService
    participant DB as MySQL
    participant AUD as audit channel

    UI->>MW: POST + reason (confirm modal)
    MW->>MW: trace scope · security headers · control-plane metrics
    MW->>MW: UseAuthentication — dash_session → capabilities RE-RESOLVED
    MW->>MW: UseAuthorization — the route's capability policy
    MW->>MW: ActorSecurityContext.Enter
    EP->>EP: DashboardRequestValidationFilter — reason ≥ 3 trimmed chars
    EP->>EP: DashboardStepUpFilter — only if the capability is in StepUpRequired
    EP->>OPS: the operation delegate
    OPS->>OPS: EntityChangeCapture.Begin()  ← arms the EF interceptor
    OPS->>T: the actual work
    T->>DB: write
    T->>T: reload the cache it feeds  (class c)
    OPS->>AUD: one AuditEvent, on EVERY exit path
    OPS-->>UI: outcome
```

## Step notes

### Capabilities are re-resolved, not cached in the cookie

`DashboardAuthenticationHandler.HandleAuthenticateAsync` → `DashboardAuthService.ResolveAsync` →
`IPermissionService.ResolveForAccountAsync`.

The real ceiling on a revocation is `PermissionService.CacheTtl` = **60 seconds** — unless the writer
invalidates, which `StaffAdminService` does for every affected account on every role change.

### Step-up is by capability, not by route

9 capabilities in `StepUpRequired.Capabilities`. A future route inheriting one is gated automatically.
The filter reads only the cookie, `SteppedUpAtUtc` and the clock.

Notably **not** gated: kick, ban, mute, trading lock, CFH manage — reversible and time-pressured, so
the friction is deliberately absent. → [Capabilities](../08-dashboard/capabilities.md)

### The reason is mandatory and audited

`IReasonedRequest` + `DashboardRequestValidationFilter`, attached once in `MapPost`. Floor is 3 trimmed
characters, matching the client's `reasonOk`.

`DashboardOperationReasonTests` drives **real requests through `TestServer`** to prove it, because a
filter is not endpoint metadata and the authorization-matrix test cannot see it.

### The envelope

`ExecuteAsync` reuses the request's correlation id, arms `EntityChangeCapture.Begin()`, timestamps,
runs the work, records `DashboardOperationCompleted(action, outcome, ms)`, and emits **one
`AuditEvent` on every exit path**.

Rejection triage is by message shape — `IsDomainCode` requires `^[a-z][a-z0-9_]*$` and ≤64 chars,
which keeps EF's sentences off the operator's screen.

## Where it actually lands

Four classes. **The class decides whether the hotel changed.**

| Class | Path | Live? |
|---|---|---|
| (a) | pure DB write; the reader queries per request | ✅ if that claim holds |
| (b) | grain method — the grain is the single writer | ✅ by construction |
| (c) | DB write **+** reload of the cache it feeds | ✅ if the reload succeeds |
| (d) | DB write to grain-owned state, no live path | ❌ — **none found; the one candidate is refused** |

The full per-operation classification is [Dashboard operations](../08-dashboard/operations.md).

## Where the chain breaks

Four failure points, in the order they bite.

### 1 · The write lands and the live view does not

The largest one: **a furniture-definition edit does not reach rooms that are already active**, because
`RoomItemsProvider` copies the definition snapshot into each `RoomItem` at materialization. Class (c)
done correctly, and still stale where it matters.

### 2 · The reload throws after the commit

All three rethrowing reload wrappers report failure *after* `SaveChangesAsync`. The operator sees
`operation_failed`; the row is written; the snapshot is stale until the next successful write or a
restart.

### 3 · The audit is dropped

`ChannelAuditSink.Emit` drops the record on a `TryWrite` failure — with a warning and
`Vortex.audit.write.failure`, but dropped. "Every operation emits a durable audit record" holds only
while the writer keeps up.

### 4 · There is no before-image

`ExecuteUpdateAsync` / `ExecuteDeleteAsync` bypass the change tracker, so `ops.player.forensics_purge`
audits the action and its counts but records **no `changes`**. `EntityChangeInterceptor`'s doc says so
rather than pretending otherwise.

And the admin surface is essentially **hard-delete** — 45 removal sites and 35 delete routes against a
single soft delete. Reversibility rests on the interceptor's full-row capture and on
`IDatabaseBackupService`, not on the schema.
→ [Dashboard operations](../08-dashboard/operations.md) · [Transactions](../06-economy/transactions.md)

## The one operational prerequisite

Room-scoped grain calls need a real `PlayerId` and reject `ActionContext.System`, so
`ResolveStaffActorPlayerIdAsync` resolves the reserved `__dashboard_staff__` player.

> **If the `SeedDashboardStaffActor` migration has not run, every room-scoped operation fails with
> `dashboard_staff_actor_missing`.** → [Migrations](../07-database/migrations.md)

## A worked example — `ops.player.mute`

Class (b), and a good illustration of doing it properly:

```
DashboardOperationsService.Moderation.cs
  ├─ GetPlayerPresenceGrain(target).GetActiveRoomAsync()      ← live read FIRST
  ├─ if no active room → "target_not_in_room"                 ← refuse up front,
  │                                                              rather than letting the grain no-op
  └─ GetRoomModeration(roomId).MuteUserAsync(staffActor, …)
        └─ RoomModerationSystem writes _state.MuteExpiresUtc AND the row, same method
              └─ RoomChatSystem reads that live state on the next message
```

Compare `ops.content.effect.revoke`, which deletes the row directly and sends no composer, while its
sibling `grant` routes through the grain **specifically so** the client is told.

## The write path in the SPA

`createWriteOps(refresh)` is the **only** write path. `ops.ask(endpoint, body, title, summary, …)`
opens the confirm-reason modal; `postWithStepUp` retries **once** on `mfa_step_up_required` after
collecting a code, and never retries `mfa_enrolment_required`.

## Sources

- `Vortex.Dashboard.API/Hosting/{DashboardWebHost,DashboardEndpoints,DashboardStepUp,DashboardRequestValidation}.cs`
- `Vortex.Dashboard.API/Operations/DashboardOperationsService.cs` + partials
- `Vortex.Dashboard.API/Security/{DashboardAuthenticationHandler,DashboardAuthService,ActorSecurityContext}.cs`
- `Vortex.Database/Auditing/{EntityChangeInterceptor,EntityChangeCapture}.cs`
- `Vortex.Observability/Audit/ChannelAuditSink.cs`
- `Vortex.Authentication/Permissions/{PermissionService,StaffAdminService}.cs`
- `Vortex.Rooms/Providers/RoomItemsProvider.cs`
- `Vortex.Dashboard.Web/src/lib/writeOps.js`
- `Vortex.Dashboard.Tests/Hosting/DashboardOperationReasonTests.cs`

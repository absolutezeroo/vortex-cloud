# Vortex Cloud — Consolidation Work (non-feature)

Hardening backlog, audited on `main` (commit `fa1e6e8`). Real data, not estimates.
This complements `ROADMAP.md` (which covers features): this covers the **quality of the
existing system**. Goal: close the gap between architectural ambition and implementation depth.

---

## Audit snapshot

| Metric | Value | Interpretation |
|---|---|---|
| Projects | 28 | some too small (over-modularization) |
| Smallest project | `Turbo.Contracts` (100 lines) | candidate for consolidation |
| Test projects | 2 (`WebApi.Tests`, `Rooms.Tests`) | in progress, but sensitive paths not covered |
| Test cases | 21 | WebApi (16) + wired round-trip; **policies + ledger missing** |
| Handlers | 498 | including **393 empty stubs (78%)** |
| TODO/FIXME/HACK | 327 | scattered technical debt |
| `TODO handle exceptions` | **0** | ✅ error strategy now applied |
| Catch blocks | 172 | must be audited (possible silent swallowing) |
| `EventContext` | `class { }` (empty) | dead interception seam |
| Hardcoded `= false` (Rooms) | 3 | simulated capabilities (group room...) |

**Already done (your credit):** grain error strategy (0 TODO exceptions), wired round-trip test,
WebApi migration + integration tests.

---

## Prioritized backlog

### P1 — Test sensitive paths (highest leverage)
**Evidence:** 21 cases, but `RoomSecurityPolicy`, `ModerationPolicy` (pure functions) and
`economy_ledger` are not covered. Those are places where silent regressions are expensive
(privilege escalation, duplicated currency).
**Work:** expand `Turbo.Rooms.Tests` (or a `Turbo.Permissions.Tests`) to cover both policies
(all branches) + invariants on the ledger (debit/credit, idempotence, insufficient balance
rejection, no path moves currency without ledger entry).
**Done when:** policies and ledger are covered; the gate executes these tests.

### P2 — Fill the empty shells
**Evidence:** `EventContext` is `class { }` → your `IEventBehavior` is inert (no plugin interception).
3 hardcoded `= false` in Rooms simulate capabilities (group room). Total of 2 empty classes.
**Work:** apply the two-phase event model + enriched `EventContext` (Cancel, CorrelationId, Items) —
template is already written. Wire real `isGroupRoom` / `canGroupDecorate` values (comes with groups branch).
Audit for other present but unbound interfaces.
**Done when:** `EventContext` is functional and no capability is hardcoded via `false`.

### P3 — Reduce over-modularization
**Evidence:** 28 projects; `Turbo.Contracts` (100l), `Turbo.Events` (186l), `Turbo.Logging`
(330l), `Turbo.Messages` (337l) are small.
**Work:** audit each boundary. Any folder that is not a real plugin seam and is purely
decomposed by size should be merged into the logical parent. `Turbo.Contracts` is the clearest case.
**Done when:** project count reflects true boundaries, not just disguised folders.

### P4 — Lock the quality gate
**Evidence:** structure exists (two-phase gate, csharpier, githooks), but formatting is not green
everywhere. A non-green gate is only a suggestion.
**Work:** make `csharpier` / `format` green for the entire repo, make `pre-push` blocking.
  (Verify with `dotnet csharpier --check .`.)
**Done when:** gate is green and enforced in pre-push.

### P5 — Audit catch block uniformity
**Evidence:** 0 `TODO exception` (good), but 172 catch blocks — some may swallow errors silently
(~24 empty catches observed in a previous pass).
**Work:** review catches and ensure none swallow without logging (using the shared error strategy).
**Done when:** no silent swallowing.

### P6 — Metrics hygiene
**Evidence:** `performance_logs` still carries legacy client data (`flash_version`).
**Work:** decide on `performance_logs` (delete or keep). Route high-volume telemetry to OTel meter
rather than transactional DB (see `DATA-MODEL.md` §8).
**Done when:** transactional DB no longer stores high-volume telemetry.

### P7 — Stub debt (state honesty)
**Evidence:** 393/498 handlers are empty (78%) + 327 TODOs. The project looks more complete than it is.
**Work:** by domain, decide — implement (which becomes feature work, out of immediate scope here) or
explicitly track stubs so empty scaffolding is not mistaken for implemented work. At minimum, do not
add new stubs without marking them.
**Done when:** actual progress is readable, not hidden by scaffolding.

---

## The meta-work (what prevents regressions from returning)

Enforce an **applied Definition of Done**: nothing lands in `main` without being tested on sensitive
logic, without newly introduced untracked stubs, and with gating/auditing in place. It is already
documented in `AGENTS.md` but not consistently followed — following it turns sequencing discipline
from reactive into deterministic.

---

## Recommended order

**P1 (tests) → P2 (shells) → P3 (projects) → P4 (gate)**, then P5/P6/P7 continuously.
Less exciting than feature work, but this is exactly what separates a beautiful architecture from a
production-ready hotel.

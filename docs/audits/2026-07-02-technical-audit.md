# Technical Audit — branch `copilot/technical-audit-emulateur-habbo` (2026-07-02)

Dated archive of the audit work carried on this branch. Detailed findings and their live tracking
stay in `CONSOLIDATION.md` (P1–P7 backlog) and `ROADMAP.md` (Epic 0/3); this document freezes a
dated snapshot of the audits performed, the confirmed bugs, and the fixes shipped.

Related protocol audits (separate, pre-existing): `docs/Compatibilite-Protocoles-Vortex-2026-vs-2021.md`,
`docs/Delta-WIN63-Source-vs-VotreStack.md`, `docs/Wired-WIN63-Source-vs-Stack-Checklist.md`.

---

## 1. Purchase / wallet flow audit (economy)

**Finding (confirmed, reproducible bug):** `CatalogPurchaseGrain`, `MarketplacePurchaseGrain` and
`LtdRaffleGrain` debited the wallet and then performed DB/grant work **with no compensation**. Any
failure after a successful debit (e.g. `FurnitureDefinitionNotFound` on a catalog offer with a
stale `FurniDefinitionId`) permanently lost the player's credits. No ledger entry covered that
path. Side gap: `Silver` was not refundable at all (only `Credits`/`ActivityPoints` were).

**Fix shipped:**
- Extracted a shared `IPlayerWalletGrain.ExecutePurchaseAsync` helper
  (`Turbo.Primitives/Players/Wallet/WalletPurchaseExtensions.cs`): debits once, runs the grant
  step, and **credits back automatically** (with a logged error) if the grant throws. All three
  grains now use it.
- `IPlayerWalletGrain` gained `CreditBackAsync` (generic refund, also covers `Silver`).

**Regression coverage:** `WalletPurchaseExtensionsTests.cs`
(`Turbo.Rooms.Tests/Observability/`) — insufficient balance never invokes the grant step; a
successful grant never refunds; a throwing grant triggers exactly one `CreditBackAsync` with the
original debit requests before the exception rethrows; an empty debit list is not refunded.

---

## 2. Catch-block uniformity audit (error handling, repo-wide)

**Scope:** 167 `catch` blocks across the whole repository (not just `Turbo.Rooms`), 0
`TODO handle exceptions` remaining.

**Findings and fixes from this pass:**
- `RoomAvatarTickSystem.cs` — walk-step failure recovery ran with no log line → log added.
- `RoomItemsProvider.cs` and `InventoryFurnitureLoader.cs` — silent
  `catch (Exception) { continue; }` on item load: real risk of furniture/inventory items vanishing
  with zero diagnostic trail → both now log a warning with the item id and the owning room/player
  before skipping. `InventoryFurnitureLoader` gained a constructor-injected
  `ILogger<InventoryFurnitureLoader>` (no logger before).
- Previously-flagged `Turbo.Rooms` files (`RoomService.Floor.cs`, `RoomService.Wall.cs`,
  `RoomWiredSystem.cs`, `RoomRollerSystem.cs`, `RoomPathingSystem.cs`, `RoomMapModule.Avatar.cs`,
  `RoomAvatarModule.cs`) — verified already fixed by an earlier pass (logging via
  `_roomGrain._logger.LogWarning`).

**Tracked remaining gap (out of scope for this pass):** `FurnitureWiredLogic.cs`
(`Turbo.Rooms/Object/Logic/Furniture/Floor/Wired/`) — two fully-silent `catch { }` (~l.314,
~l.340, swallowing `Activator.CreateInstance` failures when rehydrating wired
definition/type specifics) and one `catch (Exception ex) { Console.WriteLine(ex); return false; }`
(~l.362, bypassing structured logging). A proper fix requires threading an `ILogger` through the
base constructor, cascading over 6 intermediate abstract wired-kind classes + 83 concrete leaf
classes constructed via `ActivatorUtilities.CreateInstance` in
`RoomObjectLogicFeatureProcessor.cs` — scoped as its own follow-up (see `CONSOLIDATION.md` P5).

---

## 3. Sensitive-path test coverage audit (P1)

**Initial finding:** 21 test cases total; `RoomSecurityPolicy`, `ModerationPolicy` and
`economy_ledger` were not covered — exactly where silent regressions are expensive (privilege
escalation, currency duplication).

**State after this pass:**
- `RoomSecurityPolicyTests.cs` and `ModerationPolicyTests.cs` (`Turbo.Rooms.Tests/Permissions/`)
  cover every branch of both policies.
- Ledger mapping covered by `EconomyLedgerTests.cs`.
- Purchase-refund invariant covered (see §1).
- Real grain proof of concept: `RoomDirectoryGrainClusterTests.cs`
  (`Turbo.Rooms.Tests/Grains/`) spins up an in-process `TestCluster` via
  `Microsoft.Orleans.TestingHost` (aligned with pinned Orleans 9.2.1) and exercises
  `RoomDirectoryGrain` end-to-end (activation + DI wiring + grain-reference calls).
- `Turbo.Rooms.Tests`: 42/42 green; runs in the gate via `dotnet test Turbo.Cloud.sln`
  (`TurboCloudFastCheck`).

---

## 4. Quality gate audit (P4 — new gap identified)

**Finding:** `dotnet csharpier check .` is green repo-wide, but `TurboCloudQualityGate`
(`Directory.Build.targets`) runs `dotnet format ... --verify-no-changes` **only against
`Turbo.Main`** (composition root, nearly no domain logic). Analyzer/style violations in
`Turbo.Rooms`, `Turbo.PacketHandlers`, `Turbo.Players`, etc. are never gated. Widening the scope
today would immediately break the blocking gate on ~1200 pre-existing analyzer warnings — left as
a deliberate follow-up rather than forced through.

**Recommendation:** point the `format style`/`format analyzers` steps at `Turbo.Cloud.sln`, then
make `pre-push` blocking on the full gate.

---

## 5. Audit metrics summary

| Metric | Value | Interpretation |
|---|---|---|
| Projects | 29 | partial over-modularization; `Turbo.Contracts` already merged away |
| Test projects | 4 | `Rooms.Tests` covers policies, ledger, purchase-refund invariant |
| Test cases | 40+ in `Rooms.Tests` alone (was 21 total) | sensitive paths covered |
| Handlers | 501 | ~300 empty stubs (~60%), down from 78% |
| TODO/FIXME/HACK | 276 | scattered debt, down from 327 |
| `TODO handle exceptions` | 0 | error strategy applied |
| Catch blocks | 167 | 0 silent swallows outside the tracked `FurnitureWiredLogic.cs` gap |

---

## 6. Follow-up

- Prioritized backlog and current status: `CONSOLIDATION.md` (P1 ✅, P5 ✅ except tracked gap,
  P2/P3/P4/P6/P7 open).
- Feature status: `ROADMAP.md` (Epic 0 ✅, Epic 3 ✅).
- Explicitly scoped follow-ups: widen the gate's `dotnet format` scope (P4); thread `ILogger`
  through the wired-logic DI chain (`FurnitureWiredLogic.cs`, P5).

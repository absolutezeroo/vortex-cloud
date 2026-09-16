# Security audit — rooms, catalog, authorization surface

- **Audited revision**: `c07ca65` on `claude/vortex-cloud-beta-audit-apl5gn` (`main` = `0455032`).
- **Date**: 2026-09-15.
- **Question asked**: "rooms, the catalog, security — I want a real, complete audit I can lean on, because every time I get security surprises."
- **Nature**: read-only audit, plus a mechanical check shipped with the report (`scripts/hooks/check-authorization-surface.mjs`). No production code modified.

---

## 1. The one-page answer

**The good news first, because it changes what needs doing.** I traced by hand every path where a hole would be expensive — catalog purchase, credit redemption, gift opening, trading, furni manipulation, room settings, room entry, pets, account HTTP endpoints. **All are correctly protected**, and several with a subtlety you do not often see: the credit furni is consumed *before* paying so that a repeated click does not pay twice, the gift is protected by *ownership* and not by room rights ("otherwise anyone with rights opens every gift dropped at their place"), and the `price × quantity` overflow has already been found and fixed.

So your repository does not have a security *level* problem. It has a **verifiability** problem with its security, and that is exactly what produces surprises.

> **The same question — "is this actor allowed?" — gets 15 different answers in this repository, combined in 27 ways, across 179 decision points. None of them is named alike. Two of them are, when reading a diff, indistinguishable from a method that checks nothing.**

The case that sums it all up:

```csharp
// Vortex.Rooms/Grains/RoomGrain.Furni.Interactive.cs
IRoomItem? item = await FindManipulableItemAsync(ctx, itemId);   // asks the SecurityModule
_state.ItemsById.TryGetValue(itemId, out IRoomItem? item);       // asks nothing
```

One identifier apart. The first calls `SecurityModule.CanManipulateFurniAsync` and returns `null` if the actor lacks rights; the second hands the object to everybody. In a diff review, **the two lines look alike**. Eleven methods in the repository depend on the first today. The day one is written with the second, nothing — not the compiler, not the tests, not CI, not the eye — says so.

It is the same pattern as the wired audit, transposed to authorization: *a rule that exists, that is respected in practice, and that nothing verifies*. The difference is that here the rule is written in black and white in the code:

> "A handler is not a security boundary: the method is a member of a public grain interface, callable by anything in the cluster that can name the room (ROOMG-GATE-038). **The grain is the boundary.**"
> — `Vortex.Rooms/Grains/Modules/RoomSecurityModule.cs:265`

That rule is applied, and **tested for exactly two methods** (`StaffPowerGrainGateTests`). The other 119 grain methods that take an actor rely on each author having thought of it.

**What I deliver**: `check-authorization-surface.mjs`, which does not try to judge whether an authorization is correct — that is impossible with 15 idioms — but which **inventories the 179 decision points and the gate guarding each one**, and blocks when that inventory moves. Proof: replacing `FindManipulableItemAsync` with `TryGetValue` in `SetCustomStackHeightAsync` makes it print `gated-lookup -> NONE` and `exit=2` (§6).

---

## 2. Method, and why the raw numbers lie

I started with the obvious measurement: how many packet handlers check `ctx.PlayerId`? Answer: **351 out of 559**. I was about to write "208 unguarded handlers".

That was wrong, and it has to be said because it is this audit's method lesson. `AddItemToTradeMessageHandler` checks nothing:

```csharp
await _roomService.AddTradeItemsAsync(ctx.AsActionContext(), [message.ItemId], ct);
```

…because `RoomService.AddTradeItemsAsync` (`RoomService.Trading.cs:44-58`) starts with `if (ctx.PlayerId <= 0 || ctx.RoomId <= 0) return;`. The guard is delegated, and correctly so. Of the 209 "unguarded" handlers in the first count, virtually all delegate to a layer that guards.

**Every figure in this report was therefore verified by hand, file against file, before being written.** Where a claim could not be, it is marked as such in §8. The successive false positives from my own detectors — `UpdateRoomSettingsAsync` (guarded by `IsRoomOwnerAsync`), `PickUpPetAsync` (guarded by `EnsurePetOwner`), `SetCustomStackHeightAsync` (guarded by `FindManipulableItemAsync`), the supervisor endpoints (guarded by `AddEndpointFilter`) — are themselves proof of the structural defect: **if a detector written on purpose to find them gets it wrong four times, a human review will get it wrong too.**

---

## 3. What is verified healthy

An audit you can lean on has to say what it looked at **and found correct**, otherwise it only serves to worry you.

| Path | Protection observed | Reference |
|---|---|---|
| Catalog purchase | Price read server-side; quantity bounded below (`Math.Max(1, …)`) **and above** (`DEFAULT_MAX_PURCHASE_SIZE`); `price × quantity` in `long` | `CatalogPurchaseGrain.cs:53-62, 333-341` |
| Account safety lock | Re-checked server-side on the 6 catalog purchases + 2 marketplace ones, with the comment saying why | `SafetyLockGuard.cs` |
| Credit furni | **Ownership** required, not room rights; the item is consumed *before* the credits exist | `RoomGrain.Furni.Interactive.cs:431-466` |
| Gift | **Ownership** required; explicit comment on why not rights | same `:468-484` |
| Furni manipulation | `SecurityModule.CanManipulateFurniAsync` via `FindManipulableItemAsync` | `RoomGrain.Furni.Interactive.cs:46-54` |
| Room settings, floor plan, category, tags | `IsRoomOwnerAsync(actor)` on the first line | `RoomGrain.Settings.cs:57`, `RoomGrain.FloorPlan.cs` |
| Pets (pick up, move, feed…) | `EnsurePetOwner` throws `NoPermissionToManipulatePet` | `RoomPetSystem.cs:598-604` |
| Room entry | Ban, room full, password, locked door → doorbell; bypassed only by `>= Rights`, which is correct | `RoomService.cs:75, 108-140` |
| Trading | `PlayerId` **and** `RoomId` guarded at service level | `RoomService.Trading.cs` |
| Furni editor | `room.furni.edit` capability re-resolved on every request; the client flag only decides a button | `VortexApplyFurniEditMessageHandler.cs:40-47` (check at `:45`) |
| WebApi `/api/user/**` | All 26 routes authenticate, via `ctx.AccountId(sessions)` or `SelectedPlayerAsync`; `/api/ssotoken` too | `WebApiEndpoints.cs:800-805, 1837-1862` |
| Supervisor (`/start`, `/stop`, `/console`) | Token filter on the group, + token→`HttpOnly`/`SameSite=Strict` cookie exchange | `SupervisorEndpoints.cs:54` |
| Dashboard | `RequireAuthorization(capability)` per endpoint | `DashboardEndpoints.cs` |
| Network frame | Declared body length bounded to 64 KB, explicit rejection beyond | `ClientPacketDecoder.cs:31-37` |
| Per-session throughput | Token bucket, 50 packets/s sustained, burst 100, **before** any handler | `RateLimitConfig.cs`, `RateLimitBehavior` |

**`PlayerId` and `RoomId` never come from the client.** `MessageContext` receives them from `sessionGateway.GetPlayerId(SessionKey)` and from the ambient context resolved server-side (`MessageRegistry.cs:44-56`). That is the most important point in this whole list, and it is sound.

---

## 4. Confirmed defects

### SEC-10 — No connection limit, neither per IP nor global (P1, confirmed)

**What exists**: a **per-session** rate limit (50 packets/s, burst 100).
**What does not exist**: anything bounding the number of sessions. Searching the whole tree for `MaxConnections`, `ConnectionLimit`, `MaxSessionsPerIp`, `PerIp`: **no result**. `appsettings.json` declares nothing under `serverOptions` but the listeners (`ip`, `port`) — no `maxConnectionNumber`, no `backlog`.

**Consequence**: the per-session limit bounds nothing at host level. 1,000 connections from one machine = 50,000 packets/s, each able to activate a grain and hit the database. The cost to the attacker is a `connect()` loop.

**Aggravating factor**: nothing requires being authenticated to send packets (§5.2), so that throughput is available **before** any login.

*Fix*: `maxConnectionNumber` in both `serverOptions` sections (SuperSocket accepts it natively), plus a per-IP session counter in `SessionGateway.AddSessionAsync`, refusing beyond a configurable threshold. *Criterion*: the (N+1)th connection from the same IP is closed immediately.

### SEC-11 — Voucher redemption is an unauthenticated amplifier (P2, confirmed)

`Vortex.PacketHandlers/Catalog/RedeemVoucherMessageHandler.cs` in full:

```csharp
string? code = message.Code;
if (string.IsNullOrWhiteSpace(code)) { return; }
IVoucherGrain voucher = grainFactory.GetVoucherGrain(code);
await voucher.RedeemAsync(ctx.PlayerId, ct);
```

Four things, each verified:

1. **No `ctx.PlayerId <= 0` guard.** An unauthenticated socket carries `PlayerId = -1` (`SessionGateway.cs:50-51`) and reaches here.
2. **The grain key is the client's string, as is.** `GetVoucherGrain(code)` is a string-keyed Orleans grain: *one activation per distinct code tried*. The parser does `packet.PopString()` with no bound of its own (`RedeemVoucherMessageParser.cs`); only the frame size limits it, at 64 KB.
3. **One SQL query per activation.** `VoucherGrain.OnActivateAsync` does a `SELECT` on `Vouchers` for every distinct code.
4. **No attempt counting.** `TryRedeemAsync` does check already-redeemed, the redemption cap and the player's existence — but nothing counts *failures*. And on an unknown code, `RedeemAsync` additionally activates `GetPlayerPresenceGrain(-1)` to send the error.

**Consequence**: brute-forcing codes without lockout, at two grain activations and one SQL query per try, unauthenticated. Combined with SEC-10, it is the cheapest vector in the repository.

*Fix* (by order of effect): refuse `ctx.PlayerId <= 0`; bound the code's length and character set **before** naming the grain (a code has a known format); count failures per player and per IP. *Criterion*: a 200-character code, or the 11th failure in a minute, activates no grain.

### SEC-12 — `ApplyFurniEditAsync` delegates authorization to its caller (structural, confirmed, not exploitable today)

Two opposite rules coexist in the same repository.

`RoomSecurityModule.cs:265`: "*A handler is not a security boundary… The grain is the boundary.*"
`RoomGrain.Furni.Edit.cs:28-38` (the sentence at `:36`): "*the authorization question is answered once, **by the caller**, against `room.furni.edit`.*"

The second applies to the most powerful method in the room: owner reassignment, placement on a blocked tile, free altitude, definition change. Its only current caller does check the capability — **I read it** (`VortexApplyFurniEditMessageHandler.cs:45`). So it is not a hole: it is a guarantee resting on a convention nothing enforces, on the method where it would cost the most.

*Fix*: move the capability resolution into the grain (it already has `SecurityModule.HasCapabilityAsync`), and leave the handler's as a fast answer to the client. That is exactly what `HasCapabilityAsync`'s comment prescribes for the other staff powers.

### SEC-15 — The SSO ticket is replayable without limit, and the protection exists but ships disabled (P1, confirmed)

The beta report noted AUTH-01 "ticket replayable in a 30 s sliding window". It is worse than that, and more precise.

**The protection was written.** `AuthenticationService.cs:90` consumes the ticket on first use when `TicketSingleUse` is true, with the right comment: *"an observed ticket (proxy logs, browser history, unencrypted transport, Referer) can no longer be replayed at all"*.

**It ships disabled, and nothing replaces it:**

| Setting | Default | In your `appsettings.json` |
|---|---|---|
| `TicketSingleUse` | `false` (bool with no initializer) | **not declared** |
| `TicketAbsoluteLifetimeSeconds` | `null` (cap disabled) | **not declared** |
| `TicketTtlSeconds` | `30` | not declared |

The `Vortex:Authentication` section of `appsettings.json` contains **a single key**, `IpHashSecret`, whose shipped value is `"replace-with-a-production-secret"`.

**Consequence, following the `else` branch:** on each use, expiry is *pushed back* by 30 s (`slidExpiry`). The absolute cap that would bound the total is `null`. So **an observed ticket can be replayed indefinitely**, each replay extending its own validity. This is not a 30-second window: it is a 30-second window that moves for as long as the attacker keeps using it.

**And no guardrail flags it.** `AuthenticationConfigValidator` never mentions `TicketSingleUse` (0 occurrences) and explicitly allows the missing cap: *"Leave it unset to disable the cap"*. The combination "no single use **and** no cap" is the only dangerous one, and it is the only one the validation does not look at.

The default is documented as deliberate — *"Left default (TicketSingleUse = false) for compatibility with CMS integrations that reuse one ticket across reconnects"* — which is a valid reason for the option, not for the absence of a cap.

*Fix* (by order of effect, none touches code): declare `TicketAbsoluteLifetimeSeconds` in `appsettings.json` — the cap bounds replay **without breaking** CMS integrations that replay a ticket; set `TicketSingleUse` to `true` if your CMS does not do that; replace `IpHashSecret`. Then add the missing rule to the validator: refuse to start when both protections are absent at once.

### SEC-13 — Two money screens wired to empty handlers (P3, confirmed)

`Vault/WithdrawCreditVaultMessageHandler` and `Marketplace/BuyMarketplaceTokensMessageHandler` have `await ValueTask.CompletedTask` as their entire body. The client shows the screen, the player clicks, nothing happens and nothing says so. This is not a hole — it is the wired audit's "declared but inert" class, on screens where the player believes they are handling money.

### SEC-14 — The hand item is not validated (P3, confirmed, **deliberately not fixed**)

`RoomHandItemModule.Give(playerId, itemId)` only requires `itemId > 0`. Any hand-item identifier can be placed in an avatar's hand. It is cosmetic and temporary (`HandItemDurationMs`), hence P3 — but it is a value off the wire reaching a state broadcast to the whole room without being checked against a known list.

**Left open, and here is why.** Fixing it requires a valid range, and **no authority** for one exists: no enum, no table in the repository, nothing in the client's TypeScript port either (searched on `CarryItem` crossed with `max|valid|range`). Inventing a bound would risk refusing legitimate items — a player-visible regression — to close a cosmetic defect, right before a reopening. The right order is: establish the list from the client or from the data, *then* bound it.

---

## 5. The structural defect: authorization is not inspectable

### 5.1 Fifteen idioms

Inventory produced by the shipped check, over 179 decision points:

| Idiom | Occurrences | What it looks like |
|---|---:|---|
| *(none found)* | 48 | — |
| `security-module` | 24 | `await SecurityModule.CanManipulateFurniAsync(ctx)` |
| `account-id` | 23 | `ctx.AccountId(sessions)` then `Unauthorized()` |
| `actor-guard` | 16 | `if (ctx.PlayerId <= 0) return;` |
| `owner-compare` | 14 | `if (item.OwnerId != ctx.PlayerId) return null;` |
| `can-helper` | 13 | `CanEditContractAsync`, `CanUseChestAsync`, … |
| **`gated-lookup`** | **11** | **`FindManipulableItemAsync(ctx, itemId)`** |
| `selected-player` | 7 | `SelectedPlayerAsync(ctx, sessions, players, ct)` |
| `endpoint-filter` | 7 | `.AddEndpointFilter(RequireTokenAsync)` |
| `room-owner` | 6 | `if (!await IsRoomOwnerAsync(actor)) return false;` |
| `anonymous` | 3 | `.AllowAnonymous()` |
| **`ensure-helper`** | **2** | **`EnsurePetOwner(ctx, pet);`** |
| `require-authorization` | 2 | `.RequireAuthorization(Capabilities.…)` |
| `controller-level` | 1 | `GetControllerLevelAsync(ctx)` |
| *(implementation unresolved)* | 2 | — |

**27 distinct combinations.** The two in bold are the ones you cannot see:

- `gated-lookup`: the gate is **inside a lookup**. `FindManipulableItemAsync(ctx, id)` queries the security module and returns `null` if the actor lacks rights; `_state.ItemsById.TryGetValue(id, out item)` asks nothing. One identifier apart, opposite meaning.
- `ensure-helper`: `EnsurePetOwner(ctx, pet);` returns nothing and throws. On the call line, **nothing indicates a check happened** — no return value, no `if`, no `await`.

### 5.2 And no authentication gate in the pipeline

A packet's path is `PackageHandler.HandleCoreAsync` → `MessageSystem.PublishAsync` → `MessageRegistry.PublishAsync` → handler. **I read all three: none refuses an unauthenticated session.** The only rampart is the `if (ctx.PlayerId <= 0) return;` each handler writes itself — 351 out of 559 do, and among those that do not, most delegate correctly (§2).

That is the beta report's SES-02 finding, here quantified and confirmed all the way. It is of moderate severity in itself: `PlayerId` is `-1`, and the write paths stop (`CreateRoomAsync` throws on `Player -1 not found`, verified). But it turns every unguarded handler into an amplifier — that is the engine of SEC-11.

### 5.3 Why this produces surprises, precisely

An authorization hole does not appear when it is written. It appears when someone tries it. In between, the only thing that could flag it is a review — and here the review cannot:

1. **Nothing says how many gates there should be.** No list of decision points exists, hence no notion of coverage.
2. **Two idioms out of fifteen are invisible** in a diff (§5.1).
3. **The compiler sees nothing**: forgetting a gate means writing *less* code, never invalid code.
4. **The tests see nothing**: a test checks that an authorized action works. You have to write the unauthorized-actor test *on purpose*, and it exists **for two methods out of 121**.
5. **CI does not run**: `VortexCloudFastCheck` is red for other reasons, so commits go out with `--no-verify` (the beta report's QA-01 finding, still true).

Five nets, five holes, in the same place.

---

## 6. What is shipped, and the proof

`scripts/hooks/check-authorization-surface.mjs` (+ `authorization-surface-baseline.json`).

```
check-authorization-surface: OK (179 entries: 121 room-grain methods, 58 HTTP endpoints;
15 distinct gate idioms).
```

**What it does not do**: judge whether an authorization is correct. With 15 idioms that is out of reach, and pretending otherwise would give a check that falsely reassures — the worst possible outcome for a security tool.

**What it does**: inventory the 179 decision points **with the name of the gate guarding each**, and block when that inventory moves — a **new** entry, or an entry whose gate **disappeared**. It does not ask you to be right; it asks you to be explicit, once, in the diff where it is free.

**The regression proof.** I simulated the exact mistake this check exists to catch — replacing the guarded lookup with the raw read in `SetCustomStackHeightAsync`:

```
check-authorization-surface: the authorization surface moved.

  CHANGED  grain:IRoomFurni.Interactive.cs::SetCustomStackHeightAsync
           gated-lookup  ->  NONE
exit=2
```

A one-identifier change, which breaks neither the build nor a test nor a review, and which made any furni's stack height editable by any visitor. The file was restored immediately after; `git status` is clean on that file.

The baseline carries a note per entry class, including this one, which is the only thing to remember if you read nothing else:

> `gate:NONE` — "For most of them this is correct: an avatar acting on its own avatar (dance, wave, posture) needs no authority beyond being in the room. It is **not** automatically correct for anything touching someone else's property, a currency, or the room's settings. **A new `NONE` is the one to read twice.**"

**Adoption**: one line in `VortexCloudFastCheck`, which I did not add — same reason as with wired: the target is red, and the report had to ship a check without touching production code.

```xml
<Exec Command="node scripts/hooks/check-authorization-surface.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
```

---

## 7. Plan

**Before the beta**

1. **SEC-10** (2 h) — `maxConnectionNumber` on both listeners + per-IP counter in `SessionGateway`. *Criterion*: the (N+1)th connection from an IP is closed.
2. **SEC-11** (2 h) — `PlayerId` guard, code format validated before naming the grain, failure counting. *Criterion*: an out-of-format code activates no grain.
3. **Wire up the check** (10 min, after repairing the barrier in §5.3). Without it, it will never run.

**Shortly after**

4. **SEC-12** (0.5 d) — the `room.furni.edit` capability resolved in the grain; the handler's becomes a fast answer, not the guarantee.
5. **The missing test** (1 d) — `StaffPowerGrainGateTests` is the right model, applied to two methods. Extend it to the ~20 grain methods that guard something other than the actor's own avatar: for each, an actor without the right, and the assertion that nothing moved.
6. **SEC-13 / SEC-14** (2 h) — wire up or remove the two empty handlers; validate the hand item against the definitions list.

**Structural, whenever you want**

7. **Reduce 15 idioms to one** (2 to 3 d). The target is not to rewrite everything but to make the gate **visible and named** everywhere: an explicit `RoomAuthority.RequireAsync(ctx, …)`, and above all the end of `gated-lookup` — a lookup that authorizes should be called `FindItemIfAllowedAsync`, or better, return the authorization and the object separately. The shipped check measures progress: the number of distinct idioms must drop at every step.

---

## 8. Coverage and limits

**Verified by hand, on both sides**: the 15 paths in §3, the 4 defects in §4, the packet pipeline end to end (`PackageHandler` → `MessageSystem` → `MessageRegistry` → handler), `RoomSecurityModule` in full, `RoomService.Trading/Create/Doorbell`, `CatalogPurchaseGrain`, `VoucherGrain`, `RoomHandItemModule`, the 45 endpoints of `WebApiEndpoints.cs`, `SupervisorEndpoints.cs`, `ClientPacketDecoder`, `RateLimitConfig`, `SessionGateway`.

**Analyzed mechanically**: 559 packet handlers, 121 room grain methods taking an actor, 58 HTTP endpoints, 179 authorization decision points.

**Not verified — and you need to know it before leaning on this report**:

- **No execution.** No MySQL, no emulator, no client in this environment. **None of the defects was actually exploited**: they are established by reading the code, including SEC-10 and SEC-11, whose real effect depends on your hosting (a firewall or an upstream reverse proxy may already bound connections — I have not seen your deployment).
- **Cryptography** (`Vortex.Crypto`, the Diffie-Hellman handshake, RC4): not audited. It is a domain where a superficial review is worse than none.
- **Web authentication and sessions**: `AuthenticationService`, `WebApiSessionStore`, password hashing, cookie handling. The beta report covers them (AUTH-01 replayable SSO ticket, SEC-01 no account lockout, sessions not revoked on ban); I did not go back to them and **those three findings remain open**.
- **The 48 `NONE` entries**: I read about fifteen of them (avatar, pets, hand items, gifts, credit furni). The others — notably `ClaimWelcomeGiftAsync`, `HitCrackableAsync`, `UseMysteryBoxAsync`, `GetWiredDataSnapshotByFloorItemIdAsync`, `AddPlayerToRoomAsync` — are inventoried but **not reviewed one by one**. That is the first place to continue.
- **SQL injection**: not searched systematically. The repository uses EF Core with parameterized LINQ everywhere I looked, which makes the class unlikely, but "unlikely" is not "verified".
- **Plugins**: the extension surface (`Vortex.Plugins`) loads third-party code into the process. Out of scope here, and an audit in its own right.

*(Non-room grains were on this list; they are now covered, §10.)*


---

## 10. Non-room grains

Added afterwards: §8 listed non-room grains as uncovered. They are covered now, and the rule the repository states — "*callable by anything in the cluster that can name it*" — never said anything about rooms.

### 10.1 Result

**No exploitable hole.** Across 62 grain interfaces:

| Surface | Finding |
|---|---|
| **20 string-keyed grains** | 19 are singletons (`SingletonGrainId.GLOBAL`). **Only one** takes a string from the client: `IVoucherGrain`, already reported as SEC-11. So the "grain key chosen by the client" class is closed, with a single instance. |
| **24 non-room methods taking an actor** | 15 on `IGroupGrain`, 8 on `IGroupForumGrain`, 1 on `IPlayerGrain`. **All guarded.** |
| **31 methods acting on a named third party** (`targetPlayerId`) | Kick, promotion, ban, forum moderation: checked, guarded. |

Groups are the best-guarded system I have read in this repository. `KickCoreAsync` even refuses to kick the owner, with the reason written out: *"a guild without an owner has nobody who can disband or repair it"*.

### 10.2 But seven more idioms, three of them invisible

The §5 seam does not stop at rooms. Groups answer the same question with an **entirely distinct** vocabulary, which nothing links to the previous one:

| Idiom | Example | Visible at the call site? |
|---|---|---|
| inline comparison | `group.OwnerPlayerEntityId != actorId` | yes |
| `IsAdminAsync(dbCtx, group, actorId, ct)` | | yes |
| permission matrix | `Allows(settings.ModPermission, role)`, `CanRead`, `PostPermission` | yes |
| **guarded loader** | `LoadIfAdminAsync(dbCtx, actor, ct)` → `null` if refused | **no** |
| **guarded tuple loader** | `LoadForModerationAsync(...)` → `(null, ForumRole.None)` | **no** |
| **mutation wrapper** | `MutateAsAdminAsync(actor, group => { … }, ct)` | **no** |

The second deserves a close look, because it is the most misleading in the repository:

```csharp
(GroupEntity? group, ForumRole role) = await LoadForModerationAsync(dbCtx, actor, ct);
if (group is null) { return null; }
// `role` is never used again
```

The role is extracted then **thrown away**. The refusal travels on `group is null`, not on the role. A quick review sees a load that failed, not an authorization refused — and someone "cleaning up" that unused `role` would be touching the only line that says this method is guarded.

The third folds the gate into a wrapper taking a lambda: at the call site, `UpdateBadgeAsync` shows only an actor and a mutation, never a check.

### 10.3 Two grains that delegate to their caller (SEC-12 shape)

- **`StaffModerateThreadAsync(int actorPlayerId, …)`** checks **nothing**. Its only production caller is the dashboard route, which requires `Capabilities.Dashboard.OpsGuildsManage`. Correct today.
- **`SetHotelMuteAsync(PlayerId targetPlayerId, DateTime? expiresUtc)`** — a hotel-wide mute, and **no actor parameter**. The grain therefore cannot check, even in principle. `ModMuteMessageHandler` does resolve `ModerationAction.Mute` before calling. *The signature is the finding*: an actor you do not pass cannot be checked.

### 10.4 A false lead, and why it matters

I thought I had a privacy leak: `PlayerEntity.ProfileVisible` is carefully applied on the site side — private profile = header only, never a 404, so as not to offer a nickname-enumeration oracle — and **does not appear once** in `Vortex.Players`, so the full profile goes out on the game socket anyway.

It is a documented choice, on the property itself:

> "*It governs the WEB profile only. Nothing on the game socket reads it… calling this one "private" for that too would be the same promise broken a second time.*"

**So it is not a defect, and it is the session's seventh case** of a detector pointing at a deliberate decision. The point is not that I was wrong: it is that the justification lived in an XML comment on an entity property, three projects away from the handler concerned. No tool could see it, and neither could a reviewer in a hurry.

### 10.5 What this changes for the redesign

`docs/audits/authorization-redesign.md` proposes `[RequiresRoomAuthority]` on the room grain interfaces. **The scope has to widen**: `IGroupGrain` and `IGroupForumGrain` need it just as much, and `SetHotelMuteAsync` shows the case the attribute cannot handle alone — a method with no actor must first be given one.

The shipped check now covers that surface: **220 entries** (162 grain methods taking an actor, 58 HTTP endpoints), **19 distinct idioms** instead of 15.

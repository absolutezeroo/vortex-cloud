# FC-2 — the 43 anonymous headers, named

**Target client:** `WIN63-202607011411-782849652`
**Measured at:** `1fdfbb82`
**Scope:** naming and reachability only. No gameplay code.

Before this, 43 of the 56 MISSING obligations were `header:<id>` — the client's own class names are
obfuscated to `_SafeCls_NNNN` and no Vortex header id joined them, so the report could say a message
existed and nothing about what it was. That is 7% of the whole obligation surface as fog.

## Method

For each id, three lookups in the target client, in this order:

1. `com/sulake/habbo/communication/_SafeCls_2046.as` — `_composers[<id>] = _SafeCls_NNNN`
2. that class's file — package (which gives the domain) and constructor (which gives the wire shape)
3. `new _SafeCls_NNNN(` across the tree — the call site, which gives the meaning

Nothing here is inferred from a reference emulator or from a header comment.

## Result

**27 are reachable features.** They split into clusters, not scattered packets.

### Habbicons — 9 obligations, an entire feature with nothing behind it

`com/sulake/habbo/catalog/habbicons/HabbiconController.as` plus the chat-input selector and the
messenger. This is a shop, a currency sink and a chat-decoration system, and Vortex answers none of it.

| id | class | ctor | client call site |
|---|---|---|---|
| 272 | `_SafeCls_2718` | `()` | `HabbiconController.getShopData()` |
| 1494 | `_SafeCls_3805` | `(int)` | `HabbiconController.getHabbiconInfo(id)` |
| 3980 | `_SafeCls_3471` | `(int)` | `HabbiconController.buyHabbicon(id)` |
| 3036 | `_SafeCls_2394` | `(int)` | `HabbiconController.buyHabbiconCollection(id)` |
| 662 | `_SafeCls_2848` | `(int)` | `HabbiconController.claimHabbicon(id)` |
| 1808 | `_SafeCls_3482` | `(int)` | `HabbiconController.favoriteHabbicon(id)` |
| 75 | `_SafeCls_3712` | `(int)` | `HabbiconController.unfavoriteHabbicon(id)` |
| 1176 | `_SafeCls_3701` | `(int)` | `HabbiconSelector.sendTriggerHabbicon(id)` |
| 1163 | `_SafeCls_2591` | `(int,int,int)` | `messenger/MainView.as:1163`, habbicon selected in a conversation |

**Two of these move value** (`buyHabbicon`, `buyHabbiconCollection`). That puts the cluster at P0 by
the program's own priority model, not merely at "a feature nobody built".

### The rest, by cluster

| id | ctor | client call site | what it is |
|---|---|---|---|
| 831 | `()` | `HabboCatalog.cancelAllMarketPlaceOffers()` | marketplace bulk cancel |
| 1242 | `(int)` | `HabboCatalog.clearOwnMarketPlaceHistory(int)` | marketplace history clear |
| 145 | `(String)` | `SpecialItemsController.makeClaim()` | special-items free claim |
| 2668 | `(String)` | `SpecialItemsController.as:178` | special-items claim, second path |
| 1376 | `(String,String)` | `RewardTrackController.claimPrize(a,b)` | reward track prize claim |
| 1789 | `(String)` | `RewardTrackController.purchasePremium(track)` | reward track premium — **moves value** |
| 2304 | `(int,bool,bool,bool,bool)` | `DiscordSettingsController.updatePreferences(...)` | Discord link preferences |
| 2883 | `()` | `DiscordSettingsController.initComponent()` | Discord settings request |
| 1295 | `(int,int,int,int)` | `_SafeCls_1821.sendMoveUserObjectMessage(...)` | room engine: move a user object |
| 3422 | `(Number)` | `_SafeCls_1821.useObject(...)` | room engine: use object |
| 3159 | `(String)` | `FurnitureBadgeDisplayWidgetHandler.handleEngravingRequest(...)` | badge-display engraving |
| 3315 | `(int,bool)` | `CustomStackHeightWidget.sendAdjacentHeightRequest(bool)` | custom stack height |
| 3608 | `(String,String)` | `CollectiblesController.handlePreviewImageEasterEgg(...)` | collectibles preview |
| 1119 | `(bool,int,String,int)` | `SelfDonationTool.onDonate(type, amount)` | wired self-donation — **moves value** |
| 1225 | `(int,int,int,int)` | `BadgeLeaderboardDataServer.synchronizeChunk(...)` | group badge leaderboard paging |
| 521 | `(int)` | `TradingModel.requestRemoveItemFromTrading(id)` | trading: remove one item |
| 293 | `(int,int)` | `CallForHelpManager.onBullyReportEvent(...)` | bully report |
| 501 | `(bool)` | `WiredMenuSettingsTab` rollback/reload | already triaged in FC-3, deferred with reason |

`521` is worth singling out. Trading is implemented here (Epic 6) and this is the client's
*remove one item from the offer* message — the emulator answers the rest of the trade and not this.

**16 cannot be sent by this build.** See the N/A section.

## NOT_APPLICABLE — 16 obligations, with the evidence

These 16 ids are registered in `_SafeCls_2046` and their composer classes ship in the build, but the
class name appears **nowhere else in the entire decompiled tree** — not in a `new`, not as a value,
not in an import. Nothing constructs them, so the client cannot send them.

```text
129  245  983  1016  1277  1339  1576  1762
1768 1810 2020 2397  2708  3349  3517  3569
```

Recorded in `decisions.yaml` as `ADR-FC-002`, one entry each, with the class name as evidence.

**What would overturn this.** A capture carrying one of these ids on the wire, or a client-mandated
proof that the class is reached some way this reading missed — reflection by name, which nothing in
this tree does but which a grep cannot rule out absolutely. `decisions.yaml` states that rule itself
(`capture_or_client_mandated_evidence_can_invalidate_not_applicable: true`), so the decision is
revocable by evidence rather than permanent.

Note that two of them (`1576`, `1810`) live in the client's `quest` package and were the two "quest"
MISSING entries in the matrix. They are dead classes, not missing quest features.

## What this does NOT do

The ~17 PARTIAL obligations that look like legitimate no-ops — handshake `Pong` / `Disconnect` /
`UniqueID` / `VersionCheck`, the five `tracking` messages, the four phone-verification messages, the
four ad-tracking messages — are **left as PARTIAL**. Each needs its own reading of whether the client
expects a reply before it can be excluded, and "it looks like telemetry" is not the evidence
`decisions.yaml` asks for. Waving seventeen obligations through on a hunch is exactly the
score-gaming the program forbids.

## Effect on the numbers

```text
before   56 missing   119 partial   403 implemented   0 n/a
after    40 missing   119 partial   403 implemented   16 n/a
```

The 40 that remain are now named, and 27 of them belong to eight identifiable features. That is the
whole point of this slice: the count did not move because anything was built, it moved because
sixteen obligations were proven unreachable and forty stopped being anonymous.

# Dashboard — what an operator can actually act on

> A map of the gap between what the hotel stores and what the Dashboard lets someone do about it,
> plus the slices that would close it.
>
> Regenerate the numbers rather than trusting the ones written here:
>
> ```bash
> node scripts/dashboard-admin-coverage.mjs          # totals
> node scripts/dashboard-admin-coverage.mjs --list   # every entity, bucketed
> ```
>
> See `dashboard-architecture.md` for the contract any new operation has to satisfy — §11-13 in
> particular, because most of what is missing here is state a grain or a room owns.

## The shape of the problem

The Dashboard has 116 operation routes. About 27 of them act on a player or on live state; the rest
author content — catalogue, quests, polls, reward tracks, prize pools, habbicons, songs, targeted
offers, mystery box, gamedata, articles, navigator, furniture definitions.

Put another way, across the 171 entities the hotel persists:

| Bucket | Count | Meaning |
|---|---:|---|
| Managed | 77 | an operator can read it and act on it |
| Read-only | 48 | visible somewhere, no action available |
| Absent | 46 | not surfaced at all (37 worth a decision, 9 per-player UI state) |

**Everything an author builds is finely administrable. Almost nothing a player owns or does is.**
That is the finding, and it is why "remove a badge, delete a pet, delete a forum post" feels harder
than it should: two of those verbs exist, one does not.

## Method, and where it lies

`scripts/dashboard-admin-coverage.mjs` places every `DbSet` by where its name appears: under
`Admin/` or `Operations/` (managed), under `Api/` only (read-only), nowhere (absent).

It is a grep, not a call graph, so it is wrong in both directions and the script says so:

* **False negatives** — a surface that reaches data through a service living outside the Dashboard.
  `ErrorGroups` / `ErrorOccurrences` are the known case: the Incidents page shows them through
  `IncidentDetectionService` in `Vortex.Observability`. They are listed as indirect so the count
  does not lie. Others of that shape may exist.
* **False positives** — a name that is merely mentioned. **Managed is a ceiling**, not a promise
  that every verb exists for that entity. `Furnitures` counts as managed on the strength of
  `/items/grant`, and there is no way to take an item back.

Confirm any single row by opening the code.

## Read-only: visible, and nothing to be done

The social layer is almost entirely here.

* **Friends and messaging** — `MessengerFriends`, `MessengerMessages`, `MessengerBlocked`,
  `MessengerIgnored`, `MessengerRequests`
* **Guilds and their forums** — `Groups`, `GroupMembers`, `GroupForumThreads`, `GroupForumPosts`
* **Pets** — `Pets`, `PetPalettes`
* **Player belongings** — `PlayerWardrobeOutfits`, `PlayerOwnedChatStyles`, `PlayerChatStyles`,
  `PlayerSubscriptions`, `PlayerMysteryBoxKeys`, `PlayerMintTokens`
* **Sanction and room history** — `AccountBans`, `RoomBans`, `RoomEntryLogs`, `Chatlogs`
* **Economy traces** — `EconomyLedger`, `ItemEvents`, `MarketplaceOffers`, `WiredChestTransactions`,
  `NftAssets`, `NftAssetLedger`

Some of these are *meant* to be read-only: `AuditEvents`, `EconomyLedger` and `ItemEvents` are
append-only journals and editing them would defeat their purpose. The rest are not journals — they
are live objects an operator can watch and not touch.

## Absent: the blind spots that deserve a decision

Nine of the 46 are per-player interface state (which navigator categories someone collapsed, their
saved searches, forum read markers). Nobody administers those, and the script excludes them from the
count that matters. The remaining 37 include:

| Entity | Why it matters |
|---|---|
| `CommerceOperations`, `CommerceReceipts` | real-money commerce, with no surface at all |
| `WiredChests`, `WiredContracts` | they hold furniture and coins; the movements are visible through `WiredChestTransactions`, the chests themselves are not |
| `RoomMutes`, `RoomRights`, `RoomRatings` | a room's own moderation state |
| `GroupMembershipRequests`, `GroupBlockedMembers`, `GroupForumSettings` | joins, guild bans, forum configuration |
| `SecurityTickets`, `VoucherRedemptions`, `PlayerPrizeClaims`, `PlayerKickbacks`, `PlayerVaultIncomeRewards` | traces worth auditing |
| `PlayerWordFilters`, `PlayerClothing`, `PlayerFavouriteRooms` | player content |
| `MarketplaceSettings` | economy tuning |
| `RoomWiredLogs` | what wired did in a room |

Another group is reference data an author might reasonably want to edit one day, but which is not
moderation: `PetCommands`, `PetFood`, `PetLevels`, `PetVocals`, `PetCommandNames`, `GroupColors`,
`GroupBadgeParts`, `CfhCategories`, `FigureSellableSets`, `QuizQuestions`, `CatalogClubOffers`,
`CatalogFrontPageItems`, `FurniturePurchasableClothing`, `FurnitureTeleportLinks`, `AccountLevels`.

## The other half: what exists is scattered

Even where an action exists, reaching it means knowing which page hides it. To act on one player
today an operator visits three surfaces:

| Action | Where it lives |
|---|---|
| Revoke a badge, revoke an effect | `PlayerRewardsPage` |
| Ban, unban, kick, mute, trading lock, grant an item | `PlayerOperationsPanel`, inside Investigation's *Actions* tab |
| Delete a bot | `BotsPage` (refuses while the bot is placed) |

Meanwhile `EntityModal` — the popup that opens on any player or item id, **reachable from 22
pages** — carries exactly one button, and it closes the popup.

## Slices

Ordered by what a moderator gets per unit of risk. Each new operation has to answer §11: who owns
the mutable state, and what makes the hotel observe the change.

**Slice 0 — one place to act.** Move what already exists into `EntityModal`: sanctions, badges,
effects, currencies, item grant. No new server operation, no new navigation, no runtime risk — it
reuses `createWriteOps`, the mandatory-reason modal, capability gating and audit as they stand. This
is the whole of the "not fifty tabs" complaint, answered without touching the emulator.

**Slice 1 — take back what can be given. Done.** `/operations/items/revoke`, beside the grant and
under the same capability — the line badges and effects already draw, so an operator who can create
a mistake can undo it. It refuses `item_is_placed` while the item stands in a room, exactly as
`DeleteBotAsync` does, and `item_not_owned` when the id does not belong to the named player, because
an id alone would let a typo delete a stranger's furniture.

Worth knowing for the next slice: `IInventoryGrain.RemoveFurnitureAsync` looks like the obvious call
and is the wrong one. It removes the item from an in-memory dictionary and leaves the row — it is a
*transfer* primitive, for placing, trading and redeeming. Used for a revoke it would hand the item
straight back on the next reload. The revoke deletes the row and then calls `ReloadFurnitureAsync`,
which is what the trade path already does after handing items back.

**Slice 2 — forum and guild moderation.** Delete a post, delete a thread, act on a membership
request, lift a guild ban. Public content that gets reported, and today the only recourse is SQL.
Forum state is read per request in most paths, so this is closer to §11.A than it looks — confirm
before building.

**Slice 3 — room moderation state.** `RoomBans` is read-only, `RoomMutes` / `RoomRights` /
`RoomRatings` are invisible, and an active room holds all of it live. §11.B throughout: these go
through the room grain, not the table.

**Slice 4 — pets.** Nothing exists. A pet in an active room is grain-owned like a bot, so the same
refuse-or-route rule applies.

**Slice 5 — friends and messaging.** The largest read-only block and the least urgent: cutting a
friendship or deleting a message is rarely what an incident needs, and the chat history is already
readable. Worth doing after the rest, or never.

Left out on purpose: `CommerceOperations` / `CommerceReceipts`. Real-money records are not something
to grow an admin write path for casually — a read surface is the useful half, and the write half
should stay wherever the payment provider's reconciliation lives.

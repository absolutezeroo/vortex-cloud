# Transaction boundaries

## Purpose

Where a money or item change is atomic, where it is two writes that can diverge, and what each
divergence costs. Read before touching any value-moving flow.

## Atomic — one commit, verified by reading the save

| Operation | What commits together | Where |
|---|---|---|
| Wallet debit | the debit **and** its receipt, inside one `BeginTransactionAsync` | `PlayerWalletGrain.TryDebitAsync` |
| Refund | every credited currency row **and** its receipt | `PlayerWalletGrain.CreditOnceAsync` |
| Catalog grant | furniture + badges + pets + bots in one `SaveChangesAsync` (used to be four) | `InventoryGrain.GrantCatalogOfferAsync` |
| Delivery | N furniture copies **and** the delivery receipt | `GrantFurnitureDefinitionCopiesAsync` (operation overload) |
| Marketplace claim | `State → Sold` and `CreditsOwed`, one conditional `ExecuteUpdateAsync` | `MarketplacePurchaseGrain.BuyOfferAsync` |
| LTD draw | winner + serial + stock decrement + loser state | `LtdRaffleGrain.DrawAsync` |
| NFT mint | asset row + ledger line + stamp debit | `PlayerMintGrain.SpendTokensAsync` |
| Friendship | both directions **and** the request deletion | `MessengerGrain.AcceptFriendRequestsAsync` |
| Ownership swap (trade) | the swap **and** the provenance ledger row | `RoomTradingSystem.MoveAssets` — *"a chain gets that from its blocks, and a table gets it from a transaction"* |

Three explicit `BeginTransactionAsync` sites exist, all under `CreateExecutionStrategy()` with a fresh
context per attempt: `PlayerWalletGrain.TryDebitAsync`,
`RoomTradingSystem.TryPersistOwnershipSwapAsync`, and guild creation in `GroupDirectoryGrain`.

## Not atomic — named risk points

Ordered by severity. Each names the file that makes it so.

### 1. Marketplace listing does not remove the durable row 🔴

`MarketplacePurchaseGrain.MakeOfferAsync` treats `inventoryGrain.RemoveFurnitureAsync(...)` as the
pivot, but that call is a dictionary removal. The row stays in the seller's inventory; delivery to a
buyer inserts a *new* row. Net effect: listing duplicates furniture, and the same item can be listed
repeatedly.

Full evidence and the one-line SQL check: [Marketplace](marketplace.md).

### 2. `RedeemCreditsAsync` discards a `false` return 🔴

`CreditOnceAsync` returns `false` on a receipt-uniqueness loss rather than throwing. The retry path
recomputes the same deterministic id, is told "already credited", and clears `CreditsOwed` on a newer
offer too — reporting success. The seller loses those proceeds. → [Marketplace](marketplace.md)

### 3. Present opening: consume and grant are two grains, no journal 🔴

`Vortex.PacketHandlers/Room/Furniture/PresentOpenMessageHandler.cs` calls
`IRoomFurni.OpenPresentAsync` (→ `ConsumeItemAsync`, which destroys the present) and then, separately,
`ICatalogPurchaseGrain.GrantPresentContentsAsync`. **A failure between them destroys the gift.** No
`CommerceOperationId`, no receipt, no retry.

The consume-first ordering is itself correct — the reverse would let one present be clicked
repeatedly.

### 4. Gift purchase has no journal 🟠

`CatalogPurchaseGrain.Gift.cs` uses the journal-less `ExecutePurchaseAsync` overload. Two
consequences:

- The `CatalogPurchasedEvent` it publishes uses the 5-argument constructor, so `OperationId` defaults
  to `""` and `CommerceReplayGuard` always lets it through — harmless only because nothing replays it.
- `GrantWrappedGiftAsync` commits the present row and *then* calls `_furniModule.ReloadAsync(ct)`. If
  that reload throws — a cancelled token is the likely case — the exception escapes into
  `ExecutePurchaseAsync`, which **refunds the buyer while the recipient keeps the present**.

### 5. NFT store can refund after granting 🟠

`NftStoreGrain.PurchaseAsync` commits `GrantFurnitureDefinitionAsync`, then runs
`SoldCount + 1` as an `ExecuteUpdateAsync` **inside the same `try`**. If the counter update throws,
the catch refunds the emeralds and returns `Failed` — free furniture. This grain also hand-rolls
debit/refund instead of using the shared executor.

### 6. Vault claim: pay then delete, no receipt 🟠

`PlayerVaultGrain.ClaimCategoryAsync` grants each reward (each its own commit) and only afterwards
runs `ExecuteDeleteAsync` on the reward rows. A failed delete leaves both the rows and
`_pendingRewards` intact — the player can claim again and be paid twice.

### 7. NFT claims: same shape, deliberately 🟡

`PlayerNftClaimsGrain.ClaimAllAsync` grants first and sets `ClaimedAmount = ClaimLimit` afterwards,
with an explicit comment choosing *"an unclaimed prize is recoverable where a consumed one is not"*.
Correct on the loss axis; it does mean a failed save duplicates the prize.

### 8. Club payday can pay twice across a restart 🟡

`PlayerGrain.CheckAndGrantPaydayAsync` loops paying `GrantCreditsAsync` (durable, immediate) while
advancing `_state.KickbackPaydayAt` **in memory**, persisting it once at the end. If that final write
fails, the activation is safe but a restart re-hydrates the old `PaydayAt` and pays the cycle again.
No receipt, no operation id.

### 9. `TrackCreditSpendAsync` is never compensated 🟡

`CatalogPurchaseGrain` calls it inside the compensated scope, and it writes `player_kickbacks`
immediately. If the grant then throws, the wallet is refunded but the kickback base is not, inflating
the next payday. The in-code comment calls this "harmless to compensate" — it is in fact not
compensated at all.

### 10. Builders Club placement leaks a row 🟡

A real `furniture` row is inserted, and a failed placement "removes" it from the view only. →
[Inventory](inventory.md)

### 11. Room ↔ inventory ownership is eventually consistent 🟡

Up to 2 s (`RoomConfig.DirtyItemsTickMs`). No duplication — a crash reverts to the DB state — but the
client has already been told otherwise. → [Persistence](../03-orleans/persistence.md)

### 12. Block/ignore mutate memory before the DB 🟡

`MessengerGrain.Friends.cs` — `BlockUserAsync` and `IgnoreUserAsync` mutate the in-memory set, then
`return` early if either player entity is missing, leaving the set and the table disagreeing for the
life of the activation.

## The pattern that gets this right

`InventoryGrain.GrantCatalogOfferAsync`, worth copying wholesale:

1. **Pure planning first.** `CatalogFulfillmentPlanner.Plan(...)` touches no DB, no grains, no clock —
   so a definition or product error fails before anything durable happens.
2. **One commit.** Every family in a single `SaveChangesAsync`. That save *is* the pivot.
3. **Best-effort afterwards.** Cache updates, notifications and events each in their own `try/catch`
   that logs and swallows — a failed notification never un-sells a purchase.
4. **`CancellationToken.None` post-pivot.** A client disconnect must not read as "undo the sale".

## Reversibility

Reversibility does not rest on the schema. `VortexEntity` carries `DeletedAt` and a global
soft-delete query filter is applied to every mapped entity — but **45 of the 46 admin removal sites
are hard deletes**. The exception is `ContentAdminService.NftAvatars.cs`, which soft-deletes a revoked
avatar copy.

What it rests on instead:

1. `EntityChangeInterceptor` records the **full row** on a delete (*"on a delete the whole row is the
   thing being lost"*) into the audit's `changes` field, minus 14 redacted column names and truncated
   at 512 chars per value.
2. `IDatabaseBackupService`.

Neither covers `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, which bypass the change tracker — the
interceptor's own doc says so rather than pretending otherwise.
→ [Dashboard operations](../08-dashboard/operations.md)

## Known unknowns

- **Unknown:** whether risk #1 is live or whether some compensating mechanism was missed.
  - Inspected: `Vortex.Marketplace` for any `furniture` access, all `RemoveFurnitureAsync` callers,
    every `MarketplaceOfferListedEvent` consumer, `FurnitureEntity` for a marketplace FK, and the
    inventory loader's filter. All negative.
  - Why unresolved: static analysis only; the emulator was not run.
  - What would resolve it: the one-line `SELECT` on [Marketplace](marketplace.md).

## Sources

- `Vortex.Primitives/Players/Wallet/WalletPurchaseExtensions.cs`
- `Vortex.Players/Grains/PlayerWalletGrain.cs`, `PlayerGrain.cs`
- `Vortex.Inventory/Grains/InventoryGrain.Furni.cs`, `Fulfillment/CatalogFulfillmentPlanner.cs`
- `Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs`
- `Vortex.Catalog/Grains/CatalogPurchaseGrain.cs`, `.Gift.cs`, `LtdRaffleGrain.cs`
- `Vortex.Collectibles/Grains/{NftStoreGrain,PlayerVaultGrain,PlayerNftClaimsGrain,PlayerMintGrain}.cs`
- `Vortex.Social/Grains/MessengerGrain.Friends.cs`
- `Vortex.Rooms/Grains/Systems/RoomTradingSystem.cs`, `WiredTrading/WiredTradeSettlement.cs`
- `Vortex.PacketHandlers/Room/Furniture/PresentOpenMessageHandler.cs`
- `Vortex.Database/Auditing/{EntityChangeInterceptor,EntityChangeCapture}.cs`
- `Vortex.Database.Tests/Commerce/**`, `Vortex.Database.Tests/Marketplace/**`

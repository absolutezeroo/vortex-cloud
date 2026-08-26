# Collectibles

## Purpose

The NFT-flavoured subsystem, and what actually provides the guarantees a blockchain would — which is
a unique index and a transaction.

## What replaces a chain

There is no blockchain. Two database facts do its job:

```sql
-- Vortex.Database/Entities/Collectibles/NftAssetEntity.cs
[Index(nameof(ProductCode), nameof(SerialNumber), IsUnique = true)]
```

1. **`nft_assets (product_code, serial_number)` unique** — an edition cannot be oversold, regardless of
   what any in-memory count says.
2. **`nft_asset_ledger`** — an append-only provenance history, with `FromPlayerEntityId` nullable
   (null = mint) and a `Reason` from `NftAssetLedgerReason.{Minted, Traded}`.

`RoomTradingSystem.MoveAssets` writes the ledger row **inside the same transaction** as the ownership
change, and its comment states the thesis:

> *"a chain gets that from its blocks, and a table gets it from a transaction."*

The class doc on the ledger calls this *"the part of a blockchain that was actually worth having"*.

## The grains

| Grain | Key | State |
|---|---|---|
| `NftCollectionsGrain` `[KeepAlive]` | `"global"` | collection definitions — **ownership is not cached** |
| `NftStoreGrain` `[KeepAlive]` | `"global"` | shop offers; the limited-count decision point |
| `NftMintingGrain` `[KeepAlive]` | `"global"` | mintable types and stamp offers |
| `PlayerMintGrain` | player id | none |
| `PlayerNftClaimsGrain` | player id | none |
| `PlayerNftWardrobeGrain` | player id | none |
| `PlayerVaultGrain` | player id | `_pendingRewards` |

> `IPlayerVaultGrain` and `IPlayerNftWardrobeGrain` are declared in `Vortex.Primitives/Players/Grains/`
> but implemented in `Vortex.Collectibles` — the only two whose interface namespace does not match
> their implementation module.

## Minting

`PlayerMintGrain.MintAsync`:

1. the edition cap is checked as `CountAsync(asset.ProductCode == terms.ProductCode)` vs
   `terms.EditionSize` — **but the real enforcement is the unique index**, not this count
2. the source furniture is soft-deleted first, in its own `ExecuteUpdateAsync` that **drops the
   `room_id IS NULL` predicate**, with a comment naming the room persistence batch as the reason
   → [Persistence](../03-orleans/persistence.md)
3. `NftAssetEntity` + `NftAssetLedgerEntity` + the stamp debit commit in **one `SaveChangesAsync`**
   inside `SpendTokensAsync`
4. then `RemoveFurnitureAsync` on the inventory grain — **view only**, so step 2 is what makes it
   durable → [Inventory](../06-economy/inventory.md)

## The store

`NftStoreGrain` is a `"global"` singleton, so stock is serialized by the grain key.

Classname → definition resolves `OrderBy(Id).First`, **deliberately**, because a classname is not a key
(3533 duplicates). → [Furniture](../04-rooms/furniture.md)

> **It hand-rolls debit and refund** instead of using the shared `ExecutePurchaseAsync`, and it has a
> known post-pivot risk: `GrantFurnitureDefinitionAsync` commits, then `SoldCount + 1` runs inside the
> same `try`. If the counter update throws, the catch refunds the emeralds and returns `Failed` — free
> furniture. → [Transactions](../06-economy/transactions.md)

## Claims

`PlayerNftClaimsGrain.ClaimAllAsync` grants each copy (each its own commit), **then** sets
`ClaimedAmount = ClaimLimit` in one save.

`nft_claims (player_id, claim_code)` is unique, so a double-click is safe. The grant-then-record
ordering is deliberate: *"an unclaimed prize is recoverable where a consumed one is not"* — correct on
the loss axis, at the cost of duplicating on a failed save.

## Wardrobe

`PlayerNftWardrobeGrain` — `GetWardrobeAsync`, `GetWornAsync`, `WearAsync`, `RemoveWornAsync` over
`nft_avatars` / `player_nft_avatars` / `player_nft_outfit`.

These are **whole avatars**, given out at events, not clothing pieces.

`GetWardrobeAsync` queries `AsNoTracking()` per call, which is why NFT-avatar admin edits are class (a)
— independently verified. → [Dashboard operations](../08-dashboard/operations.md)

## Vault

`PlayerVaultGrain` holds `_pendingRewards` and grants income rewards. `ClaimCategoryAsync` is a
pay-then-delete with no receipt — a known risk.
→ [Transactions](../06-economy/transactions.md)

## Persistence

`nft_collections` · `nft_collection_items` · `nft_store_offers` · `nft_claims` ·
`nft_mintable_item_types` · `nft_mint_token_offers` · `nft_assets` · `nft_asset_ledger` ·
`player_collector_stats` · `player_mint_tokens` · `nft_avatars` · `player_nft_avatars` ·
`player_nft_outfit`

Unique: `nft_collections.collection_code`, `nft_collection_items (collection_id, product_code)`,
`nft_store_offers.product_code`, `nft_mintable_item_types.product_code`,
`nft_claims (player_id, claim_code)`, `nft_avatars.avatar_code`,
`player_nft_avatars (player_id, avatar_id)`. `player_collector_stats` and `player_mint_tokens` are
one-per-player.

## The project caveat

> **`Vortex.Collectibles` has no DI module and no production consumer.** `grep "using Vortex.Collectibles"`
> across production code returns nothing — only test files. `Vortex.Main.csproj` references it anyway,
> and that reference is what puts the assembly into Orleans' referenced-assembly scan.
>
> Removing it as "unused" would unregister its 7 grains. Whether they resolve at runtime today is
> **Unverified**. → [Solution map](../00-overview/solution-map.md)

`FurnitureDefinitionLookup` also lives here rather than in `Vortex.Furniture`, despite being the
canonical classname→definition helper.

## Known unknowns

- **Unknown:** whether `ops.content.claim.create` / `.delete` need a reload. They write with no reload
  and carry no comment explaining why, so whether NFT claims have a cached owner is unverified.
- **Unknown:** whether the 8 collectibles grains activate at all in production. See the caveat above.

## Sources

- `Vortex.Collectibles/Grains/{NftCollectionsGrain,NftStoreGrain,NftMintingGrain,PlayerMintGrain,PlayerNftClaimsGrain,PlayerNftWardrobeGrain,PlayerVaultGrain,FurnitureDefinitionLookup}.cs`
- `Vortex.Database/Entities/Collectibles/{NftAssetEntity,NftAssetLedgerEntity,NftClaimEntity}.cs`
- `Vortex.Rooms/Grains/Systems/RoomTradingSystem.cs` — `MoveAssets`
- `Vortex.Players/Content/ContentAdminService.NftAvatars.cs`

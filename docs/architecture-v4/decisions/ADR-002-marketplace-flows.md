# ADR-002 — Marketplace: three flows reordered, and one post-pivot revert kept

- **Status**: Accepted (2026-08-25)
- **Amends**: ADR-000, D-V4-4 (partially — see "The divergence")
- **Depends on**: ADR-001

## Context

Four marketplace flows, four ways to commit one half of a transfer before attempting the other. The
V4 note's framing was right about Buy — the `ExecuteUpdate` claim is a correct concurrency pivot and
stays — and right that the other three are simply ordered backwards.

## Decision

**MakeOffer** writes the offer first, in a new `PendingRemoval` state that no buyer can see, and only
then takes the item out of the inventory. The pivot is the withdrawal; activating the offer is the
completion. A failure before the withdrawal removes the offer row, so the listing simply never
happened. The old order — take the item, then insert the offer — destroyed the furniture outright if
anything went wrong in between: gone from the inventory and held by nothing.

**Cancel** keeps its order: `Cancelled` is committed first and is the pivot, then the item goes back.
What changes is that the restitution carries a receipt written in the inventory grain's own
transaction, and the operation id is derived from the offer rather than minted fresh. A seller
clicking cancel again after a failure therefore finishes the job instead of being handed the item a
second time. Reversing the order instead would have risked the opposite: the item back in the
inventory *and* the offer still on the market.

**Redeem** is reversed. It used to zero what the seller was owed and then credit them, so a failure in
between settled the debt in the database and paid nobody. It pays first, under a receipt, and clears
the debt afterwards. The failure that is left is a debt still shown — which the next redeem clears,
answered "already paid" by the receipt.

**Buy** keeps the claim as its pivot. The delivery is retried up to three times under a receipt before
anything else is considered.

## The divergence

D-V4-4 says the revert exists only before a pivot. Buy keeps one *after* its pivot, and this is
deliberate.

A marketplace offer holds no furniture row of its own between listing and delivery: the item left the
seller's inventory at MakeOffer and exists only as the offer until delivery creates a new row for the
buyer. Putting a claimed offer back to `Active` therefore restores exactly the state before the claim
— nothing is lost, nothing is duplicated, and the buyer is refunded by the shared purchase primitive
as the exception leaves. It is a complete compensation, not a best-effort one, which is not true of
any other post-pivot step in the codebase.

What was genuinely broken is what happened when the revert itself failed: a log line, and an offer
left `Sold`, undelivered, with a refunded buyer and a seller who thinks they sold something. That case
now transitions the operation to `NeedsIntervention`, which is a row an operator can find and an
alert that can fire.

Forward recovery for Buy — resuming the delivery instead of reverting — becomes the better answer the
moment a recovery worker exists. Until then it would mean stranding the buyer to honour a principle.

## Consequences

- `MarketplaceOfferState.PendingRemoval` exists. It is invisible to buyers (search filters `Active`)
  and excluded from the seller's own list, because the client has no status to render it with and a
  listing that has not taken its item is a moment rather than an offer.
- No migration: the state is a new value in an existing `int` column.
- Cancel and Redeem are self-healing on the player's own retry. Buy is not, and needs the worker.
- `IInventoryGrain.GrantFurnitureDefinitionCopiesAsync` gained an operation-carrying overload that
  writes its receipt in the same transaction as the rows. That is what makes any cross-grain delivery
  safe to retry, here and everywhere later.

## Evidence

`Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs`,
`Vortex.Database/Entities/Marketplace/MarketplaceOfferState.cs`,
`Vortex.Database.Tests/Commerce/MarketplaceWindowTests.cs`,
`Vortex.Database.Tests/Commerce/MarketplaceListingTests.cs`,
`Vortex.Database.Tests/Marketplace/MarketplaceClaimRaceTests.cs` (the claim, unchanged).

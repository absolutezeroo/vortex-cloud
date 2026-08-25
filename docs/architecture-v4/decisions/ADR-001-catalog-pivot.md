# ADR-001 — The pivot of a catalog, gift and targeted-offer purchase is the local grant commit

- **Status**: Accepted (2026-08-25)
- **Resolves**: OQ-2
- **Depends on**: ADR-000 (D-V4-1, D-V4-6, D-V4-7, D-V4-8)

## Context

Every flow that moves value needs one statement that separates "failure is compensated" from
"failure is retried". The V4 note left the exact point open (OQ-2) and offered two candidates:

1. the validated wallet debit, immediately after preflight;
2. a later point, with full compensation up to it.

The choice matters because it decides what a failure means. Before the pivot the answer to a failure
is to give the money back; after it, the answer is to finish the job, and refunding becomes a bug —
the goods exist.

## Decision

**The pivot is the single commit inside `InventoryGrain.GrantCatalogOfferAsync`** (and, for targeted
offers, `GrantFurnitureDefinitionCopiesAsync`).

Before it: preflight, the club gate, the price, and the wallet debit. All compensable, and
compensated by the wallet's shared purchase primitive exactly as they were.

After it: the avatar effects (another grain), the pet and bot notifications, the badge composer, the
targeted-offer counter, and the purchase event. None of them may throw back into the compensated
scope; each is logged and, where it changes state, carries a receipt.

## Why not the debit

Making the debit the pivot is the stronger guarantee — it would forbid "debited and nothing
delivered" outright — but it only holds up with a recovery worker that can actually resume a
half-delivered purchase. There is no such worker yet. Pivoting at the debit today would replace a
window that refunds with a window that strands the player, and call it progress.

The local-grant pivot is also where compensation genuinely stops being correct, which makes it the
honest place to draw the line rather than an arbitrary one. And after the consolidation (D-V4-1),
almost nothing sits between the debit and it: one grain call, whose failure already refunds and is
tested.

## Consequences

- The state "goods delivered and purchase refunded" is unreachable for catalog, gift and targeted
  offers. `Vortex.Database.Tests/Commerce` proves it from every step that can fail.
- Post-pivot steps run under `CancellationToken.None`. The client hanging up was the most common way
  a grant failed, and it was being read as "undo the sale".
  <br>*ponytail: not a host-shutdown token. The requirement is that the request's token cannot reach
  these steps; wiring `IHostApplicationLifetime` into every grain buys the ability to abandon work
  already owed to a player, which is not obviously what you want.*
- The window between the debit and the pivot remains: a crash there refunds, which is correct, but a
  crash between the wallet commit and the refund still leaves the player debited. The journal now
  records that operation as `Debited`, so it is visible; **resuming it is not yet automatic**, and
  that is the next slice.
- Marketplace keeps its own pivot: the `ExecuteUpdate` claim (ADR-000, D-V4-4).

## Evidence

`Vortex.Inventory/Grains/InventoryGrain.Furni.cs` (the single `SaveChangesAsync`, and the
`AnnounceGrantedFamiliesAsync` that follows it), `Vortex.Catalog/Grains/CatalogPurchaseGrain.cs`,
`Vortex.Catalog/Grains/PlayerTargetedOfferGrain.cs`,
`Vortex.Database.Tests/Commerce/CatalogGrantWindowTests.cs`,
`Vortex.Database.Tests/Commerce/TargetedOfferWindowTests.cs`.

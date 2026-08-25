# Walkthrough: Add a flow that moves value

`add-a-feature.md` is the shape of any packet feature. This is the extra contract a feature has to
keep when it moves **currency, items, or anything a player would file a ticket about losing** — a
catalogue purchase, a marketplace sale, a raffle payout, a gift.

The rule it exists to enforce is one sentence:

> **Nothing may end with the goods delivered and the payment refunded, or with the payment taken and
> nothing delivered.**

Everything below is machinery for keeping that true across a crash, a retry, and a silo that
disappears mid-operation. If your feature only reads, or only writes something a player can redo for
free, stop here and use `add-a-feature.md`.

---

## The pivot, and why everything hangs off it

Every value-moving flow has exactly one moment that decides the outcome. Before it, nothing durable
has happened and the operation can be abandoned. After it, the player owns something, and the only
correct response to any failure is to **finish**, however many attempts that takes.

That moment is the **pivot**. Naming it is the first design decision, before any code:

| Flow | Pivot |
|---|---|
| Catalogue purchase | the single `SaveChanges` that commits furniture + badges + pets + bots (ADR-001) |
| Marketplace buy | the `ExecuteUpdate` that claims the offer for this buyer (ADR-002) |
| Marketplace cancel | the delivery receipt |

Two consequences, and they are not negotiable:

- **Before the pivot, work must be compensable.** A debit taken before the pivot has to be
  refundable, and the refund has to run on the failure path.
- **After the pivot, work must be idempotent and retryable.** It may be attempted again, by a retry
  or by the recovery sweep, and the second attempt must not deliver twice.

## Step 1 — Open an operation

```csharp
CommerceOperationId operationId = CommerceOperationId.New();

await journal.OpenAsync(
    operationId, CommerceOperationKind.CatalogPurchase, playerId, detail: null, ct);
```

`ICommerceJournal` is a plain table, deliberately — queryable by whoever is on call at three in the
morning, not a grain per operation. `CommerceOperationId.New()` is a v7 GUID, so the table sorts by
time for free.

Use `Deterministic(kind, entityId)` instead when the operation is *about* one row and must never be
opened twice for it — cancelling marketplace offer 4711 is the same operation however many times the
button is pressed. `OpenIfNewAsync` then returns the existing one.

## Step 2 — Work out what you owe, before you take anything

```csharp
FulfillmentPlan plan = planner.Plan(offer, product);
```

Purely, deterministically, and **before the debit**. A plan that cannot be computed is a purchase
that must not start; discovering that after taking the credits means writing a refund path for a case
that never had to exist. `CatalogFulfillmentPlanner` is the worked example — no I/O, no grain calls,
fully unit-tested.

## Step 3 — Debit, carrying the operation id

```csharp
if (!await wallet.TryDebitAsync(requests, operationId, ct))
{
    await journal.TransitionAsync(operationId, CommerceOperationState.FailedBeforePivot, ct);

    return PurchaseResult.NotEnoughCredits;
}
```

The wallet writes its receipt **inside the transaction the debit already opens**. That is the whole
trick and it is worth understanding rather than copying: a retry of the same `operationId` hits the
unique index on `(operationId, stepKey)`, the transaction rolls back, and the wallet returns the
earlier success without debiting twice and without publishing a second currency event.

So: never write a receipt in a transaction of its own. A receipt that commits separately from the work
it certifies is a lie waiting for a crash between the two.

## Step 4 — Cross the pivot in one commit

```csharp
await inventory.GrantFurnitureDefinitionCopiesAsync(
    definitionId, extraData, copies, operationId, CommerceStepKeys.LOCAL_GRANT, ct);

await journal.TransitionAsync(operationId, CommerceOperationState.Pivoted, ct);
```

Everything the player gets goes in **one** `SaveChanges`, with the receipt. `InventoryGrain.Furni`
is the reference: furniture, badges, pets and bots commit together, because a partial grant is a
support ticket that nobody can reconstruct.

Anything that is not part of what the player owns — telling the room, refreshing a tracker, notifying
another grain — moves **after** the commit, logs its own failures and swallows them. See
`AnnounceGrantedFamiliesAsync`: it takes `CancellationToken.None` on purpose, because a host shutting
down must not turn a completed grant into a half-announced one.

## Step 5 — Finish, and let the event leave from the journal

```csharp
await journal.CompleteWithRelayAsync(
    operationId,
    new CatalogPurchasedEvent { ..., OperationId = operationId.ToString() },
    ct);
```

The terminal transition and the event are one write. `CommerceRelayService` picks it up and publishes
at-least-once; a crash between committing the grant and publishing the event no longer loses it,
which is the dual-write problem the outbox is here to solve.

At-least-once means consumers see duplicates. Every consumer of a relayed event dedupes:

```csharp
if (!await CommerceReplayGuard.FirstDeliveryAsync(journal, e.OperationId, "quest", ct))
{
    return;
}
```

**One key per consumer.** Quests and daily tasks both consume `CatalogPurchasedEvent`; sharing a key
would mean whichever ran first silently ate the other's delivery.

## Step 6 — Decide what a failure after the pivot does

Two answers, and the flow has to pick one deliberately:

- **Retry, then escalate.** Marketplace buy attempts delivery `POST_PIVOT_ATTEMPTS` times and, if the
  compensation also fails, transitions to `NeedsIntervention` — which is a metric, an alert, and a
  row an operator can find. Nothing is silently lost.
- **Nothing.** If the post-pivot work is announcements only, log and move on. The player has their
  goods; a missed room notification is not a commerce failure.

What a flow may **not** do is refund after the pivot. ADR-002 documents the one deliberate exception
and why it is one: a marketplace offer holds no furniture row between listing and delivery, so there
is nothing yet to have delivered twice.

## Step 7 — Prove it by breaking it

A commerce slice is not done because it works. It is done when it survives being interrupted at every
step:

```csharp
[Theory]
[InlineData(CommerceFaultStep.AfterDebit)]
[InlineData(CommerceFaultStep.AfterGrant)]
[InlineData(CommerceFaultStep.AfterReceipt)]
public async Task NoWindowLeavesGoodsDeliveredAndPaymentRefunded(CommerceFaultStep step) { ... }
```

`Vortex.Database.Tests/Commerce/` holds the harness. It runs the real `InventoryGrain` against EF and
throws at a named step, and the assertions are on the **final business state** — the balance, the rows
in the inventory, the offer's state — never on "a refund was called". A test that asserts a refund was
requested passes just as happily when the refund throws.

---

## The checklist

1. The pivot is named, in a comment or an ADR, before the code is written.
2. Every pre-pivot durable effect has a compensation, and the failure path runs it.
3. Every post-pivot step takes an `operationId` and a `stepKey`, and writes its receipt in the same
   transaction as its work.
4. The critical event leaves from the journal, not from the call site.
5. Every consumer of that event dedupes, under its own key.
6. A post-pivot failure either retries to exhaustion and escalates, or is explicitly harmless.
7. A fault-injection test exists for each step, asserting final business state.

## What this replaces

Doing it by hand: a debit, then a grant, then a `try`/`catch` that refunds. That shape reads as
careful and loses money, because the window between the two is exactly where a crash puts the player
in the state nobody can undo. `PR-C1` characterised eight of those windows in this codebase by
breaking them on purpose; all eight are closed, and the machinery above is what closed them.

See `docs/architecture-v4/decisions/ADR-001-catalog-pivot.md` and `ADR-002-marketplace-flows.md`.

# Slice contract — <name>

Copy this file to `plans/<slice-id>.md` before writing code, fill it, and add the slice id to
`STATE.yaml: active_slices`. A slice with no contract is the drift this workflow exists to prevent.

## Goal
One sentence. What is true after the slice that is not true now.

## Preconditions
Which audits must be `valid`, which ADRs must be accepted, which earlier PR must have landed.

## Allowed files
Explicit list or globs. Anything outside is out of scope.

## Forbidden files
The neighbours it would be tempting to "fix on the way".

## Invariants preserved
The named semantics this slice must not move. Cite the test that pins each one.

## behavior_change
`none` | `<explicit description>`. A structural slice declares `none` and is held to it.

## Semantic risks
The things that are easy to break silently while moving this code.

## Required tests
Named tests, written before or with the change. "The suite still passes" is not an entry.

## Abort / rollback
What to revert, and how to tell the slice failed.

## Done
The observable condition. Not "the code is written".

---

## Commerce extension — mandatory for any slice that moves value

Required for anything touching Catalog, Gift, Targeted Offers, Marketplace, Wallet or Inventory
fulfilment (ADR-000, D-V4-1 to D-V4-8).

| Field | Value |
|---|---|
| OperationId | which `CommerceOperationKind`, who mints it |
| Pivot | the exact statement that makes the operation irreversible |
| CompensableBeforePivot | steps that may be undone, and by what |
| RetryableAfterPivot | steps that must be replayable, and the receipt or natural idempotence proving it |
| IdempotencyKey / StepKey | the key each post-pivot step writes |
| RecoveryOwner | who resumes an interrupted operation |
| CriticalOutboxEvents | the events that must survive a crash after the pivot |
| Crash points tested | one row per frontier, with the final business state asserted |

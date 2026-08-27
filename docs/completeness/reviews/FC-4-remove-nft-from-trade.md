# FC-4 — taking a Relic back off the trade table

**Target client:** `WIN63-202607011411-782849652`
**Chosen from:** `docs/completeness/reviews/FC-2-triage.md`, id `521`

## OBLIGATION

`incoming/header:521` — MISSING. Named in FC-2 as
`TradingModel.requestRemoveItemFromTrading()`, NFT branch.

## CLIENT ENTRYPOINT

`com/sulake/habbo/inventory/trading/TradingModel.as:964`

```as3
_loc5_ = ownUserNftItems.getWithIndex(param1 - _loc2_);   // past the furniture rows
_loc4_ = _loc5_.pop(1);
if(_loc4_ != null && _loc4_.length == 1)
{
   _communication.connection.send(new _SafeCls_3173(_loc4_[0]));
}
```

The same user gesture that removes furniture — clicking a row in your own offer. Rows past
`ownUserItems.length` are Relics, and that branch sends `521` instead of `RemoveItemFromTrade` (573).

## CURRENT STATUS

MISSING, and **recorded in two places as impossible**:

`Vortex.Revisions/Revision20260701/Headers.cs:70`
> `RemoveNftFromTradeEvent` used to sit here on an invented id (9014) above the client's highest real
> header … the second has no composer anywhere in the 701 client, whose `TradingModel` can add NFTs
> to a trade but never remove them individually.

`Vortex.Rooms/Grains/Systems/RoomTradingSystem.cs`, on `AddTradeAssetsAsync`:
> There is no counterpart that removes one. The client has no such message — it re-derives which of
> its Relics are locked from the list sent back — so an offered Relic stays offered until the trade
> ends.

**Both are wrong.** The composer exists at `_composers[521] = _SafeCls_3173`, `(int)`, with a live
call site. The previous pass deleted a fabricated header id — correctly — and then wrote a negative
claim it had not established: "I could not find it" became "it does not exist".

## PROTOCOL EVIDENCE

| dir | id | class | wire |
|---|---|---|---|
| in | 521 | `_SafeCls_3173` | `int assetId` |

That the int is an **asset id** and not a row index is settled, not inferred.
`_SafeCls_1951.parseNftTradeMap` builds the client's Relic groups keyed by `productCode`, each
holding a `Vector.<Number>` of `assetId`; `pop(1)` returns one of those numbers. It is the same key
`AddNftToTrade` (2481) already uses, which is the only key both sides share.

## ACTUAL DOMAIN OWNER

`RoomTradingSystem` — the trade session lives on the room grain beside the furniture half of the same
offer, which is what lets a chair and a Relic change hands in one transaction.

## EXPECTED CLIENT-VISIBLE RESULT

Clicking an offered Relic takes it off the table: it leaves the list both parties see, and both
acceptances reset, exactly as removing a piece of furniture already does.

## OPEN UNKNOWNS / CONFLICTS

None. The wire is one int, the owner exists, and the furniture counterpart is already implemented and
tested — this mirrors it on the asset list.

## FILES I EXPECT TO TOUCH

```text
Vortex.Revisions/Revision20260701/Headers.cs                     # + the correction
Vortex.Revisions/Revision20260701/Maps/CollectiblesMap.cs
Vortex.Revisions/Revision20260701/Parsers/Collectibles/RemoveNftFromTradeMessageParser.cs
Vortex.Protocol/Messages/Incoming/Collectibles/RemoveNftFromTradeMessage.cs
Vortex.PacketHandlers/Collectibles/RemoveNftFromTradeMessageHandler.cs
Vortex.Primitives/Rooms/Grains/IRoomTrading.cs
Vortex.Rooms/Grains/RoomGrain.Trading.cs
Vortex.Rooms/Grains/Systems/RoomTradingSystem.cs                 # + the correction
Vortex.Rooms.Tests/Trading/**
```

## FILES I WILL NOT TOUCH

```text
Vortex.Rooms/Grains/Systems/RoomTradingSystem.cs  # the commit/settle path — no value movement here
```

## PERSISTENCE IMPACT

None. The offer is session state until the trade settles.

## VALUE / SECURITY IMPACT

This **removes** something from an offer, so it cannot give anything away — but it must reset both
acceptances, or one party could pull a Relic after the other has accepted. That is the whole risk of
the slice and it is the one thing the tests must pin. It is also why the phase guard matters:
removal is refused outside `Building`.

## TESTS REQUIRED

1. removing an offered asset takes it off that player's list;
2. removing resets both acceptances;
3. removing outside the `Building` phase is refused;
4. removing an id that is not on the table is a no-op, not an error;
5. one player cannot remove from the other's offer;
6. the parser reads a single int.

## ROLLBACK CONDITION

If a live client shows the wrong Relic leaving the table, the int is a row index and not an asset id
— revert the map entry and the feature is inert again.

## DONE WHEN

`completeness` no longer lists `header:521`, and both false comments are gone.

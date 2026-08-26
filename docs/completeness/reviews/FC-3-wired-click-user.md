# FC-3 — Wired click-user and the inspector's variable writes

**Target client:** `WIN63-202607011411-782849652`
**Measured from:** `docs/completeness/generated/domains/userdefinedroomevents.md`, `.../wired.md`

## OBLIGATION

| id | client class | status before |
|---|---|---|
| `incoming/header:1953` | `_SafeCls_2111` | MISSING |
| `incoming/header:689` | `_SafeCls_3855` | MISSING |
| `incoming/header:501` | `_SafeCls_3462` | MISSING — **deferred, see below** |

## CLIENT ENTRYPOINT

`1953` — `HabboUserDefinedRoomEvents.userSelected(id)`, reached from
`InfoStandWidgetHandler` when an avatar's info stand opens. Guarded by `hasClickUserWired()`, so the
client sends it **only** while the server has told it the room has a click-user wired box.

`689` — `WiredMenuInspectionTab.onCellEdit` / `onDeleteVariableClicked`.

## CURRENT STATUS

`wf_trg_click_user` exists as a room logic (`WiredTriggerClickUser`) and `PlayerClickedPlayerEvent`
is already published by `RoomGrain.ClickCharacterAsync` from `ClickCharacter` (3244). What is missing
is the whole client-facing half:

- nothing sends `WiredEnvironment` (2827), so `hasClickUserWired` is permanently false client-side —
  the context menu is never suppressed and `1953` is never sent;
- nothing receives `1953`, so the client's pending menu is never resolved;
- the trigger's own comment records `[blockMenuOpen, doNotRotate]` as "persisted here but not yet
  honoured server-side".

`WiredSetObjectVariableValue` is fully implemented and mapped at `625`. The client sends the *same
message* from a second surface on a different id, and only one of the two is bound.

## CURRENT VORTEX FLOW

```text
ClickCharacter(3244) -> RoomGrain.ClickCharacterAsync -> PlayerClickedPlayerEvent
                                                      -> WiredTriggerClickUser fires
(no reply, no environment push, menu always opens)
```

## ACTUAL DOMAIN OWNER

`RoomWiredSystem` owns the trigger registry (`WiredTriggerIndex`); `IRoomWired` is the facet a
handler may call. The click event itself stays owned by `IRoomAvatars` — this slice does not move it.

## EXPECTED CLIENT-VISIBLE RESULT

1. On room entry the client learns whether the room has a click-user wired box.
2. With one present, clicking an avatar sends `1953`; the server answers `309` with
   `(index, openMenu)` and the client opens or suppresses the context menu accordingly.
3. Editing or deleting a variable from the Wired menu's Inspection tab works, as it already does
   from the Variables Management view.

## PROTOCOL EVIDENCE

Read directly from the target client, not from a header comment.

| dir | id | client class / parser | wire |
|---|---|---|---|
| in | 1953 | `_SafeCls_2111` | `int` — the clicked avatar's room object id |
| in | 689 | `_SafeCls_3855` | `int, int, String, int, int` — identical to `625`'s `_SafeCls_2426` |
| out | 2827 | `_SafeCls_3319` / `_SafeCls_3496` | `bool hasClickUserWired`, then **optional** `int count` + `count × String` |
| out | 309 | `_SafeCls_3728` / `_SafeCls_3523` | `int index`, `bool openMenu` |

`index` is echoed back: `AvatarInfoWidget.onUserClickHandledEvent` passes it to `setupMenuView`,
which only fires when it equals the id the client is waiting on.

## OPEN UNKNOWNS / CONFLICTS

- **Does `1953` fire the trigger, or only ask about the menu?** With `hasClickUserWired` true the
  client sends `1953` *in addition to* the `3244` it already sends for the same click, and `3244`
  already drives the trigger here. This slice therefore treats `1953` as a **query**: it decides the
  menu and raises nothing. Firing on both would double every click-user trigger. Official behaviour
  is unknown and stays unknown; the choice is recorded in the handler.
- `enabledAchievements` is sent empty. No wired box in this repository enables an achievement, so
  there is nothing to name. Not a guess — an empty list is what this emulator actually knows.
- `doNotRotate` (int param 1) still is not honoured. Out of scope: it belongs to the look-at path
  (`_SafeCls_3064`), not to this reply.

## FILES I EXPECT TO TOUCH

```text
Vortex.Revisions/Revision20260701/Headers.cs
Vortex.Revisions/Revision20260701/Maps/UserDefinedRoomEventsMap.cs
Vortex.Revisions/Revision20260701/Parsers/UserDefinedRoomEvents/WiredClickUserMessageParser.cs
Vortex.Revisions/Revision20260701/Serializers/UserDefinedRoomEvents/**  (2 new)
Vortex.Protocol/Messages/Incoming/Userdefinedroomevents/WiredClickUserMessage.cs
Vortex.Protocol/Messages/Outgoing/Userdefinedroomevents/**  (2 new)
Vortex.PacketHandlers/UserDefinedRoomEvents/WiredClickUserMessageHandler.cs
Vortex.PacketHandlers/Room/Engine/GetRoomEntryDataMessageHandler.cs
Vortex.Primitives/Rooms/Grains/IRoomWired.ClickUser.cs
Vortex.Rooms/Grains/RoomGrain.Wired.ClickUser.cs
Vortex.Rooms/Grains/Systems/RoomWiredSystem.ClickUser.cs
Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/Triggers/WiredTriggerClickUser.cs
Vortex.Rooms.Tests/**
```

## FILES I WILL NOT TOUCH

```text
Vortex.Rooms/Grains/RoomGrain.Avatar.cs        # the click event keeps its current owner
Vortex.Rooms/Wired/Engine/IWiredRoomActions.cs  # not a capability a wired box asks for
Vortex.Rooms/Wired/Engine/WiredTriggerIndex.cs
```

## PERSISTENCE IMPACT

None. Every value read here is already persisted wired data.

## VALUE / SECURITY IMPACT

No currency, no items, no permissions. `689` reuses the `625` handler unchanged, so it inherits the
same room-rights checks — it adds a second door to a room that is already locked, not a new door.

## TESTS REQUIRED

1. no click-user box in the room → environment reports absent, reply opens the menu;
2. a box with `blockMenuOpen = true` → reply suppresses the menu;
3. a box with `blockMenuOpen = false` → reply opens the menu;
4. `689` and `625` parse to the same message with the same field order;
5. the environment serializer writes bool + count + names in the client's order.

## ROLLBACK CONDITION

If a live client shows the context menu breaking (never opening) with no wired box present, revert
the `WiredEnvironment` push from room entry — the client then falls back to its current behaviour
and the rest of the slice is inert.

## DONE WHEN

`completeness --domain userdefinedroomevents` no longer lists `header:1953` and `header:689`, both
having become entries with a handler and a flow, for the reason above and not because a stub was
added.

---

## DEFERRED — `501`, room state reload / rollback

`_SafeCls_3462(Boolean)` from `WiredMenuSettingsTab`: `true` from `onRollbackConfirmed`, `false` from
`onClickReload`. The client's own strings say what it costs:

```text
wiredmenu.settings.room_state.reload.warning
    Are you sure you want to reload the room? Everyone will re-enter the room
wiredmenu.settings.room_state.roll_back.warning
    Are you sure you want to roll this room back? Any furni movement or state change since the
    last room reload will be gone!
```

Rollback needs a snapshot of every furni position and state taken at room load, and a definition of
"the last room reload" that this emulator does not currently have — `ApplySnapshot` (2790) is mapped
to an empty handler and there is no snapshot store behind it. That is a feature with a persistence
design in it, not a message. It stays MISSING and visible.

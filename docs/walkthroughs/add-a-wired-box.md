# Walkthrough: Add a Wired box

A wired box is one of three things, and which one decides everything else:

| Kind | Base class | Answers | Called |
|---|---|---|---|
| **Trigger** | `FurnitureWiredTriggerLogic` | "did my event just happen, and is it mine?" | when a room event of a type it declares is drained |
| **Condition** | `FurnitureWiredConditionLogic` | "should this pile run?" | synchronously, before any effect |
| **Action** | `FurnitureWiredActionLogic` | "do the thing" | after the conditions pass, possibly after a delay |

There are 31 triggers, 43 conditions and 48 actions in the tree. Read two of the nearest kind before
writing yours — most of this document is the part that is not obvious from copying one.

> **Start at the specs, not at the code.** `dotnet run --project Vortex.Specs.Cli -- analyze <name>`.
> What Habbo's own client does with the box is the authority; what another emulator does is evidence.
> If the official behaviour is unknown, it stays explicitly unknown.

---

## Step 1 — Bind the class to the furniture

```csharp
[RoomObjectLogic("wf_act_bot_teleport")]
public class WiredActionBotTeleport(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
```

The string is what `furniture_definitions.logic` holds, and **that column is what binds — never the
classname.** A definition whose `logic` says `wf_act_bot_teleport` gets this class; a definition with
the right classname and the wrong `logic` gets the family default and silently does nothing.
`Vortex.furniture.logic.fallback` counts exactly that case, by name.

Registering a logic name twice is a hard error now, not a last-one-wins (ADR-000). If the build fails
on a collision, two classes claim the same box and one of them is a copy-paste.

## Step 2 — Say which box the client thinks it is

```csharp
public override int WiredCode => (int)WiredActionType.BOT_TELEPORT;
```

`WiredActionType` / `WiredConditionType` / `WiredTriggerType` mirror the client's own enum. Look the
value up in the client rather than adding one: a code the client does not know produces a dialog it
cannot draw, and that failure looks like the box being broken rather than unimplemented.

## Step 3 — Declare the parameter rules, even if they look redundant

This is the one that bites, and it fails **silently on the operator's screen**:

```csharp
public override List<IWiredParamRule> GetIntParamRules() =>
    [
        new WiredRangeParamRule(0, 119, 0),
        new WiredRangeParamRule(0, 99, 0),
        new WiredBoolParamRule(false),
    ];
```

`TryNormalizeIntParams` compares what the dialog sent against these rules, and with no tail rule it
requires the counts to match **exactly**:

```csharp
if (proposed.Count != fixedRules.Count)
{
    return false;
}
```

A box that declares no rules therefore has `fixedRules.Count == 0`, so **any** configuration carrying
an int parameter is refused. The dialog closes, nothing is saved, and nothing anywhere says why. If
your box takes numbers from its form, it needs one rule per number. If it takes a variable-length
list, give it `GetIntParamTailRule()` and the count check relaxes to a minimum.

Everything else the form can carry is on `IWiredData`: `StringParam` for the text field, `StuffIds`
for the selected furni, `VariableIds` for the variable pickers. The dialog reads the box's **stuff
data**, not a message of its own — so a box whose dialog opens empty is usually a stuff-data problem
and not a wired one.

## Step 4 — Implement the one method your kind owns

**Trigger** — declare the events, then decide whether this instance cares:

```csharp
public override List<Type> SupportedEventTypes { get; } = [typeof(BotReachedAvatarEvent)];

public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
{
    if (ctx.Event is not BotReachedAvatarEvent evt)
    {
        return Task.FromResult(false);
    }

    return Task.FromResult(MatchesConfiguredBot(evt.BotName));
}
```

`SupportedEventTypes` is not documentation — it is the index. `WiredTriggerIndex` uses it to decide
whether an incoming room event is worth queueing at all, so a type missing from that list is a
trigger that never fires and costs nothing to diagnose because nothing happened.

**Condition** — synchronous, and that is deliberate:

```csharp
public override bool Evaluate(IWiredProcessingContext ctx)
```

No `await`, no I/O. A condition runs inside the pile's resolution while the room is mid-turn; going
async there would let the room change between two conditions of the same stack.

**Action** — asynchronous, and may ask to wait:

```csharp
public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)

public override int GetDelayMs() => 500;   // optional
```

A non-zero delay does **not** mean "run in N ticks". The scheduler anchors on the room clock, and the
effect is re-validated at execution time: if the box has been dragged off the trigger's tile or picked
up during the wait, it does not fire, counts `WiredStopReason.REVALIDATION` and says so in the room's
wired log. That is Habbo's own rule — a trigger drives the boxes stacked with it — and the delay is
the one window in which it can stop being true after the pile was resolved.

## Step 5 — Declare what the box may select

```csharp
public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
    [
        [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
    ];
```

One entry per selection slot the form offers, each listing the sources that slot accepts. An action
that reaches for a selection it never declared gets an empty set, not an error.

## Step 6 — Test it on a room that does not exist

This is the part that changed, and it is why the engine was pulled apart.

`RoomWiredSystem` takes an `IWiredRoomHost`, so the whole pipeline runs on a fake room with a fake
clock and a seeded RNG. There are three levels and they buy different things:

**The box alone** — most of the 33 existing wired test files. Construct the logic with
`WiredTestBoxes.Context(objectId)` and call `Evaluate` or `ExecuteAsync` against a stub context.
Good for a condition's truth table.

**The box in a pile** — `FakeWiredRoomHost` plus the real engine:

```csharp
FakeWiredRoomHost room = new();
room.With(WiredTestBoxes.FloorItem(1, new MyTrigger(1)), tileIdx: 0);
room.With(WiredTestBoxes.FloorItem(2, new MyAction(2)), tileIdx: 0);

RoomWiredSystem engine = new(room);

await engine.OnRoomEventAsync(SomeEvent, CancellationToken.None);
await engine.ProcessWiredAsync(now: 1_000, CancellationToken.None);
```

This is where ordering, delays and re-validation are actually exercised — see
`WiredParityTests`. The clock is a `long` you pass in, so a delayed effect is tested by ticking to
1_500 rather than by sleeping.

**What the box did** — the fake host records rather than performs, so an effect's whole observable
behaviour is a list to assert on: `FloorItemMoves`, `AvatarRolls`, `BotCommands`, `RoomComposers`,
`HandItemsGiven`, `StopReasons`, `RoomLog`. An action that "works" but records nothing is an action
that did nothing.

`WiredEngineCostTests` covers the other axis: `room.Scans` counts the calls whose cost grows with the
room, so a box that walks every item per tick is a failing test rather than a slow hotel.

---

## The checklist

1. `Specs analyze` first; unknown official behaviour stays unknown.
2. `[RoomObjectLogic("...")]` matches the `logic` column of the definitions that should use it.
3. `WiredCode` is a value the client already knows.
4. One `IWiredParamRule` per int the form sends, or a tail rule — **or the config is silently refused**.
5. A trigger's `SupportedEventTypes` lists every event it can fire on.
6. A condition is synchronous. An action may delay, and must survive being re-validated.
7. A test on `FakeWiredRoomHost` that asserts on what the fake recorded.

## Anti-patterns this exists to prevent

- **Reaching for the room.** The box sees `_ctx` and the context it is handed. `RoomGrain` is not on
  the other side of that interface any more, and the tests are the reason.
- **A condition that awaits.** The room can change under it.
- **Inventing a `WiredCode`.** The client draws the dialog; a code it does not have is a box nobody
  can configure.
- **Assuming a delay means ticks.** It is the room clock, and the effect is re-checked at the end of it.

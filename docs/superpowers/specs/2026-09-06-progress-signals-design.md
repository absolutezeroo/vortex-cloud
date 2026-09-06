# Progress Signals — design

**Date**: 2026-09-06
**Status**: approved, stage ① in implementation
**Scope**: reward tracks, daily tasks, achievements. Wired gets the grammar, not the bus (§7.4).

One vocabulary of facts, one translation of each domain event, and three progression systems that
subscribe to it instead of each writing their own.

Every claim below about existing code was verified by reading it; paths are cited so it stays
re-checkable.

---

## 1. What exists today

### 1.1 Three translations of the same event

`Vortex.Primitives/Events` declares **137 domain events**. Three handler families translate them,
each with its own vocabulary:

| Family | File | Handlers | Lines | Vocabulary |
| --- | --- | --- | --- | --- |
| Reward tracks | `Vortex.RewardTracks/Events/RewardTrackEventHandlers.cs` | 22 | 778 | action + amount + `target` + **facts** |
| Daily tasks | `Vortex.Progression/Quests/Events/DailyTaskProgressEventHandlers.cs` | 7 | 132 | `QuestTypes.*` + amount |
| Achievements | `Vortex.Progression/Achievements/Events/AchievementProgressEventHandlers.cs` | 8 | 145 | `AchievementNames.*` + amount |

**Five events are translated three times** — `PlayerEnteredRoomEvent`, `PlayerFigureChangedEvent`,
`PlayerMottoChangedEvent`, `ItemPlacedEvent`, `RespectGivenEvent` — and two are translated twice
(`CatalogPurchasedEvent`: tracks + tasks; `FriendRequestAcceptedEvent`: tasks + achievements).
Nineteen handlers for seven events, 1,055 lines in all.

> **The measurable consequence**: enriching an event today only helps the system whose handler was
> edited. The other two keep ignoring the data that just arrived.

**115 of the 137 events feed no progression system at all.** The ceiling was never the filter
engine, it is the translation surface.

### 1.2 Two of the three families have no gate

`RewardTrackSignal.SendAsync` opens with `if (!catalog.IsActionInteresting(action)) return;`, and
`RewardTrackCatalog` says why: room entries, chat lines and furniture placements arrive constantly,
so an action no content mentions costs a hash lookup and stops there.

**Daily tasks and achievements have no such gate.** `DailyTaskRoomEntryHandler` and
`AchievementRoomEntryHandler` call their grain on **every** room entry, unconditionally. The calls
are `[OneWay]`, so the arrival does not wait — but each activates a per-player grain
(`PlayerDailyTaskGrain`, `PlayerAchievementGrain`) and loads its state whether or not today's
content mentions room entry.

> Today a room entry costs three handler activations, two unconditional grain calls and one hash
> lookup. This design can only remove from that (§4.4).

### 1.3 The fact map is hand-copied

`RewardTrackActionFacts` describes, action by action, which facts the matching handler emits. The
dashboard editor reads it to offer only filters that can match
(`DashboardApiService.RewardTracks.cs:49`), and the content validator reads it to refuse the rest
(`RewardTrackSequenceRules.cs:75,96`, `filter_fact_not_emitted_by_action`).

Its own comment claims `RewardTrackActionFactsTests` keeps the two lists in step. **That test does
not exist** — nothing in the repository references `RewardTrackActionFacts` outside those three
sites. The two lists had drifted on **nine actions**: the map advertised facts no handler emitted,
the editor offered them, and a filter written on one could never match.

| Action | Facts advertised, never emitted |
| --- | --- |
| `create_room` | `room` |
| `chat_with_someone` | `room` |
| `dance`, `wave` | `room` |
| `complete_trade` | `player` |
| `buy_from_catalogue` | `offer` |
| `use_habbicon` | `habbicon`, `room` |
| `complete_habbicon_collection` | `collection` |
| `pet_level` | `pet` |
| `wear_badge` | `badge` |

*(Fixed and committed, `0c36265d5`.)*

The root cause is not carelessness: **the translation is welded to an Orleans handler** that needs
an `IGrainFactory` and an `IRewardTrackCatalog` to be constructed. It cannot be driven from a unit
test without standing up half a silo — which is why the advertised test was never written.

### 1.4 What the handlers do that is not uniform

Taken from reading all 22 one by one. Each line is a constraint on the translator's shape (§4):

| Handler | Particularity |
| --- | --- |
| Catalogue purchase | **Two actions** per event (`buy_from_catalogue` then `spend_credits`, different amounts), and a **replay guard** (`CommerceReplayGuard`, key `"reward-track"`) *before* translating, with a third dependency (`ICommerceJournal`) |
| Trade completed | **Two players**: one signal per participant, the other as the `player` fact |
| Badges equipped | **N signals**: one per badge in `ImmutableArray<string> BadgeCodes` |
| Item moved | **Two handlers on one event**: `move_item` always, plus `rotate_item` when `RotatedInPlace`. Deliberate — a rotation *is* a move, and excluding it would make existing `move_item` tasks count less than the day they were written |
| Gesture | `dance` **or** `wave`, nothing for an unknown gesture |
| Chat | Nothing for a whisper ("a whisper to yourself would farm the task") |
| Pet level | The **amount is the level**, not 1 — for `Highest` mode. Credit goes to the owner, not whoever fed it |
| Habbicon used | `RoomId` is **0** in a private conversation, emitted as-is today |
| Walk on furni | **By far the most frequent signal in the hotel**: once per tile, for every avatar. Published `PublishDetached` from the room tick (`RoomAvatarTickSystem.cs:247`), whose comment relies explicitly on the gate on the other side |
| Quest completed, achievement level | Reward tracks **consume events produced by** quests and achievements — the two future consumers |

### 1.5 The replay guard is per consumer

`CommerceReplayGuard.FirstDeliveryAsync(journal, operationId, consumer, ct)`
(`Vortex.Primitives/Commerce/CommerceReplayGuard.cs`) records a `RELAY:<consumer>` step in the
commerce journal and answers `true` on first delivery only. Reward tracks pass `"reward-track"`,
daily tasks `"daily-task"`. The key carries **the consumer's name**, and that must be preserved:
without it, whichever of the three handles a republication first would rob the other two of their
first delivery.

What it does **not** do: guarantee retry after failure. The receipt is written *before* the business
call, `InvokeOneAsync` swallows the handler exception, and `CommerceRelayService` calls
`PublishAsync` then `MarkRelayedAsync` — the operation is marked relayed even if a consumer failed
inside it. It is a republication guard, not a processing acknowledgement (§7.3).

`RewardTrackCatalogPurchaseHandler` calls it **once**, then emits both signals. That cardinality is
what §3.1 preserves.

An empty or non-GUID `operationId` answers `true` without writing: the guard is inert for an event
that is not replayable — which is every event except the catalogue purchase.

### 1.6 What the pipeline already does — verified in `Vortex.Pipeline`

- **Discovery**: `EnvelopeFeatureProcessor` scans each assembly and registers every **public,
  concrete, non-generic** type implementing a closed `IEventHandler<X>`. A non-public type is
  skipped *with a warning* (`WarnNonPublic`).
- **Activation**: `ActivatorUtilities.CreateFactory` — **one instance per invocation**, resolved
  from the service provider and disposed after. A handler holds no state between events.
- **Plugins**: `PluginManager` runs the same `AssemblyProcessor` over plugin assemblies with the
  plugin's provider, and `InvokeOneAsync` wraps that provider in a
  `CompositeServiceProvider(plugin, host)` — a plugin handler **can** resolve a host service, but
  only inside the invocation. The registration batch is an `IDisposable`: unloading a plugin removes
  its handlers (`EnvelopeHost`, `RemoveAll`).
- **Dispatch**: `HandlerMode = Parallel`, `Task.WhenAll`; each handler is isolated
  (`OnHandlerInvokeError`), and a failing handler brings down neither the action nor its peers.
- **Nested publish**: `PublishAsync` has **no reentrancy guard**. A fresh context is built per
  publish (correlation read from the `AsyncLocal`). No handler in the repository publishes today —
  a new pattern, but nothing forbids it.
- **Nothing joins in quietly**: no catch-all `IEventHandler<IEvent>` exists (so inheritance dispatch
  would route the signal to nobody else), one `IEventBehavior` exists on a group event, and
  `OnNoHandlerRegistered` is unset.

> **So there is no infrastructure to build.** The signal is one more event.

---

## 2. What we build — and what we do not

**We build:**

1. A canonical signal contract, published as an ordinary `IEvent` (§3).
2. **Translators**: one class per domain event, a pure function with no dependencies, that
   **describes itself** (§4).
3. An `IAssemblyFeatureProcessor` of ~60 lines that discovers translators, registers them as
   handlers and feeds the vocabulary — the only component that sees plugins and their unload (§4.5).
4. A registry of **typed** facts, from which the editor derives its controls and operators (§5).
5. A shared filter evaluator and validator (§6).
6. **The interest gate**, taken over and widened — listed here because forgetting it would turn this
   design into a performance regression on the hotel's three hottest paths (§4.4).
7. The consumers: reward tracks first, then daily tasks and achievements (§7).

**We do not build:**

- **No new bus.** The existing event pipeline is enough (§1.6).
- **No rewrite of existing content.** `QuestTypes.*` and `AchievementNames.*` stay; each consumer
  keeps a code → action table. Existing fact keys and action codes **keep their exact strings**:
  they are in the database.
- **No runtime schema versioning.** The vocabulary is additive (§5.3).
- **Wired is not a bus consumer** (§7.4).
- **No enrichment that costs a read** (§4.3).
- **No side-by-side run** of old handler and new consumer, not even to compare (§11.2).

---

## 3. The contract

```csharp
namespace Vortex.Primitives.Signals;

/// <summary>What a player just did, said once for everyone.</summary>
public sealed record ProgressSignal(
    long PlayerId,
    string Action,          // SignalActions.*
    int Amount,             // 1 for an act; N for "spent N credits"; the level for pet_level
    string? Target,         // what the signal is mainly about (§3.3)
    ImmutableArray<SignalFact> Facts
);

public readonly record struct SignalFact(string Key, string Value);

/// <summary>
/// What one domain event produced: a batch, not a signal. A source event often produces several
/// (§4.1), and it is the batch that carries the delivery identity.
/// </summary>
public sealed record ProgressSignalsRaised(
    string DeliveryId,      // the source event's OperationId, or empty when it is not replayable
    ImmutableArray<ProgressSignal> Signals
) : IEvent;
```

### 3.1 Why a batch, and not one event per signal

`DeliveryId` sits on the envelope, not on the signal, and that is **the only shape that preserves
today's semantics**.

`RewardTrackCatalogPurchaseHandler` calls the replay guard **once**, then emits two signals
(`buy_from_catalogue` and `spend_credits`). A consumer guarding per *signal* would write the
`RELAY:reward-track` receipt on the first and **have the second rejected**: `TryRecordStepAsync`
inserts a `(OperationId, StepKey)` row under a unique constraint, so the second call with the same
key answers `false`. Credits spent would never count. One batch per source event restores exactly
today's behaviour: **one guard, once, for everything the event produced.**

Two side effects, both good: the overhead drops from **one envelope per action to one per translated
event** (the twenty badges of a `BadgesEquippedEvent` travel together), and a consumer sees at once
everything the act produced instead of reassembling it.

The translator says where the identity comes from, because only it knows which field carries it:

```csharp
public interface ISignalTranslator<TEvent> where TEvent : IEvent
{
    static abstract ImmutableArray<SignalShape> Shapes { get; }
    ImmutableArray<ProgressSignal> Translate(TEvent e);

    /// <summary>Empty by default: most events are not replayable.</summary>
    string DeliveryIdOf(TEvent e) => string.Empty;
}
```

Exactly one translator overrides it today, the catalogue purchase.

### 3.2 Values

**Values stay strings.** They already are (`RewardTrackFactSnapshot`), it is what the cross-step
capture serialises onto the player's row, and a typed `object` would force every consumer to know
each fact's type. The **type** lives in the registry (§5), not in the value: it serves the editor,
not the engine.

**A missing identifier is omitted, never emitted as `0`.** `HabbiconUsedEvent.RoomId` is 0 in a
private conversation and an absent category is 0, so the translator omits the fact. A filter on an
absent fact fails closed (§6), so "any room but 12" does not match a private conversation — which
`room = 0` would have broken.

**The rule stops at identifiers and optionals.** A `FactKind.Number` may legitimately be zero:
"received 0 respect in total" is a value, not an absence. The fact builder says so in its names —
`IdIfAny(...)` drops the zero, `Number(...)` writes it.

`SignalActions` re-declares the `RewardTrackActions` constants **with the same strings**: content
names them in the database.

### 3.3 `Target`: a field, and a fact inserted in one place for everyone

`Target` is already a first-class parameter of `IPlayerRewardTrackGrain.ProgressAsync`, where it does
**two things no fact does**:

1. it is what a task's `Parameter` is compared against — the pre-sequence mechanism, which keeps
   working untouched;
2. it is **the dedupe key for distinct-mode tasks**: "visit 20 different rooms" counts 20 distinct
   `Target` values.

But it is **also** a fact today: `RewardTrackSignal.SendAsync` does
`if (target is not null) facts = facts.Insert(0, new(RewardTrackFacts.Target, target));`, and
`RewardTrackActionFacts` lists `Target` for twelve actions. Content filters on it, and the editor
offers it as "Target".

> **That insertion is centralised, never copied into translators.** A translator sets `Target` and
> nothing else; the host (§4.5) adds the `target` fact before publishing, exactly where `SendAsync`
> does today. Leaving 21 translators to remember it is writing the next §1.3 drift — and the first
> version of this spec had already forgotten it in its own example.

Because the target's nature varies by action — a room id here, a player there, an offer for the
catalogue, a badge code for a badge — **`SignalShape` carries a `TargetKind`**:

```csharp
public sealed record SignalShape(
    string Action,
    ImmutableArray<FactKey> Facts,
    FactKind? TargetKind = null   // null = this action has no target
);
```

The vocabulary then exposes `target` as a **virtual fact typed per action**: a room picker on
`create_room`, a player picker on `give_respect`, an offer picker on `buy_from_catalogue`. Which
also answers why "Target" had no picker before: its type was written nowhere.

---

## 4. The translator

### 4.1 Shape

```csharp
public sealed class RoomCreatedTranslator : ISignalTranslator<RoomCreatedEvent>
{
    // One shape per action produced -- the catalogue purchase declares two.
    // TargetKind says what the target is; the "target" fact is added by the host (§3.3).
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(SignalActions.CreateRoom,
            [Facts.Room, Facts.RoomName, Facts.RoomDescription, Facts.Category, Facts.Model],
            TargetKind: FactKind.RoomId),
    ];

    public ImmutableArray<ProgressSignal> Translate(RoomCreatedEvent e) =>
        [new(e.OwnerId.Value, SignalActions.CreateRoom, 1,
            Target: Id(e.RoomId),
            Facts: SignalFacts.Build()
                .Id(Facts.Room, e.RoomId)
                .Text(Facts.RoomName, e.Name)
                .Text(Facts.RoomDescription, e.Description)
                .IdIfAny(Facts.Category, e.CategoryId)      // dropped when 0 (§3.2)
                .Text(Facts.Model, e.ModelName))];
}
```

**No constructor, no dependency.** A translator is a pure function and nothing else.
`ISignalTranslator<TEvent>` requires `Shapes` as a **static abstract** interface member (C# 11):
forgetting to declare it is a compile error, not a production discovery.

**`Translate` returns zero, one or many signals** — not one. That is not gratuitous generality; it
is what the current handlers already do (§1.4):

| Handler | What it emits |
| --- | --- |
| Catalogue purchase | **two actions**, `buy_from_catalogue` then `spend_credits` when `CreditCost > 0` |
| Trade completed | **two players**, each with the other as the `player` fact |
| Badges equipped | **one signal per badge** in the list |
| Item moved | `move_item` **always**, plus `rotate_item` when `RotatedInPlace` — one translator replaces both handlers |
| Gesture | `dance` **or** `wave`, and nothing for an unknown gesture |
| Chat | nothing for a whisper |

A single-return signature would have forced those six to stay hand-written — outside the generic
test, which is exactly the population the drift happened in.

The empty array replaces the `if (...) return;` guards at the top of today's handlers. Each `Shape`
declares **the union** of facts that action's signals may carry, and the §10.1 test requires every
declared key to be covered by at least one produced signal.

### 4.2 Two properties that justify everything else

1. **`Shapes` is the single source of truth.** The dashboard and the validator read it, not a
   parallel map. A fact offered in the editor but never emitted becomes impossible to write.
2. **`Translate` is a pure function.** A generic test walks every translator, feeds it a fabricated
   event and checks that each declared key actually comes out — **one test for 137 events** (§10.1).
   That is precisely what the current shape made impossible.

### 4.3 The richness rule

> A translator publishes only what the event **already carries**.

Enrichment happens **at the publish site**, where the data is in hand. `RoomCreatedEvent` is the
example: name, description, category and model cost three lines in `RoomService.Create` because the
entity was right there.

If a fact would need a database read or a grain call, **it does not enter the vocabulary**. That is
already the written reasoning for `room_owner`, absent from room entry because
`PlayerEnteredRoomEvent` does not carry it and reading it would be a grain call on an arrival path
that has been slow before. The rule becomes general, and a fact excluded for this reason is
**documented as excluded**, never declared.

Corollary: `RespectReceivedEvent` carries `RespectTotal`, `PetLeveledUpEvent` carries `RoomId` —
what is already there is free and gets declared; what is not gets plumbed at the source first.

### 4.4 The interest gate — the one thing not to lose on the way

Publishing unconditionally would remove the §1.2 gate and pay one envelope per action forever —
including once per tile walked by every avatar in the hotel. **The translator host (§4.5) takes it
over, widened, and evaluates it before anything else**: before `Translate`, before any allocation.

```csharp
public interface ISignalInterest { bool AnyConsumerCares(string action); }

/// <summary>One interest source. A DI singleton, NOT the consumer itself.</summary>
public interface ISignalInterestSource { ImmutableHashSet<string> Actions { get; } }
```

> **Interest is not carried by the handler.** `EnvelopeFeatureProcessor` does not turn handlers into
> services: it builds an activator and registers it in the registry, and the instance is created and
> dropped per invocation (§1.6). Having `RewardTrackSignalConsumer` implement `ISignalInterestSource`
> would leave **no instance** reachable by `ISignalInterest`.

Each system registers a **dedicated singleton** — `RewardTrackSignalInterest`,
`DailyTaskSignalInterest`, `AchievementSignalInterest` — in its own module, next to its other
services. `SignalInterest` takes `IEnumerable<ISignalInterestSource>` by injection and queries their
live sets, with no cached union: three hash lookups and no invalidation problem. A consumer and its
interest source read the same index; they are simply not the same object.

What each exposes: reward tracks already have the set (`_index.Actions`, swapped atomically by
`ReloadAsync` at the four write sites of `RewardTrackAdminService`) — the source re-reads it per
call, so a content reload is seen immediately. Achievements have a **static** set: the eight actions
of their table (§7.2). Daily tasks have **no content index today** (§1.2); stage ② gives them one,
built from the `quest_type_code` of published tasks, or failing that the static set of their table.

The gate closes on the **action**, and a translator may produce several: it is evaluated over the
union of its `Shapes`' actions. The fine sort — "this particular action interests nobody" — stays
with the consumer, where it already is (`IsActionInteresting` before the grain call, and `TasksFor`
inside the grain).

> Without this gate the design is a performance regression. With it, it is the opposite: two
> consumers that had none inherit one.

### 4.5 Discovery and hosting — a processor, no magic

A translator is not a handler; it has no constructor and no dependency. What wires it to the
pipeline is a `SignalTranslatorFeatureProcessor : IAssemblyFeatureProcessor`, registered beside
`EventFeatureProcessor` and run by the same `AssemblyProcessor` — so over the host **and every
plugin**, with the same `IDisposable` removal on unload.

For each public concrete type implementing `ISignalTranslator<TEvent>` it:

1. reads `Shapes` (static) by reflection, **validates** its keys (§8.1) and registers it in the
   vocabulary (§5);
2. instantiates the translator **once** (`Activator.CreateInstance`, parameterless constructor — a
   translator with constructor parameters is refused with a clear message, not skipped).
   **A non-public translator is refused the same way**, not merely warned about: `AssemblyExplorer`
   keeps public types only and settles for a warning (`WarnNonPublic`), which is fine for one
   handler among thirty but not for a translation whose absence makes an action entirely silent. A
   silo that refuses to start is a better diagnosis than an action that stops advancing;
3. builds `SignalTranslatorHost<TEvent>(translator, IEventPublisher, ISignalInterest)` **once** and
   registers it via `EnvelopeHost.RegisterHandler(typeof(TEvent), sp, _ => host, invoker)` — both
   APIs are public, and `EnvelopeInvokerFactory.CreateHandlerInvoker` resolves `HandleAsync` on the
   host type, which implements `IEventHandler<TEvent>`;
4. returns a disposable that removes **the handler and the shapes** together.

`SignalTranslatorHost<TEvent>.HandleAsync` does, in order:

1. **the interest gate** over the union of the `Shapes`' actions — before any allocation;
2. `Translate`;
3. for each signal, **inserting the `target` fact** when `Target` is non-null (§3.3);
4. `DeliveryIdOf`, then **one** publish of `ProgressSignalsRaised` carrying the batch.

It is the only non-trivial code in the project, and it is written once. Steps 1 and 3 are the two
rules an earlier draft left implicitly to 21 translators — that is, to their memory.

Two measurable consequences: **the host is a singleton**, so translators cost no per-event
activation, where every current handler is constructed and disposed per invocation (§1.6); and **the
plugin provider never comes into play** for a translator. The `CompositeServiceProvider(plugin, host)`
of §1.6 exists only inside `InvokeOneAsync`: what a processor resolves **at registration time** from
a plugin's provider does not see host services. A singleton built by the processor from its own
injected host services sidesteps the question — the same trap that once took the whole dashboard
down at startup, closed before it exists.

---

## 5. The fact registry

### 5.1 A fact is typed

```csharp
public sealed record FactKey(
    string Key,
    FactKind Kind,
    string LabelKey,                                  // dashboard locale key
    string FallbackLabel,                             // shown when the key is missing (plugins)
    ImmutableArray<EnumValue> EnumValues = default    // Kind == Enum only
);

/// <summary>An allowed value of a closed fact: what the engine compares, what the operator reads.</summary>
public readonly record struct EnumValue(string Value, string LabelKey, string FallbackLabel);

public enum FactKind
{
    Text,           // a name, a description, a motto
    Number,         // a count, a level, a total
    PlayerId,       // -> player picker
    RoomId,         // -> room picker
    FurnitureId,    // -> furniture picker + sprite
    CategoryId,     // -> navigator category list
    BadgeCode,      // -> badge picker
    OfferId,        // -> catalogue offer picker
    Enum,           // closed values, enumerated in EnumValues
    OpaqueId,       // a live id: a placed item, a pet. Free text, no directory.
}
```

Two fields without which the editor cannot be generic:

- **`EnumValues`.** §5.2 promises "a select of the declared values"; something has to declare them.
  `Facts.Placement` carries `[("floor", …), ("wall", …)]`, and the validator refuses a value outside
  the list — replacing the dashboard's hardcoded `PLACEMENTS` and its hand-added locale pair. A
  `Kind == Enum` with empty `EnumValues` is refused at load: it is a select with no options.
- **`FallbackLabel`.** A plugin cannot add keys to the dashboard locales (§8.1); without a fallback
  its facts would render as raw keys, `acme:trophy`. The core fills it too, which keeps the editor
  readable when a locale lags — something already seen here, where a key missing in `fr` silently
  falls back to English.

`Facts` declares the core keys with **their current strings** (`room`, `def`, `item`, `kind`,
`player`, `offer`, `habbicon`, `collection`, `pet`, `badge`, `name`, `desc`, `category`, `model`).

### 5.2 What the type decides

| `FactKind` | Editor control | Operators offered |
| --- | --- | --- |
| `Text` | text field | `Contains`, `Equals`, `NotEquals` |
| `Number` | number field | `Equals`, `NotEquals` |
| `PlayerId` / `RoomId` / `FurnitureId` / `OfferId` / `BadgeCode` | **picker** + label and sprite | `Equals`, `NotEquals`, `OneOf` |
| `CategoryId` | category list | `Equals`, `NotEquals`, `OneOf` |
| `Enum` | select of the declared values | `Equals`, `NotEquals` |
| `OpaqueId` | text field + `$N` reference | `Equals`, `NotEquals` |

This is exactly what is hardcoded today in `RewardTracksPage.svelte` (`PICKER_KINDS`, the floor/wall
select). Moving it into the contract means **daily tasks and achievements inherit the pickers
without writing a line** the day they gain filters.

### 5.3 Governance: additive only

Fact keys **and action codes** are stored as strings in content: `RewardTrackStepFilterEntity.FactKey`
for the former, the task action column for the latter, and the equivalent tables of future consumers.

> **Facts and actions are added, never renamed, never removed.** What no longer makes sense is marked
> `[Obsolete]`, disappears from the editor, and keeps being evaluated for the content already using
> it.

The rule covers **both** vocabularies: a renamed `create_room` breaks as much content as a renamed
`room`. Test §10.3 freezes both lists.

---

## 6. The operators

`Equals`, `NotEquals`, `OneOf` exist; `Contains = 3` is added and already committed:

```
Contains   // substring, case-insensitive, ordinal comparison
```

Without it `Text` is unusable: an exact match on a free-form room name is a filter that never fires.
**Ordinal ignore-case**, not culture-aware, so the same content matches the same room names on every
silo whatever locale the process runs in.

The §5.2 table is enforced **on both sides**: the editor offers only the allowed operators, the
validator refuses the rest. A `Contains` on a `RoomId` is a content error, not an exotic filter.

**Semantics unchanged, deliberately**: a filter on a fact the signal does not carry fails closed.
Including `NotEquals` — "any room but yours" is a claim about a room, and an action naming none has
not made it true. This is also the answer to the classic canonical-model trap: a consumer ignoring a
new fact does not start matching everything.

The evaluator is `TaskProgressRules.StepMatches`, moved as-is into Primitives: pure logic, no
dependencies.

---

## 7. The consumers

### 7.1 Shape and invariant

```csharp
public sealed class RewardTrackSignalConsumer(IGrainFactory grains, IRewardTrackCatalog catalog, ICommerceJournal journal)
    : IEventHandler<ProgressSignalsRaised>
{
    public async ValueTask HandleAsync(ProgressSignalsRaised e, EventContext ctx, CancellationToken ct)
    {
        // Filter first, and MATERIALISE: the replay guard must not be spent on a batch that does
        // not concern me, and must be spent exactly ONCE on a batch that does.
        ImmutableArray<ProgressSignal> mine =
        [
            .. e.Signals.Where(s => s.PlayerId > 0 && catalog.IsActionInteresting(s.Action)),
        ];

        if (mine.IsEmpty) return;

        if (!await CommerceReplayGuard.FirstDeliveryAsync(journal, e.DeliveryId, "reward-track", ct))
            return;

        foreach (ProgressSignal s in mine)
            await grains.GetPlayerRewardTrackGrain(s.PlayerId)
                .ProgressAsync(s.Action, s.Amount, s.Target, s.Facts.ToSnapshots(), ct);
    }
}
```

One consumer per system, discovered and isolated by the existing registry like any handler.

**Order matters**: filter, then guard, then process. Guarding before filtering would spend the
receipt on a batch none of whose signals concern this consumer, and a later republication — after an
operator publishes content that *does* care — would be rejected.

**So does materialising.** A lazy `IEnumerable` would be re-evaluated after the guard's `await`, and
`IsActionInteresting` reads an index that `ReloadAsync` swaps atomically on every content write: a
reload during the wait would make the guarded set diverge from the processed set. The receipt would
be spent for one batch and progress applied for another. Two brackets, and the window does not exist.

> **Invariant: a consumer makes exactly the same grain calls, with the same attributes and in the
> same number, as the handlers it replaces.** `IPlayerDailyTaskGrain.ProgressAsync` and
> `IPlayerAchievementGrain.ProgressAsync` are `[OneWay]` for a documented reentrancy reason ("the
> grain would deadlock behind its own event"); `IPlayerRewardTrackGrain.ProgressAsync` is not. None
> of that moves. The translator itself **calls no grain**.

**What the invariant does not cover: cross-grain parallelism.** The loop above serialises what two
handlers do concurrently today — `AchievementFriendCountHandler` calls both players via
`Task.WhenAll`, and a friendship batch carries two signals aimed at two different grains.
Serialising two `[OneWay]` calls is nearly free, but it is not *identical*, and a batch of twenty
badges would make it visible.

So the rule is explicit: **a consumer groups by grain and issues the groups concurrently** where the
handler it replaces did — `Task.WhenAll` across distinct grains, sequential within one grain (the
grain serialises anyway). A §10.1 matrix test checks the call count per grain; concurrency is read
in the consumer's ten lines.

### 7.2 Vocabulary mapping

Daily tasks and achievements **keep their codes**. Each holds an `action → its code` table, read
once. Exact inventory of what their 15 handlers do today:

**Daily tasks** (`DailyTaskSignalConsumer`, replay-guard key `"daily-task"`):

| Action | `QuestTypes.*` | Note |
| --- | --- | --- |
| `enter_other_users_room` | `RoomEntry` | |
| `change_figure` | `AvatarLooks` | |
| `change_motto` | `MottoChange` | |
| `give_respect` | `RespectGiven` | |
| `friend_added` | `FriendListSize` | **both players** — the translator emits two signals |
| `place_item` | `PlaceItem` | |
| `buy_from_catalogue` | `CatalogPurchase` | |

**Achievements** (`AchievementSignalConsumer`):

| Action | `AchievementNames.*` | Note |
| --- | --- | --- |
| `login` | `Login` | **`ProgressDailyAsync`**, at most once per calendar day — the table carries a `daily` flag |
| `enter_other_users_room` | `RoomEntry` | |
| `change_motto` | `Motto` | |
| `change_figure` | `AvatarLooks` | |
| `friend_added` | `FriendListSize` | both players, as above |
| `place_item` | `RoomDecoFurniCount` | |
| `give_respect` | `RespectGiven` | |
| `receive_respect` | `RespectEarned` | the signal's `PlayerId` is **the receiver** |

Three of those actions have **no translator today**: `friend_added`
(`FriendRequestAcceptedEvent`, two signals), `login` (`PlayerLoggedInEvent`) and `receive_respect`
(`RespectReceivedEvent`, with `RespectTotal` free as a `Number` fact). Stage ② writes them.

Out of scope: `IPlayerQuestGrain.ProgressAsync` is also called **straight from packet handlers**
(`ChatMessageHandler`, `DanceMessageHandler`, `AvatarExpressionMessageHandler`,
`FriendRequestQuestCompleteMessageHandler`) — a fourth path, outside events. It stays as is; moving
it onto signals is a possible stage ④, which would also settle a breach of "packet handlers
orchestrate only".

**Anti-loop rule**: reward tracks translate `QuestCompletedEvent` and `AchievementLevelUpEvent`,
produced by the two future consumers. No table may subscribe a consumer to an action produced by its
own events. A test checks this on the tables (§10.2).

### 7.3 What the replay guard guarantees — and what it does not

The guard stays **per consumer**, with today's key, and is called **once per batch** (§3.1). The
translator never dedupes — it is pure and has no journal.

> **The receipt is a republication guard, not a processing acknowledgement.** `FirstDeliveryAsync`
> writes the receipt **before** the business call, `EnvelopeHost.InvokeOneAsync` captures the
> handler's exception **without rethrowing**, and `CommerceRelayService` calls `PublishAsync` then
> `MarkRelayedAsync`. A consumer can therefore do receipt ✓ → `ProgressAsync` ✗ and never see it
> again.

What the per-consumer key really guarantees, and why it is indispensable: **one consumer's receipt
does not suppress another's first delivery.**

What it does not guarantee: recovery after failure. A real guarantee would need an idempotent inbox
in the consumer grain — writing the receipt and applying progress in one transaction. That is a
separate subject, it predates this rework, and stage ① **keeps it as is** rather than mixing a
migration with no observable risk into a change of delivery semantics.

### 7.4 Wired is not a consumer

Wired is evaluated **live inside the room grain**, over the room's live state, not over an event
stream. It takes **the grammar** (the fact registry, the operators, the evaluator, the editor
controls) and supplies its facts itself from grain state. Making it consume the bus would mean
routing every avatar step through the event pipeline: no.

The reverse direction already exists and does not change: the wired action `PROGRESS_REWARD_TRACK`
calls `IPlayerRewardTrackGrain.ProgressTaskAsync`, which names a track and a task and **deliberately
bypasses the action index**. Wired writes into reward tracks without a signal, because it does not
describe something a player did — it orders progress.

### 7.5 Order, concurrency, and where it runs

The registry dispatches in parallel (`Task.WhenAll`), so two signals for one player may be processed
out of order. That is not new: it is already true of today's three families. **The serialisation
point is the player's grain** — Orleans runs its calls one at a time, and that is where the
cross-step capture (`StepCaptures`) is read and rewritten.

A multi-step sequence is therefore safe in that two signals cannot clobber each other, but **the
order of two near-simultaneous actions is not guaranteed**. "Place a sofa then walk on it" in two
tenths of a second may fail to order. Current behaviour, preserved, and written down here so nobody
later discovers the design promised otherwise.

Where it runs: in the room and player grains alone, **62 sites await `PublishAsync`** in the calling
grain's turn, against **7 `PublishDetached`** in the whole repository — one of them the walk-on-furni
step. So the translator → nested publish → consumers chain runs **inside the publishing grain's
turn** for most events, exactly like today's handlers. Consumers make the same `[OneWay]` calls as
before (§7.1); the translator adds a pure function and one envelope.

---

## 8. Where the code lives

| Project | Contents | Depends on |
| --- | --- | --- |
| `Vortex.Primitives/Signals/` | `ProgressSignal`, `ProgressSignalsRaised`, `SignalActions`, `Facts`, `FactKind`, `SignalShape`, `FilterOperator`, the evaluator, the validation rules, **`ISignalVocabulary`**, `ISignalInterest`, `ISignalInterestSource`, `ISignalTranslator<T>` | nothing |
| **`Vortex.Signals`** *(new)* | the 21 (+3) translators, `SignalTranslatorHost<T>`, `SignalTranslatorFeatureProcessor`, `SignalVocabulary` | Primitives, Events, Pipeline, Runtime |
| `Vortex.RewardTracks` | its consumer; `RewardTrackSequenceRules` takes an `ISignalVocabulary` parameter instead of reading a static map | Primitives |
| `Vortex.Progression` | its two consumers + the two tables | Primitives |
| `Vortex.Dashboard.API` | serves `ISignalVocabulary.Shapes` to the editor | Primitives |

**No cycle, and nobody references `Vortex.Signals`**: consumers, validator and dashboard know only
the interface in Primitives, supplied by injection. The new project must be referenced by
`Vortex.Main` for the assembly scan to see it, and filed in the right solution folder (commit
`14226133b`).

### 8.1 A plugin can add a translator

`PluginManager` runs **the same `AssemblyProcessor`** as the host over plugin assemblies (§1.6). The
§4.5 processor therefore discovers a plugin's translators exactly like the core's, **with no core
change** — and removes their shapes and handlers on unload through the same disposable. Modularity
is not built here; it is inherited from the plugin loader.

Two rules follow:

1. **Namespacing — validated, never rewritten.** A plugin writes its keys **already qualified**
   (`acme:trophy`) in its `Shapes` and in whatever `Translate` returns; the processor **checks** the
   prefix at load against the `PluginManifest` key and refuses the assembly otherwise. It rewrites
   nothing.

   > Rewriting at load would have put `acme:trophy` in the vocabulary while the host published the
   > raw `trophy` from `Translate`, and no filter would ever have matched. Validating instead of
   > rewriting also lets a plugin **reuse core facts** (`room`, `player`) without them silently
   > becoming `acme:room` — so a plugin can say "in that room" in everyone's vocabulary.

   An unprefixed key that does not exist in the core is refused; a collision between two plugins is
   impossible by construction, since the prefix is the plugin key.
2. **"Never rename" (§5.3) applies to them too**, but the repository cannot lock it with a test. It
   is written into the plugin contract: a plugin renaming one of its keys breaks the content written
   on it, and owns that migration.

---

## 9. The dashboard becomes generic

`RewardTrackActionOptions` returns `{ name, wired, facts: string[] }` today, where `facts` comes
from the hand-kept map and `wired` from a hand-kept `HashSet`. It will return `ISignalVocabulary`
with **typed** facts:

```json
{ "name": "create_room", "hasProducer": true,
  "facts": [
    { "key": "room",     "kind": "RoomId",     "labelKey": "facts.room" },
    { "key": "name",     "kind": "Text",       "labelKey": "facts.roomName" },
    { "key": "category", "kind": "CategoryId", "labelKey": "facts.category" } ] }
```

The filter editor becomes its own component, driven by `kind`: it picks picker, select or text field,
and the operator list. The task and achievement editors reuse it unchanged the day they gain filters.

The "does this action have a producer?" flag **stays**, and becomes accurate instead of hand-kept:
the vocabulary knows which actions have a translator. It cannot go away: `SignalActions` remains a
**declared list**, not a projection of the translators. Of the 26 declared actions, 24 have a
producer and **two do not** (`teleport`, `wired`), and content may already name them. Deriving the
list from translators would make those two vanish from the editor without warning, and a task written
on one would become invisible instead of being flagged inert.

---

## 10. Tests

### 10.1 The missing test — in two halves, because one does not suffice

A generic fixture **cannot** prove every shape, and the spec carries its own counter-examples: chat
requires `Whisper == false`, gesture requires `Gesture == "dance"` or `"wave"` exactly, item-moved
requires `RotatedInPlace` in **both** states, purchase requires `CreditCost > 0` for its second
action. A generator that knows how to make "a non-empty string" will never guess `"dance"`. Claiming
otherwise is a green test that proves nothing — the very defect it is meant to prevent.

**Generic half — structural invariants, over every translator.** The fixture builds the event by
reflection over its positional constructor with non-null, non-empty values, and the test requires:

1. no signal carries a key **not declared** in `Shapes`;
2. every produced `Action` is declared;
3. no fact value is null or empty;
4. `PlayerId > 0` on every produced signal.

Those four hold even when `Translate` returns an empty array, which is the normal outcome for a
branching translator whose input was guessed wrong.

**Explicit half — a case matrix for the branching translators.** One named case per path, with the
event written by hand, requiring that **every declared key of the targeted shape is present and
non-empty**:

| Translator | Cases |
| --- | --- |
| Chat | normal line → signal; whisper → nothing |
| Gesture | `"dance"` → `dance`; `"wave"` → `wave`; `"cough"` → nothing |
| Item moved | move → `move_item` only; rotation → `move_item` **and** `rotate_item` |
| Catalogue purchase | `CreditCost > 0` → two signals; `CreditCost == 0` → one; `DeliveryIdOf` returns the `OperationId` |
| Badges | three codes → three signals; empty list → nothing |
| Trade | two signals, each with **the other** player as the `player` fact |
| Habbicon | in a room → `room` fact; in a private conversation (`RoomId == 0`) → **no** `room` fact |
| Friend accepted | two signals, one per player |
| Pet level | `Amount` == the level, not 1; credit goes to the owner |

**And the net that makes the matrix trustworthy**: a coverage test requires **every declared `Shape`
to be reached by at least one case**, explicit or generic. A branching translator added without its
case fails the suite instead of silently going untested. Without that net, the matrix is exactly the
kind of hand-kept list that produced the §1.3 drift.

No fixture generator exists in the repository (no AutoFixture, no Bogus): the builder is homemade,
~60 lines, covering exactly the types the 24 events use — `int`, `long`, `bool`, `string`,
`string?`, `PlayerId`, `PlayerId?`, `RoomId`, `ImmutableArray<string>`, `IReadOnlyList<int>`. An
unknown type fails the test with its name rather than passing silently.

### 10.2 Evaluator, tables, loop

The `TaskProgressRulesTests` cases stay valid; added are `Contains` (case, substring, empty string),
fail-closed on a missing fact for all four operators, and the allowed-operators table per `FactKind`.

Each mapping table (§7.2) gets a test: every named action exists in `SignalActions`, every code
exists in `QuestTypes` / `AchievementNames`, and **no action in the table is produced by an event of
the same system** (anti-loop rule).

### 10.3 Governance — both vocabularies

§5.3 freezes facts **and** actions, because both are stored as strings in content. The test freezes
both against two reference lists versioned in the repository:

| List | Fails if |
| --- | --- |
| `Facts` | a key disappears, changes `Kind`, or loses an `EnumValue` |
| `SignalActions` | a code disappears or changes string |

Adding a key, an action or an enum value updates the list; removing one requires wanting to, in the
same commit, under a reviewer's eyes. A removed `EnumValue` is treated as a removed key: content may
already compare against it.

### 10.4 The hot paths

`Vortex.Benchmark` and `Vortex.LoadGen` already measure arrivals. **Two paths to measure, not one**,
each in both states:

**Room entry** — the triple-translation path (§1.1):

- *no content listening*: the gate (§4.4) must make this at least as fast as today — and it should be
  faster, since two unconditional grain calls disappear (§1.2);
- *content listening*: three handlers activated yesterday; one singleton host, one envelope and three
  consumers tomorrow.

**`walk_on_furni`** — and this is the real test. By far the most frequent signal in the hotel (once
per tile, for every avatar in every room, published from the tick), it has **one** consumer, and
`RoomAvatarTickSystem.cs:247`'s comment relies explicitly on the gate opposite. It is where this
design adds the most and gains the least:

- *no content names `walk_on_furni`* — a normal hotel — : a hash lookup on the calling thread
  yesterday, a hash lookup tomorrow. **The delta must be indistinguishable from noise; if the
  measurement says otherwise, the gate is in the wrong place.**
- *content names it*: one envelope and one nested publish per tile walked. That is the worst case in
  the whole design, and its cost must be known before ①, not after.

Protocol: `LoadGen` with N avatars walking continuously in a furnished room, both content states,
before/after on tick time and per-tick allocation. Measure, never assume.

### 10.5 Observability

The hotel already has a Prometheus `/metrics` endpoint and an error-grouping sink. Signals add four
counters, because a progression system that stops advancing is a bug found through a player
complaint, never through an exception:

| Metric | What it answers |
| --- | --- |
| `signals_raised_total{action}` | which actions actually arrive, and which never do |
| `signal_translations_gated_total{translator}` | how many events the gate stopped before translating — the proof it earns its place |
| `signal_batches_published_total{translator}` | how many got through: the denominator of the above |
| `signal_consumer_failures_total{consumer}` | which consumer is failing, without reading logs |

> The gate runs **before** `Translate` (§4.4), so no signal and no action exists yet — and a
> translator may declare several. An `{action}` label on the gate would have had no value to write.
> The gate's unit is the event, its natural label the translator; the signal's unit stays the action.

The first counter is also the §13 coverage tool: a declared action whose counter stays at zero for a
week is either content nobody triggers or a translator that does not translate. The ratio of the next
two is the gate's health on `walk_on_furni`: on a hotel with no campaign it must read 100%.

### 10.6 Hosting and visibility

A hosting test starts the pipeline with `Vortex.Signals` loaded and checks the processor registered
at least one translator and one shape — because a project forgotten in `Vortex.Main` is an entirely
silent progression system, without a single exception.

And a visibility test that goes through no discovery at all: it sweeps the assembly by reflection,
**including non-public types**, and fails if it finds an `ISignalTranslator<>` the processor would
not have kept. Without it, §4.5's refusal protects nothing in the case that matters: a translator
written without `public` is never discovered, therefore never loaded, therefore never refused — and
the §10.1 generic test, which discovers through the same path, would not see it either. The hole is
the §1.3 hole: an absence raises nothing.

---

## 11. Delivery stages

Each stage ships alone and leaves the system working.

**① The contract, the translators, one consumer.**
`Vortex.Primitives/Signals`, `Vortex.Signals` with the 21 translators ported from
`RewardTrackEventHandlers` (one for `ItemMovedEvent` replacing two handlers), the processor, the
reward-track consumer, the evaluator and rules moved, `ISignalVocabulary` wired into the validator
and the dashboard, tests §10.1, §10.3, §10.6. `RewardTrackEventHandlers.cs` and
`RewardTrackActionFacts` are deleted. No observable behaviour change; the existing content tests
prove it.

**② Daily tasks and achievements.**
The three missing translators (§7.2), the two consumers, the two tables and their tests, an interest
index for daily tasks, and the deletion of their 15 handlers. The five thrice-translated events are
translated once.

**③ The grammar for wired and a wider vocabulary.**
The shared filter component in the dashboard, wired plugged into the fact registry with its own
sources, and widening coverage (§13).

### 11.1 What ② does NOT deliver

Stage ② gives daily tasks and achievements **no new capability**: no filters, no facts, no sequences.
It is pure de-duplication. Their content, tables and screens are unchanged and a player sees nothing
— except, for daily tasks, an interest gate they never had.

Deliberate: giving them filters at the same time would mix a migration with no observable risk into a
brand-new feature. Filters for tasks, if wanted, are a stage ④ — cheap, since the grammar and the
editor will be there.

### 11.2 Cutover and rollback

**The old handler and the new consumer must not run at the same time.** Both would call
`ProgressAsync` for the same act and every progression would count double — on persisted cumulative
counters, so unrecoverable except by hand in the database. A "double run to compare" is the obvious
trap of this migration; it is explicitly excluded.

Rollback is therefore **a commit revert**, not a configuration flag. What makes the revert safe:

- stage ① touches **no database schema** — same tables, same columns, same action and fact strings.
  The same grain calls, issued from somewhere else;
- it ships **in one go** (contract, translators, consumer, deletion of the old handlers) precisely so
  no intermediate state exists where both run;
- the post-cutover check is `signals_raised_total` (§10.5) against the expected actions: an action
  dropping to zero says immediately which translator is missing.

A configuration flag would be worse than the revert: it would keep both paths alive, and with them
the double-count risk, to save a `git revert`.

### 11.3 Rough size

Indicative, for planning — not a commitment.

| Stage | Files | The bulk of the work |
| --- | --- | --- |
| ① | ~40 (21 of them mechanical translators) | the §4.5 processor, porting the 21 translations, the §10.1 generic test |
| ② | ~15 | three translators, two tables, an interest index, deleting 15 handlers |
| ③ | ~15 dashboard + wired | the shared filter component, then §13 coverage over time |

The 21 translators in ① are line-by-line ports from a file read end to end (§1.4): that is the
volume, not the difficulty. The difficulty in ① is the processor, the interest gate and the generic
test — three things that do not exist yet and on which everything else depends.

---

## 12. Already committed

Done before this design and still valid — these are facts and fixes, not architecture:

- `26f35920f` — the block-based sequence editor, the premium tag, stage cards, pickers.
- `0c36265d5` — `RoomCreatedEvent` carries description, model and category; the `name`, `desc`,
  `category`, `model` facts; the nine §1.3 drifts fixed; `Contains`.

In stage ① those translations become translators; the work is moved, not thrown away.

---

## 13. Coverage: the remaining 115 events

Ordered by usefulness for content, not by ease:

| Family | Events | What it unlocks |
| --- | --- | --- |
| Pets | `PetLeveledUp` (declare `RoomId`, already carried), birth, care, training | "raise a pet", "hatch a plant" |
| Groups | creation, membership, forum | "found a group", "post on a forum" |
| Marketplace / trading | listing, sale completed | "sell three items" |
| Collectibles / mystery box | mint, opening, prizes | "open a box", "mint an item" |
| Habbicons | already translated | filters by collection |
| Fishing, games, wired | catch, score, game won | mini-game tasks |

For each: first check what the event **already carries**, then enrich at the publish site if the data
is free there, and **explicitly exclude** anything that would cost a read (§4.3).

---

## 14. Risks

| Risk | Mitigation |
| --- | --- |
| **The interest gate is forgotten** or evaluated after `Translate`: one envelope per event, once per tile walked | §4.4: first statement in the host, before any allocation. `walk_on_furni` benchmark in both states (§10.4), `signal_translations_gated_total` in production. |
| +1 envelope per **translated event** when content listens (§3.1: a batch, not one per action) | Against it: translation and enrichment go from three times to once, the host is a singleton where three handlers were instantiated, and two unconditional grain calls disappear. Measured before shipping ①. |
| A consumer spends its replay receipt on a batch that does not concern it | Filter before guarding, guard before processing — written in §7.1, covered by a §10.1 matrix case (catalogue purchase). |
| Old handler and new consumer run together: **every progression counts double**, on persisted counters | One commit, no flag, rollback by revert (§11.2). |
| The canonical model becomes a bottleneck forcing lock-step deploys | Additive vocabulary (§5.3); a consumer ignores what it does not know; a filter on an absent fact fails closed (§6). |
| A renamed key breaks content already in the database | Forbidden and locked by a test (§5.3, §10.3). |
| A redelivery skips a consumer | Per-consumer replay guard, `DeliveryId` on the batch (§7.3). |
| A consumer subscribes to an action produced by its own events | Anti-loop rule, tested on the tables (§7.2, §10.2). |
| A failing translator takes down three systems instead of one | `Translate` is pure with no I/O: it fails only on a code bug, the registry isolates the exception, §10.1 covers it. |
| Two plugins declare the same key | Mandatory plugin-key prefix, collision refused at load (§8.1). |
| The new project is not loaded: no signals at all, no exception | Hosting test (§10.6); `signals_raised_total` in production. |
| A multi-step sequence fails to order on two near-simultaneous actions | Current behaviour, preserved and written down (§7.5). |
| A non-public translator compiles and is never discovered | The processor refuses rather than warns (§4.5), **and** a reflection sweep including non-public types fails the build (§10.6). |

---

## 15. Not verified

No reference emulator sources are present on this machine (only `vortex-modern-client` sits beside
the repository). This design therefore rests on **no** third-party evidence about how another Habbo
emulator triggers its quests — only on this repository and on public integration patterns (Message
Translator, Canonical Data Model, Content Enricher, and event-taxonomy governance).

Also unmeasured: the real cost of the nested publish on room entry and on `walk_on_furni`. That is
what §10.4 exists to establish before ① ships.

---

## 16. Review history

Four review rounds against the code, each of which changed the design:

1. **Multi-signal translation.** `Translate` returns zero, one or many — the catalogue purchase emits
   two actions, a trade two players, badges N, and a rotation emits `move_item` **and** `rotate_item`.
   A single-return signature would have kept six translators hand-written and untested.
2. **`Target` is a field, and its fact is inserted centrally.** It drives `Parameter` matching and
   distinct-mode dedupe, and `SendAsync` adds it as a fact today — an early draft dropped it, which
   would have silently broken existing filters. `SignalShape.TargetKind` makes it a typed virtual
   fact, which finally gives it a picker.
3. **The interest gate**, and the fact that it cannot live on the consumer: handlers are not DI
   services. Three dedicated singletons.
4. **Batching.** `DeliveryId` per signal would have had the second signal of a purchase rejected by
   the receipt the first one wrote — `spend_credits` would never have counted. The envelope carries
   the batch and the guard runs once per source event, as today.

Plus the corrections found by re-reading: the replay guard is a republication guard and not a retry
guarantee (§7.3); a reflected fixture cannot prove branching translators (§10.1); `FactKey` needs
`FallbackLabel` and `EnumValues` to drive a generic editor (§5.1); actions must be frozen like facts
(§10.3); the gate's metric is per translator, not per action (§10.5); consumers preserve call
cardinality but must restore cross-grain concurrency explicitly (§7.1); filtered signals must be
materialised before the guard's `await` (§7.1); and a non-public translator needs a reflection sweep
to be caught at all (§10.6).

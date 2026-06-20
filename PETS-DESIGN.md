# Vortex Cloud — Pets Design

This implementation design for pets is correct where current retrospectives fail. It complements
`DATA-MODEL.md` (section 4) for behavior, persistence, and architecture. Read together with
`docs/walkthroughs/request-lifecycle.md` (grain/system model) and `ROADMAP.md`.

---

## Why current retrospectives fail at pets

Retros currently handle a pet as either **a moving piece of furniture** or **a fake avatar**:
an object that moves randomly and remains stuck. A correct pet is an **autonomous agent**:

1. it has **needs** that decrease over **real time**;
2. it **acts on its own** to satisfy them (eat, sleep);
3. it **learns** (levels, commands).

This autonomous loop — needs -> decision -> action -> learning — is rarely implemented correctly.
Everything else (animation, figure) is cosmetic.

---

## Architecture in the Orleans model

### Where the pet lives

- **Persistent identity + stats** -> `PetEntity` in DB (`DATA-MODEL.md` section 4).
- **Behavior** -> a `RoomPetSystem` **inside `RoomGrain`**, NOT a separate `PetGrain`.

> **Why not a PetGrain.** The pet needs room state continuously to navigate (map, furniture,
> other avatars). A separate grain would require a cross-grain call at each step -> unnecessary
> latency and complexity. The pet is fundamentally **room-scoped** while placed, so it is a
> `RoomObject` of `RoomGrain`, driven by a system — same pattern as
> `RoomChatSystem` / `RoomPathingSystem` / `RoomAvatarTickSystem`.

### Integration with existing systems

- **Movement** -> reuse existing **A\*** (`RoomPathingSystem`). The pet requests a path like an
  avatar; it navigates around furniture and does not teleport.
- **Tick** -> `RoomPetSystem` ticks with avatars (`RoomAvatarTickSystem`) or a dedicated tick.
  Each tick advances the state machine and need decay.
- **Broadcast** -> pet updates (position, gesture, status, level-up) go through
  `SendComposerToRoomAsync` -> stream -> presence (`request-lifecycle.md`).

---

## Heart of the system: need-driven state machine

```text
            Needs OK
  +-------------------------------+
  |              Idle             |
  +-------------------------------+
              | timer
              v
         [Wander] --(to random reachable tile)-->
              |
   nutrition < threshold                no food
              v
           [Hungry] --food found--> [Eat] --(nutrition+, XP+)-->
              |                             |
              | no food                     |
              +--> whine/idle               v
              |
              v
            [Sleep] --(energy regenerates)-->
              |
              |
       owner orders
              |
              v
          [Command] --skill known?--> gesture/anim + XP
                         no --------------> ignore
```

States:

- **Idle**: default; occasionally transitions to Wander.
- **Wander**: moves to a random reachable tile using A\*.
- **Hungry** (nutrition below threshold): finds the nearest `pet_food` matching
  its `pet_type`, goes there, eats. No food -> whine/idle.
- **Eat**: consumes food (decrement/remove item), `nutrition += pet_food.nutrition`,
  small XP gain.
- **Sleep** (low energy): pet bed/seat or stay in place; regenerates energy.
- **Command** (owner): interrupts; executes **only if the skill is learned** (based on level),
  performs gesture, gains XP, returns to Idle.

**Needs drive transitions.** Hunger rises and energy drops -> Hungry/Sleep automatically. This is the
autonomy retrospectives miss.

---

## Two engineering points that make it correct

### 1. Decay based on ELAPSED TIME, not tick count

If hunger is decremented **at every tick while the room is loaded**, a pet in an unloaded room never
gets hungry -> inconsistent. Correct model:

```
on pet load (or every update):
    elapsed = now - pet.updated_at
    pet.nutrition = clamp(pet.nutrition - hungerRate * elapsed)
    pet.energy    = clamp(pet.energy    - energyRate * elapsed)
```

The pet gets hungry according to **real time**, observed or not.

### 2. Persist stats in memory, flush THROTTLED — never per tick

Stats change on every tick (decay). Do not write them to DB every tick.

- Keep stats in room state memory.
- Flush to DB **periodically** (every N seconds, or on significant changes: level-up, feeding)
  **and** on room unload / pet pickup.

> This is the lesson from your wired bug (`RoomActive` vs `Persistent`): do not lose data,
> but do not hammer DB. Retros fail in two extremes — either stats are lost on reboot,
> or DB is written every frame.

---

## Leveling & commands (layered over the loop)

- **XP sources**: feeding, executing a command, receiving scratches (respect).
- **Level thresholds**: `pet_levels` config (`DATA-MODEL.md` section 4.2). Leveling increases
  stat caps and unlocks commands.
- **Commands**: `pet_commands` config (`pet_type`, `command`, `level_required`).
  A pet executes a command only if it has learned it (level reached).

---

## Details retros miss (and make it correct)

- **True pathfinding** around furniture via existing A\* — not fake movement.
- **Daily scratch/respect cap** (like player respect: N/day, not infinite).
- **Breeding inherits traits** (type/color from parents) with some randomness — not a random pet.
  Traceability via `parent_one_id` / `parent_two_id` (`DATA-MODEL.md` section 4).
- **Offline decay**: applied on reload (see point 1), not skipped.

---

## `RoomPetSystem` contract (what it exposes to `RoomGrain`)

- `LoadPetAsync(petId)` — loads `PetEntity`, applies elapsed-time decay, adds as RoomObject.
- `TickAsync()` — advances state machine and needs for each pet; marks dirty.
- `IssueCommandAsync(petId, command, actorPlayerId)` — validates learned skill + ownership,
  executes, grants XP.
- `FeedAsync(petId, foodFurniId)` — consumes food, increases nutrition, grants XP.
- `FlushDirtyAsync()` — persists dirty pet stats (throttled + on unload).

Same shape as other room systems: a flat in-process system holding a grain reference, delegating
persistence and broadcast to existing mechanisms.

---

## Suggested implementation order

1. `PetEntity` + place/pickup (pet exists and appears in room).
2. `RoomPetSystem` + Idle/Wander state machine (pet moves intelligently).
3. Needs + elapsed-time decay + Hungry/Eat with `pet_food` (autonomy).
4. Throttled persistence (the wired lesson).
5. Sleep, then levels (`pet_levels`), then commands (`pet_commands`).
6. Breeding and scratches last.

> Each step is independently playable and testable. Do not skip steps 3-4: this is what separates
> a real pet from a moving piece of furniture.

# Walkthrough: Add a Room Game

Freeze, Battle Banzai, football, hockey: a room game is a subsystem that a round starts and
stops, that gets a slice of the room tick, and that scores into teams. They all plug into the
same seam so that **nothing which starts or stops a round has to know which games exist**.

That seam was not free. Freeze was originally wired in by hand at every call site, and the
result was the bug this document exists to prevent: the game-timer furni's button started both
the room game and Freeze, but the wired `wf_act_control_clock` action started only the room
game. A wired clock ran the countdown and the arena furniture next to it never woke up. Nothing
in the build or the tests could see it.

> Follow the hard boundaries in `CONTEXT.md` and the contract in `AGENTS.md`. The placement
> rules here restate where those files say each kind of code belongs.

---

## The seam

`RoomGameSystem` is the room's **game coordinator**. It owns the two things every game shares:

- **Teams and scores** — one `GameTeamState` per room, which every game writes into and every
  wired team leaf reads out of (`wf_cnd_actor_in_team`, `wf_cnd_team_has_score`,
  `wf_cnd_team_has_rank`, `wf_slc_users_team`, `wf_trg_score_achieved`).
- **The round lifecycle** — `StartGameAsync` / `EndGameAsync`, which clear the scores, raise the
  wired `GAME_STARTS` / `GAME_ENDS` triggers, and fan out to the registered games.

A game derives from `RoomMinigameBase` (the no-op-virtuals base over `IRoomMinigame`) and is
registered once. Everything else already routes through the coordinator.

```
game-timer furni button ─┐
wf_act_control_clock ────┼─> RoomGameSystem.StartGameAsync / EndGameAsync ─> every IRoomMinigame
a game ending itself ────┘

room tick ───────────────> foreach GameSystem.Minigames ─> TickAsync
avatar leaves room ──────> RoomGameSystem.OnPlayerLeftAsync ─> every IRoomMinigame
```

---

## Checklist

| # | File | What to add |
|---|------|-------------|
| 1 | `Vortex.Rooms/Grains/Systems/Room<Game>System.cs` | the system, `: RoomMinigameBase`, with a lowercase constant `Name`; override only the hooks you use |
| 2 | `Vortex.Rooms/Grains/Systems/<Game>/Room<Game>Game.cs` | the pure rules POCO, no IO, holding `GameTeamState Teams { get; init; }` |
| 3 | `Vortex.Rooms/Grains/RoomGrain.cs` | the field, the construction next to the other systems, and **`GameSystem.Register(<Game>System);`** |
| 4 | `Vortex.Rooms/Object/Logic/Furniture/Floor/<Game>/` | one `[RoomObjectLogic("...")]` class per arena furni family — a colour family (gates, scoreboards, goals) is ONE class with one attribute per colour key and `GameColorKey.FromKeySuffix(ctx.Definition.LogicName)` |
| 5 | `Vortex.Primitives/Rooms/Object/IRoom<Game>Access.cs` + `RoomObjectContext.cs` + `RoomGrain.Capabilities.cs` | only the verbs the furni needs (walk-on, click, …) — **never Start/End** |
| 6 | `Vortex.Rooms.Tests/<Game>/` | rules tests on the POCO, plus the shared-team-state tests |

Step 3 is the one that matters. A game written but never registered builds clean, tests clean,
and never runs — `RoomMinigameCoordinationTests.Freeze_Is_Registered_On_Every_Room` is the
pattern for guarding against that; add the equivalent for the new game.

## The shared bricks

The point of the seam is that a game is rules + composition, not plumbing. The plumbing exists
once; use it:

- **`RoomGameChrome`** (`_roomGrain.GameChrome`) — every client-facing send a game needs:
  `BroadcastEffectAsync`, `BroadcastTeamAuraAsync` (the two aura sets are the `GameAuraSet`
  enum: Wired/Banzai = 33-36, Freeze = 40-43), `SetPlayingModeAsync`, `BroadcastPlayerValueAsync`
  (the number bubble), `LockMovement`/`UnlockMovement` (the one "frozen in place" primitive —
  the wired freeze-user boxes and a Freeze hit share it), `ResetGameTimers` (after an early round
  end). From a player-LEFT hook, only `SetPlayingModeAndForget` — awaiting back into the leaver's
  presence grain deadlocks the room's turn for 30 s.
- **`RoomItemIndex`** (`_roomGrain._state.ItemIndex`) — `ItemsOf<TLogic>()` / `LogicsOf<TLogic>()`
  instead of scanning `ItemsById`; `RoomMapModule.FirstLogicOnTile<TLogic>(tileIdx)` for "is my
  arena furni on this tile". `LogicsOf` returns a snapshot that is safe to await across.
- **`GameCadence`** — a slow in-game clock (Freeze's 1 s player tick) as a struct field:
  `if (_tick.Due(now)) { … }`, `Reset()` at round start. Keep the field non-readonly.
- **`IServerConfigGrain.GetManyAsync` + `ServerConfigValues`** — resolve the whole balance group
  in one grain round trip (see `FreezeConfig.ResolveAsync`); round start is a hot path.
- **`GameColorKey.FromKeySuffix`** — colour from `_red/_green/_blue/_yellow` logic keys or
  `_r/_g/_b/_y` classnames; unknown → `None`, never a throw (a throw in a logic constructor
  fails the item attach).

Candidates deliberately not yet converted to the index: the roller, pet-nest and wired item
scans. Convert them when touched.

---

## Rules that are not obvious

**Do not keep your own teams or scores.** Hold the room's `GameTeamState` and mirror
membership into it as players join. A second store is invisible to every wired team leaf, which
is what made Freeze's gates and scores unreachable from wired until they were merged.

**Score through `RoomGameSystem.AddTeamScoreAsync`**, not by mutating the state directly. That
is what fires the `SCORE_ACHIEVED` trigger; a direct write scores silently.

**Do not clear scores when your round starts.** The coordinator already did it, *before*
`GAME_STARTS` was published. Clearing them again in `StartAsync` lands after the event and wipes
whatever a `GAME_STARTS`-triggered give-score box just awarded — with no error and no log.

**End through the coordinator.** When your game decides the round is over (Freeze does, the
moment one team is left standing), call `RoomGameSystem.EndGameAsync`, never your own
`EndAsync`. Ending yourself skips `GAME_ENDS` and leaves the room's other games running. The
re-entrancy is safe: the coordinator flips its running flag before fanning out.

**Return early when idle.** `TickAsync` runs on every frame of every room, including the
overwhelming majority that contain none of your furniture.

**Your failures stay yours.** The coordinator catches and logs per game, so one game that
throws does not stop the others starting, ending or cleaning up. Do not rely on that to skip
your own error handling — it is a backstop, not a strategy.

---

## Validation

```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
```

Then confirm by hand what the gate cannot see: start a round from the game-timer furni's button
**and** from a wired `wf_act_control_clock` box, and check both reach the new game.

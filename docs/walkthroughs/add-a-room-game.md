# Walkthrough: Add a Room Game

Battle Banzai, Freeze, football, and whatever comes next: a room game is a set of rules that the
framework drives. It does not own its lifecycle, its teams, its scores, its arena index or any
packet — so adding one is **a new folder and an attribute**, and no file outside that folder
changes.

> Follow the hard boundaries in `CONTEXT.md` and the contract in `AGENTS.md`. The placement rules
> here restate where those files say each kind of code belongs.

---

## The shape

```
room tick ─────────────► RoomGameRuntime.TickAsync ─┐
game-timer furni button ┐                           │
wf_act_control_clock ───┼► StartGameAsync/EndGameAsync
a game ending itself ───┘                           │
                                                    ▼
                                        ┌──── GameHost (phase, match, arena, rng) ────┐
arena furni walked on ──► GameSignal ───►│  IRoomGame  ── rules only                  │
                                         │      │                                     │
                                         │      ├─► IRoomGameContext  ── the room     │
                                         │      ├─► GameTeamBook      ── teams/scores │
                                         │      └─► IGameArena        ── its furni    │
                                         └──────────────┬──────────────────────────────┘
                                                        ▼
                                             GameEvent ──► sinks
                                                            ├─ GameScoreboardPresenter → furni
                                                            ├─ RoomGameChrome          → composers
                                                            └─ GameDiagnosticsSink     → logs
```

Five things live in the framework and never in a game:

| Concern | Where |
|---|---|
| Lifecycle | `GamePhase` + `GameStateMachine` + `RoomGameRuntime`. A module has **no** `IsRunning` of its own. |
| Teams and scores | `TeamBook` over the game's own `TeamSet`, keyed by `TeamId`. Never a colour. |
| Habbo colours | `HabboTeamPalette`, the one mapping between a team and one of the four. |
| Arena lookup | `IGameArena`, a filtered view over the room's single item index. No game scans the room. |
| Client IO | `IGameChrome` (effects, game mode, the number bubble, movement locks, timer reset). |
| Which arena starts | `GameTargetResolver`. A start has one target or none — never a fan-out. |
| Match identity | `MatchId`, minted per ARENA per round, carried by everything a module defers. |

---

## Checklist

| # | File | What to add |
|---|------|-------------|
| 1 | `Vortex.Rooms/Games/<Game>/<Game>Game.cs` | the module: `[RoomGame]`, `: RoomGameModule`, a `GameProfile`, and only the hooks you use |
| 2 | `Vortex.Rooms/Games/<Game>/<Game>Constants.cs` | the `GameId` and the wire-fixed furni states |
| 3 | `Vortex.Rooms/Games/<Game>/<Game>Config.cs` + `<Game>Settings.cs` | balance, resolved in ONE round trip at prepare |
| 4 | `Vortex.Rooms/Games/<Game>/Components/*.cs` | one `[RoomObjectLogic]` class per arena furni family, `: GameFurnitureLogic` + a capability interface |
| 5 | `Vortex.Primitives/Server/ConfigKeyCatalog.cs` | the same keys, so an operator can edit them |
| 6 | `tools/catalog_converter/data/vortex_logics.json` | the new logic keys, so furniture can bind to them |
| 7 | `Vortex.Rooms.Tests/<Game>/` | rules tests on the pure parts, plus an integration test through the runtime |

**There is no step that edits the room grain, the timer furni, the wired actions or the runtime.**
That is the point. A game is found by the `[RoomGame]` attribute at startup and every room hosts it
from then on; `RoomGameRuntimeTests.EveryShippedGame_IsHostedByEveryRoom` is the guard.

---

## The module

```csharp
[RoomGame]
public sealed class TagGame(IRoomGameContext context) : RoomGameModule(context)
{
    public override GameProfile Profile { get; } =
        new() { Id = TagConstants.Game, Teams = TeamLayout.FourColours };

    public override ArenaValidation ValidateArena() =>
        ArenaValidation.Builder().Require("Tag tiles", _context.Arena.CountOf<IArenaTileComponent>()).Build();

    public override async Task OnPreparingAsync(GameMatch match, CancellationToken ct) { … }

    public override Task OnSignalAsync(GameSignal signal, CancellationToken ct) =>
        signal switch
        {
            { Kind: GameSignalKind.WalkOn, Component: IArenaTileComponent tile } => TagAsync(signal.Player, tile, ct),
            _ => Task.CompletedTask,
        };
}
```

A component is as small as it looks:

```csharp
[RoomObjectLogic("tag_tile")]
public sealed class TagTileComponent(IStuffDataFactory f, IRoomFloorItemContext ctx)
    : GameFurnitureLogic(f, ctx), IArenaTileComponent
{
    public override GameId Game => TagConstants.Game;
}
```

The base forwards walk-on, walk-off, use and detach into the runtime as `GameSignal`s, keeps the
furni's state unpersisted (a match's paint means nothing outside it), and deliberately does **not**
advance the state on a use the way ordinary furniture does — an arena furni's state belongs to the
game.

---

## Rules that are not obvious

**Teams are yours, colours are Habbo's.** Declare a `TeamSet` in your profile — any number of teams,
named by you. `_context.Teams` is keyed by those. A gate, goal or board reports the COLOUR it is
painted, because that is what the furni is; `_context.Palette.TeamOf(colour)` turns it into one of
your teams and `ColourOf(team)` turns one back for an aura. Those two calls are the only place the
two meet. A game whose teams the four colours cannot express still plays; it simply has nothing to
show on coloured furniture.

**Never capture the team book in a field initialiser.** An arena's book is bound *after* the module
is constructed, because which book it gets depends on the teams the module declares. Read
`_context.Teams` where you need it; a field initialiser captures nothing and `ArenaHost.Teams` will
throw to tell you so.

**A start may not reach you.** The framework resolves a start to ONE arena — the game the caller
named, else the arena the requesting furni belongs to, else the one it stands nearest to, else the
only candidate — and refuses an ambiguous room. Nothing about this is yours to influence beyond
declaring `ArenaSeparation` if your game genuinely supports several playfields in a room.

**Never keep your own phase.** `IsLive` and `HasMatch` on `RoomGameModule` read the runtime's. A
second flag is how the old system ended up with four booleans in four files, none of which agreed.

**Never end yourself.** Call `_context.RequestMatchEndAsync`. It ends YOUR arena's match and, if it
was the last one running, the room's round with it — which is what fires `GAME_ENDS` and resets the
game-timer furni. Calling your own end hook skips both.

**Never write a score directly.** `_context.ScoreAsync(new GameScore(team, player, amount, reason,
source))`. That is what fires `SCORE_ACHIEVED`, repaints the boards and puts the act in the trace. A
direct write to the team book scores silently — and the runtime refuses a score outside a live match
for you, so "a finished game cannot accept score changes" is not something you have to remember.

**Never clear the scores at kick-off.** The runtime already did, *before* `GAME_STARTS` was
published. Clearing them again lands after the event and wipes whatever a `GAME_STARTS`-triggered
give-score box just awarded, with no error and no log.

**Stamp deferred work with the match.** Anything you queue — a projectile, a delayed teleport, a
rolling ball — carries `_context.Match` and is dropped when it no longer matches. That is what makes
"events from match N cannot mutate match N+1" true rather than hoped for.

**Ask for idle ticks, do not assume them.** A game with no match is not ticked at all. If you have
work in flight outside a match (a football rolling in a room with no goals), call
`_context.KeepTicking()` for as long as it lasts.

**Roll through `_context.Random`.** It is seeded from the match id, so a match replays identically
and a test can assert on a power-up or a teleport destination. `Random.Shared` cannot be stubbed.

**Your failures stay yours.** The runtime catches and logs per game, per step, so one game that
throws does not stop the others starting, ending or cleaning up — and a game that throws while
preparing does not go on to play a match on an arena it failed to set up. It is a backstop, not a
strategy.

---

## The shared bricks

- **`IGameChrome`** (`_context.Chrome`) — every client-facing send a game needs. From a
  participant-LEFT hook, only `SetPlayingModeAndForget`: awaiting back into the leaver's own
  presence grain deadlocks the room's turn for 30 s.
- **`IGameArena`** (`_context.Arena`) — `ComponentsOf<T>()`, `CountOf<T>()`, `OnTile<T>(idx)`,
  `TilesOf<T>()`, all filtered to your game. A filtered view over the room's one item index, so it
  is correct through placement, pickup and a definition swap with no subscription.
- **`TeamGateRules.Toggle`** — the gate rules, once, over a `TeamSet` and a `TeamId`. The capacity
  check runs before the player leaves their current team, so a rejected switch never strips the
  membership they had, and capacity is per team so an asymmetric game can have one seeker and five
  hiders.
- **`GameCadence`** — a slow in-game clock as a struct field: `if (_tick.Due(now)) { … }`,
  `Reset()` at prepare. Keep the field non-readonly; it is a mutable struct.
- **`GameColorKey.FromKeySuffix`** — colour from `_red/_green/_blue/_yellow` logic keys or
  `_r/_g/_b/_y` classnames; unknown → `None`, never a throw (a throw in a logic constructor fails
  the item attach and takes the furni out of the room).
- **`IServerConfigGrain` via `_context.GetConfigAsync`** — the whole balance group in one round
  trip, at prepare.

---

## Concurrency

Everything in the games tree runs inside the room grain's single-threaded turn. There is no locking
and there must not be: Orleans already serialises the room, and a lock inside an actor can only
deadlock. What the turn does **not** give you is atomicity across an `await` — it can interleave at
every one — so a loop that awaits must iterate a snapshot. `IGameArena`'s queries materialise for
exactly this reason.

---

## Validation

```bash
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck
dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate
```

Then confirm by hand what the gate cannot see: start a round from the game-timer furni's button
**and** from a wired `wf_act_control_clock` box, and check both reach the new game.

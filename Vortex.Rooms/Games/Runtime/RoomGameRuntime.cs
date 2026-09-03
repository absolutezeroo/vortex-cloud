using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>One registered game: its shape, how to build another instance of it, and the partition of
/// its furniture into arenas.</summary>
internal sealed class GameRegistration
{
    public required GameProfile Profile { get; init; }

    public required Func<IRoomGameContext, IRoomGame> Factory { get; init; }

    /// <summary>The furniture partition this game's arenas were cut from. Recomputed at most once a
    /// tick, and only for a game that actually separates — a game with
    /// <c>ArenaSeparation &lt;= 0</c> is one arena per room and never partitions anything.</summary>
    public ArenaPartition Partition { get; set; } = ArenaPartition.Single;

    public long PartitionedAtMs { get; set; } = long.MinValue;

    public bool Separates => Profile.ArenaSeparation > 0;
}

/// <summary>
/// The room's game runtime: the one coordinator, and deliberately not a god manager. It knows the
/// lifecycle, the team ledgers, the event fan-out and how to route a component signal — and nothing
/// at all about Battle Banzai, Freeze or football. Every rule lives in a module; this file would not
/// change if the room hosted twenty games.
/// <para><b>Arenas, not games.</b> The unit of everything here is the ARENA — one installation of one
/// game — because a room may hold several, of the same game or of different ones. A start has a
/// target arena or it does nothing: the old "start every game whose arena validates" answered one
/// press of one counter by kicking off every unrelated match in the hall.</para>
/// <para><b>Concurrency.</b> Everything here runs inside the room grain's single-threaded turn, so
/// there is no locking and none is wanted: Orleans already serialises the room, and a lock inside an
/// actor can only deadlock. The property that needs care is not exclusion but atomicity across an
/// <c>await</c>: the turn can interleave at every one, so every fan-out iterates a snapshot and every
/// deferred piece of work re-checks the match it belongs to.</para>
/// <para><b>Match isolation.</b> A match id is minted per ARENA per round and carried by everything a
/// module defers. A callback from a finished match finds the phase changed and drops itself, which is
/// what makes stale state from one board unable to touch the next match — or the board next to it.</para>
/// <para><b>State is ephemeral.</b> Built fresh when the room grain activates, so a match dies with
/// the room — matching Habbo, and making a zombie match impossible by construction.</para>
/// </summary>
public sealed class RoomGameRuntime
{
    private readonly RoomGrain _roomGrain;
    private readonly List<GameRegistration> _games = [];
    private readonly List<ArenaHost> _hosts = [];
    private readonly List<IGameEventSink> _sinks = [];

    /// <summary>
    /// The room's Habbo-facing team ledger: four teams, the four colours, one score each. It is a
    /// ROOM concept and not a game one, because that is what Habbo makes it — <c>wf_act_join_team</c>
    /// puts you on the room's red team, one <c>bb_score_r</c> shows one red score, and a wired
    /// give-score box works in a room with no game furniture at all. Every arena whose game plays
    /// with the room's team space shares it; a game that defines its own teams keeps its own book,
    /// which the coloured furniture cannot address anyway.
    /// </summary>
    private readonly TeamBook _roomTeams = new(TeamSet.HabboColours);

    private long _nowMs;

    /// <summary>Whether the room's round has been announced (GAME_STARTS fired, GAME_ENDS not yet).
    /// One flag for the room, because the wired triggers are room-level: a room does not fire two
    /// GAME_STARTS because a second arena kicked off, and does not fire GAME_ENDS until the last one
    /// has stopped.</summary>
    private bool _roundAnnounced;

    public RoomGameRuntime(RoomGrain roomGrain)
    {
        _roomGrain = roomGrain;
        Chrome = new RoomGameChrome(roomGrain);
    }

    /// <summary>The room's client-facing game plumbing.</summary>
    public IGameChrome Chrome { get; }

    /// <summary>The room's Habbo-facing team + score ledger, read by every wired team leaf.</summary>
    internal TeamBook RoomTeams => _roomTeams;

    /// <summary>The colour mapping for the room's ledger.</summary>
    internal HabboTeamPalette RoomPalette => HabboTeamPalette.Standard;

    /// <summary>The timestamp of the room tick being processed, for modules that need "now" outside
    /// their own tick (a signal handler queueing deferred work).</summary>
    internal long NowMs => _nowMs;

    /// <summary>The game modules currently hosted, one per arena, in registration order.</summary>
    public IReadOnlyList<IRoomGame> Games
    {
        get
        {
            List<IRoomGame> games = [];

            foreach (ArenaHost host in _hosts)
            {
                games.Add(host.Game);
            }

            return games;
        }
    }

    /// <summary>The arenas the room currently holds, in a stable order.</summary>
    public IReadOnlyList<ArenaId> Arenas
    {
        get
        {
            List<ArenaId> arenas = [];

            foreach (ArenaHost host in _hosts)
            {
                arenas.Add(host.Id);
            }

            return arenas;
        }
    }

    /// <summary>Whether any arena in the room currently has a live match. What the wired engine and
    /// the timer furni mean by "the room's game is running".</summary>
    public bool IsRunning
    {
        get
        {
            foreach (ArenaHost host in _hosts)
            {
                if (host.IsLive)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // ---- composition -------------------------------------------------------

    /// <summary>
    /// Plugs a game into the room, creating its first arena. The factory receives the context that
    /// arena's module will use for its whole life, so a module captures it in a readonly field
    /// instead of being handed the room later, and is called again for each further arena the
    /// game's furniture turns out to form. Called once per game while the room grain is being
    /// constructed.
    /// </summary>
    public void Register(Func<IRoomGameContext, IRoomGame> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        ArenaHost host = BuildHost(factory, instance: 0);

        _games.Add(new GameRegistration { Profile = host.Game.Profile, Factory = factory });
        _hosts.Add(host);
    }

    private ArenaHost BuildHost(Func<IRoomGameContext, IRoomGame> factory, int instance)
    {
        ArenaHost host = new();
        RoomGameArena view = new(host, _roomGrain);

        host.View = view;
        host.Context = new RoomGameContext(_roomGrain, this, host, view);
        host.Game = factory(host.Context);

        GameProfile profile = host.Game.Profile;

        host.Id = new ArenaId(profile.Id, instance);
        host.Palette = HabboTeamPalette.For(profile.Teams);

        // The room's ledger is shared only by a game that plays with the room's team space. Anything
        // else — five teams, teams with no colour, the four colours in a different order — gets its
        // own book: sharing would mean two games writing different meanings into one red score.
        host.SharesRoomTeams = profile.Teams.HasSameTeamsAs(TeamSet.HabboColours);
        host.Teams = host.SharesRoomTeams ? _roomTeams : new TeamBook(profile.Teams);

        return host;
    }

    /// <summary>Attaches an event sink (a scoreboard painter, the diagnostics tracer). Sinks see
    /// every arena's events.</summary>
    public void AddSink(IGameEventSink sink) => _sinks.Add(sink);

    // ---- arena discovery ---------------------------------------------------

    /// <summary>
    /// Brings the host list in line with the room's furniture: a game that separates its installations
    /// is re-partitioned (at most once a tick) and gains a host for each installation found. A game
    /// that does not separate — every Habbo game, because the client cannot address a second board —
    /// costs nothing here at all.
    /// <para>
    /// Hosts are only ever added, never removed while the room lives: an arena whose furniture was
    /// picked up becomes an empty arena that fails validation and refuses to start, which is a far
    /// safer thing than a host disappearing from under a live match.
    /// </para>
    /// </summary>
    private void RefreshArenas()
    {
        foreach (GameRegistration game in _games)
        {
            if (!game.Separates || game.PartitionedAtMs == _nowMs)
            {
                continue;
            }

            game.PartitionedAtMs = _nowMs;
            game.Partition = ArenaPartition.Build(
                PlacementsOf(game.Profile.Id),
                game.Profile.ArenaSeparation
            );

            int known = CountHostsOf(game.Profile.Id);

            for (int instance = known; instance < game.Partition.InstanceCount; instance++)
            {
                _hosts.Add(BuildHost(game.Factory, instance));
            }
        }
    }

    private List<ArenaPlacement> PlacementsOf(GameId game)
    {
        List<ArenaPlacement> placements = [];

        foreach (IGameComponent component in _roomGrain._state.ItemIndex.LogicsOf<IGameComponent>())
        {
            if (component.Game == game)
            {
                placements.Add(new ArenaPlacement(component.ObjectId, component.X, component.Y));
            }
        }

        return placements;
    }

    private int CountHostsOf(GameId game)
    {
        int count = 0;

        foreach (ArenaHost host in _hosts)
        {
            if (host.Id.Game == game)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The partition a given arena's furniture belongs to, for the arena view's filter.</summary>
    internal ArenaPartition PartitionOf(GameId game)
    {
        foreach (GameRegistration registration in _games)
        {
            if (registration.Profile.Id == game)
            {
                return registration.Partition;
            }
        }

        return ArenaPartition.Single;
    }

    // ---- target resolution -------------------------------------------------

    /// <summary>
    /// Which arena a start applies to. Candidates are the arenas that are idle AND whose furniture
    /// validates, so "start" can never pick something unplayable, and the resolver picks between them
    /// by the request's own origin. An ambiguous room resolves to nothing and says so.
    /// </summary>
    public GameTarget ResolveStartTarget(RoomObjectId source, GameId game)
    {
        RefreshArenas();

        List<ArenaCandidate> candidates = [];

        foreach (ArenaHost host in _hosts)
        {
            if (host.Phase != GamePhase.Idle && !IsRestartable(host.Phase))
            {
                continue;
            }

            if (!SafeValidate(host).CanStart)
            {
                continue;
            }

            candidates.Add(CandidateFor(host, source));
        }

        return GameTargetResolver.Resolve(candidates, game, source);
    }

    /// <summary>Which arena a stop applies to. Candidates are the arenas actually holding a match:
    /// stopping resolves against what is running, so a counter beside a finished board does not reach
    /// across the room and end the match that is still going.</summary>
    public GameTarget ResolveStopTarget(RoomObjectId source, GameId game)
    {
        List<ArenaCandidate> candidates = [];

        foreach (ArenaHost host in _hosts)
        {
            if (GameStateMachine.HasMatch(host.Phase))
            {
                candidates.Add(CandidateFor(host, source));
            }
        }

        return GameTargetResolver.Resolve(candidates, game, source);
    }

    /// <summary>A phase a fresh start supersedes: a new round replaces the previous one's showcase
    /// rather than being ignored for the seconds a celebration lasts.</summary>
    private static bool IsRestartable(GamePhase phase) =>
        phase is GamePhase.RoundEnding or GamePhase.Finished or GamePhase.Resetting;

    private ArenaCandidate CandidateFor(ArenaHost host, RoomObjectId source)
    {
        List<RoomObjectId> components = [];
        int distance = int.MaxValue;

        bool haveSource =
            source.Value != 0
            && _roomGrain._state.ItemsById.TryGetValue(source, out IRoomItem? sourceItem)
            && sourceItem is not null;
        int sourceX = 0;
        int sourceY = 0;

        if (haveSource)
        {
            sourceX = _roomGrain._state.ItemsById[source].X;
            sourceY = _roomGrain._state.ItemsById[source].Y;
        }

        foreach (IGameComponent component in host.View.ComponentsOf<IGameComponent>())
        {
            components.Add(component.ObjectId);

            if (!haveSource)
            {
                continue;
            }

            int step = Math.Max(Math.Abs(component.X - sourceX), Math.Abs(component.Y - sourceY));

            if (step < distance)
            {
                distance = step;
            }
        }

        return new ArenaCandidate(host.Id, components, distance);
    }

    // ---- lifecycle ---------------------------------------------------------

    /// <summary>
    /// Starts a match on the ONE arena this request resolves to. Returns false when the room offered
    /// nothing to start or offered several and nothing chose between them — in both cases nothing
    /// happens, which is the point: a single press of a counter in a hall with three arenas used to
    /// start three matches.
    /// <para>
    /// The order is load-bearing and has been wrong before. The target's scores are zeroed FIRST,
    /// then the wired GAME_STARTS trigger fires, and only then does the game prepare: a GAME_STARTS
    /// box wired to a give-score action runs off that event, and a reset arriving afterwards would
    /// wipe the points it just awarded with no error and no log.
    /// </para>
    /// </summary>
    public async Task<bool> StartGameAsync(RoomObjectId source, GameId game, CancellationToken ct)
    {
        GameTarget target = ResolveStartTarget(source, game);

        if (!target.IsResolved)
        {
            LogUnresolved("start", target, source, game);

            return false;
        }

        ArenaHost? host = FindHost(target.Arena);

        if (host is null)
        {
            return false;
        }

        if (IsRestartable(host.Phase))
        {
            await TransitionAsync(host, GamePhase.Resetting, ct);
            await TransitionAsync(host, GamePhase.Idle, ct);
        }

        if (host.Phase != GamePhase.Idle)
        {
            return false;
        }

        // Teams survive: they are picked at the gates before kick-off, so wiping membership here
        // would empty the arena. Scores do not — but only when no OTHER live arena is keeping score
        // in the same book, because zeroing a shared ledger under a running match would wipe it.
        if (!IsBookBusy(host))
        {
            host.Teams.ResetScores();
        }

        await AnnounceRoundStartAsync(ct);

        await StartMatchAsync(host, ct);

        return true;
    }

    /// <summary>Ends the match on the ONE arena this request resolves to. The final scores are left
    /// standing so a GAME_ENDS box can read the winner through the team rank/score conditions.</summary>
    public async Task<bool> EndGameAsync(RoomObjectId source, GameId game, CancellationToken ct)
    {
        GameTarget target = ResolveStopTarget(source, game);

        if (!target.IsResolved)
        {
            LogUnresolved("stop", target, source, game);

            return false;
        }

        ArenaHost? host = FindHost(target.Arena);

        if (host is null)
        {
            return false;
        }

        await EndMatchAsync(host, ct);
        await AnnounceRoundEndIfLastAsync(ct);

        return true;
    }

    /// <summary>Ends every match in the room, whatever arena it is on. The room is being cleared —
    /// a moderator wipe, a shutdown — not a game being stopped, so there is no target to resolve.</summary>
    public async Task EndAllGamesAsync(CancellationToken ct)
    {
        foreach (ArenaHost host in Snapshot())
        {
            await EndMatchAsync(host, ct);
        }

        await AnnounceRoundEndIfLastAsync(ct);
    }

    /// <summary>Whether another live arena keeps score in the same book as this one — the guard that
    /// stops a second board's kick-off zeroing the first board's running tally.</summary>
    private bool IsBookBusy(ArenaHost host)
    {
        foreach (ArenaHost other in _hosts)
        {
            if (
                !ReferenceEquals(other, host)
                && other.IsLive
                && ReferenceEquals(other.Teams, host.Teams)
            )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Fires the room's GAME_STARTS once, on the first arena to go live. The wired triggers
    /// are room-level, so a second arena starting is not a second round.</summary>
    private async Task AnnounceRoundStartAsync(CancellationToken ct)
    {
        if (_roundAnnounced)
        {
            return;
        }

        // Set before publishing, so a GAME_STARTS box that starts another arena falls into the guard
        // above instead of recursing.
        _roundAnnounced = true;

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameStartedEvent
            {
                RoomId = _roomGrain.RoomId,
                CausedBy = ActionContext.Wired,
            },
            ct
        );
    }

    /// <summary>Fires the room's GAME_ENDS once the LAST live arena has stopped.</summary>
    private async Task AnnounceRoundEndIfLastAsync(CancellationToken ct)
    {
        if (!_roundAnnounced || IsRunning)
        {
            return;
        }

        _roundAnnounced = false;

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameEndedEvent { RoomId = _roomGrain.RoomId, CausedBy = ActionContext.Wired },
            ct
        );
    }

    /// <summary>
    /// A game's own rules decided its round is over. It ends THAT arena's match and, if it was the
    /// last one running, the room's round with it — which is what fires GAME_ENDS, and why a module
    /// must never call its own end hook. The game-timer furni is reset with it, because an early
    /// finish leaves the countdown running.
    /// </summary>
    internal async Task RequestRoundEndAsync(ArenaHost host, CancellationToken ct)
    {
        await EndMatchAsync(host, ct);
        await AnnounceRoundEndIfLastAsync(ct);

        if (!IsRunning)
        {
            Chrome.ResetGameTimers();
        }
    }

    private void LogUnresolved(string verb, GameTarget target, RoomObjectId source, GameId game)
    {
        if (target.Outcome == GameTargetOutcome.Ambiguous)
        {
            _roomGrain._logger.LogInformation(
                "Refused to {Verb} a game in room {RoomId}: {Count} arenas could have been "
                    + "meant and nothing chose between them (source {Source}, game '{Game}'). "
                    + "Name the game, or put the control beside the arena it belongs to.",
                verb,
                _roomGrain.RoomId,
                target.CandidateCount,
                source,
                game.IsNone ? "any" : game.Value
            );

            return;
        }

        _roomGrain._logger.LogDebug(
            "Nothing to {Verb} in room {RoomId} (source {Source}, game '{Game}').",
            verb,
            _roomGrain.RoomId,
            source,
            game.IsNone ? "any" : game.Value
        );
    }

    internal ArenaHost? FindHost(ArenaId arena)
    {
        foreach (ArenaHost host in _hosts)
        {
            if (host.Id == arena)
            {
                return host;
            }
        }

        return null;
    }

    /// <summary>Ends one arena's match, through the phases and with cleanup guaranteed.</summary>
    internal async Task EndMatchAsync(ArenaHost host, CancellationToken ct)
    {
        if (!GameStateMachine.HasMatch(host.Phase))
        {
            return;
        }

        // Phase flips before the module hook runs, so a module that ends the round from inside its
        // own tick and lands back here falls into the guard above instead of recursing.
        if (host.Phase == GamePhase.Running || host.Phase == GamePhase.Countdown)
        {
            if (host.Phase == GamePhase.Countdown)
            {
                // A match abandoned during its countdown never had rules to wind down; skip the
                // showcase phase entirely rather than dwelling on an outcome that never happened.
                await TransitionAsync(host, GamePhase.Resetting, ct);
                await TransitionAsync(host, GamePhase.Idle, ct);

                return;
            }

            await TransitionAsync(host, GamePhase.RoundEnding, ct);
            await AdvancePhasesAsync(host, _nowMs, ct);

            return;
        }

        // Preparing, RoundEnding, Finished: fall straight to cleanup.
        await TransitionAsync(host, GamePhase.Resetting, ct);
        await TransitionAsync(host, GamePhase.Idle, ct);
    }

    private async Task StartMatchAsync(ArenaHost host, CancellationToken ct)
    {
        host.Sequence++;
        host.Match = new GameMatch(new MatchId(_roomGrain.RoomId, host.Id, host.Sequence), _nowMs);

        // Seeded from the match id — the ARENA's, so two boards of the same game in one room do not
        // replay each other's rolls — which is what makes a Freeze power-up or a Banzai teleport
        // destination assertable in a test.
        host.Random = new GameRandom(
            HashCode.Combine(
                _roomGrain.RoomId.Value,
                host.Id.Game.Value,
                host.Id.Instance,
                host.Sequence
            )
        );

        await TransitionAsync(host, GamePhase.Preparing, ct);

        if (host.Phase != GamePhase.Preparing)
        {
            // Preparing threw its way into cleanup; nothing more to do.
            return;
        }

        GameProfile profile = host.Game.Profile;

        if (profile.CountdownMs > 0)
        {
            host.PhaseDeadlineMs = _nowMs + profile.CountdownMs;

            await TransitionAsync(host, GamePhase.Countdown, ct);

            return;
        }

        await TransitionAsync(host, GamePhase.Running, ct);
    }

    // ---- tick --------------------------------------------------------------

    /// <summary>
    /// One room frame. Arenas in <see cref="GamePhase.Idle"/> are not called at all unless they asked
    /// to be: at twenty frames a second per room, the overwhelming majority of which host no game,
    /// "return early when idle" was still a virtual call per arena per frame.
    /// </summary>
    public async Task TickAsync(long nowMs, CancellationToken ct)
    {
        _nowMs = nowMs;

        foreach (ArenaHost host in Snapshot())
        {
            if (host.Phase == GamePhase.Idle && !host.WantsIdleTick)
            {
                continue;
            }

            // Cleared before the call, so a module that still has work re-arms it from inside its
            // own tick and one that has finished simply stops being called.
            host.WantsIdleTick = false;

            await RunGuardedAsync(host, "tick", () => host.Game.TickAsync(nowMs, ct), ct);
            await AdvancePhasesAsync(host, nowMs, ct);
        }
    }

    /// <summary>Drives the timed phases forward. Loops because a zero-length showcase phase means
    /// RoundEnding, Finished, Resetting and Idle all land in the same turn.</summary>
    private async Task AdvancePhasesAsync(ArenaHost host, long nowMs, CancellationToken ct)
    {
        for (int guard = 0; guard < 8; guard++)
        {
            switch (host.Phase)
            {
                case GamePhase.Countdown when nowMs >= host.PhaseDeadlineMs:
                    await TransitionAsync(host, GamePhase.Running, ct);

                    return;

                case GamePhase.RoundEnding when nowMs >= host.PhaseDeadlineMs:
                    if (host.Match is not null && host.Match.Round < host.Game.Profile.Rounds)
                    {
                        host.Match.Round++;
                        await TransitionAsync(host, GamePhase.Preparing, ct);
                        await TransitionAsync(host, GamePhase.Running, ct);

                        return;
                    }

                    await TransitionAsync(host, GamePhase.Finished, ct);

                    break;

                case GamePhase.Finished:
                    await TransitionAsync(host, GamePhase.Resetting, ct);

                    break;

                case GamePhase.Resetting:
                    await TransitionAsync(host, GamePhase.Idle, ct);

                    return;

                default:
                    return;
            }
        }
    }

    // ---- phase transition --------------------------------------------------

    private async Task TransitionAsync(ArenaHost host, GamePhase to, CancellationToken ct)
    {
        GamePhase from = host.Phase;

        if (!GameStateMachine.CanTransition(from, to))
        {
            _roomGrain._logger.LogWarning(
                "Rejected game phase transition {From} -> {To} for arena {Arena} in room {RoomId}.",
                from,
                to,
                host.Id,
                _roomGrain.RoomId
            );

            return;
        }

        GameMatch? match = host.Match;

        host.Phase = to;

        if (to == GamePhase.RoundEnding)
        {
            host.PhaseDeadlineMs = _nowMs + host.Game.Profile.RoundEndMs;
        }

        if (match is null)
        {
            return;
        }

        await PublishGameEventAsync(
            new GamePhaseChangedEvent
            {
                Game = host.Id.Game,
                Match = match.Id,
                From = from,
                To = to,
            },
            ct
        );

        switch (to)
        {
            case GamePhase.Preparing:
                if (
                    !await RunGuardedAsync(
                        host,
                        "prepare",
                        () => host.Game.OnPreparingAsync(match, ct),
                        ct
                    )
                )
                {
                    // A game that could not set its arena up must not then play a match on it.
                    await TransitionAsync(host, GamePhase.Resetting, ct);
                    await TransitionAsync(host, GamePhase.Idle, ct);
                }

                break;

            case GamePhase.Running:
                await RunGuardedAsync(host, "start", () => host.Game.OnStartedAsync(match, ct), ct);
                await PublishGameEventAsync(
                    new GameMatchStartedEvent
                    {
                        Game = host.Id.Game,
                        Match = match.Id,
                        Round = match.Round,
                    },
                    ct
                );
                break;

            case GamePhase.RoundEnding:
                await RunGuardedAsync(
                    host,
                    "round-end",
                    () => host.Game.OnRoundEndingAsync(match, ct),
                    ct
                );
                break;

            case GamePhase.Finished:
                await PublishGameEventAsync(
                    new GameMatchEndedEvent
                    {
                        Game = host.Id.Game,
                        Match = match.Id,
                        Outcome = BuildOutcome(host),
                    },
                    ct
                );
                break;

            case GamePhase.Resetting:
                await RunGuardedAsync(
                    host,
                    "reset",
                    () => host.Game.OnResettingAsync(match, ct),
                    ct
                );
                break;

            case GamePhase.Idle:
                // The match is over the moment the game stops holding anything from it. Dropping the
                // reference here is what guarantees no later callback can find it.
                host.Match = null;
                break;
        }
    }

    // ---- signals -----------------------------------------------------------

    /// <summary>Routes one component signal to the ARENA that owns the component — the game it
    /// belongs to, and within that game the installation its furniture sits in. O(arenas in the
    /// room), which is two or three.</summary>
    public async Task SignalAsync(GameSignal signal, CancellationToken ct)
    {
        GameId game = signal.Component.Game;
        int instance = PartitionOf(game).InstanceOf(signal.Component.ObjectId);
        ArenaId arena = new(game, instance);

        foreach (ArenaHost host in _hosts)
        {
            if (host.Id != arena)
            {
                continue;
            }

            await RunGuardedAsync(host, "signal", () => host.Game.OnSignalAsync(signal, ct), ct);

            return;
        }
    }

    /// <summary>Whether ANY arena of that game has a live match — what a team gate reads to go
    /// unwalkable mid-match.</summary>
    public bool IsRunningGame(GameId game)
    {
        foreach (ArenaHost host in _hosts)
        {
            if (host.Id.Game == game && host.IsLive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The furthest-along phase any arena of that game is in. A room with one arena per
    /// game — every Habbo room — reads this as simply "that game's phase".</summary>
    public GamePhase PhaseOf(GameId game)
    {
        GamePhase phase = GamePhase.Idle;

        foreach (ArenaHost host in _hosts)
        {
            if (host.Id.Game == game && host.Phase != GamePhase.Idle)
            {
                phase = host.Phase;
            }
        }

        return phase;
    }

    public GamePhase PhaseOf(ArenaId arena) => FindHost(arena)?.Phase ?? GamePhase.Idle;

    // ---- participants ------------------------------------------------------

    /// <summary>Clears membership when a player leaves the room, so team state never outlives a
    /// player's presence, and lets every arena drop whatever it held for them.</summary>
    public async Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        _roomTeams.OnPlayerLeft(playerId);

        foreach (ArenaHost host in Snapshot())
        {
            if (!host.SharesRoomTeams)
            {
                host.Teams.OnPlayerLeft(playerId);
            }

            await RunGuardedAsync(
                host,
                "player-left",
                () => host.Game.OnParticipantLeftAsync(playerId, ct),
                ct
            );
        }
    }

    public async Task OnPlayerEnteredAsync(PlayerId playerId, CancellationToken ct)
    {
        foreach (ArenaHost host in Snapshot())
        {
            await RunGuardedAsync(
                host,
                "player-entered",
                () => host.Game.OnParticipantEnteredAsync(playerId, ct),
                ct
            );
        }
    }

    // ---- teams and scores: the room's Habbo-facing surface -------------------
    //
    // Everything below speaks GameTeamColor, and that is deliberate: these are the members the wired
    // boxes, the coloured furniture and IRoomGameAccess use, and a colour is exactly what those
    // things have. They translate once, here, into the room's ledger. Nothing further in means a
    // colour again.

    public GameTeamColor GetTeam(PlayerId playerId) =>
        RoomPalette.ColourOf(_roomTeams.GetTeam(playerId));

    public int GetTeamScore(GameTeamColor team) =>
        _roomTeams.GetTeamScore(RoomPalette.TeamOf(team));

    public IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team) =>
        _roomTeams.GetPlayersInTeam(RoomPalette.TeamOf(team));

    /// <summary>The team with the highest score in the room's ledger, or None on a scoreless round.</summary>
    public GameTeamColor LeadingTeam => RoomPalette.ColourOf(_roomTeams.GetLeadingTeam());

    public Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct) =>
        _roomTeams.JoinTeam(playerId, RoomPalette.TeamOf(team))
            ? Chrome.BroadcastTeamAuraAsync(playerId, GameAuraSet.Wired, team)
            : Task.CompletedTask;

    public Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct) =>
        _roomTeams.LeaveTeam(playerId) ? Chrome.ClearEffectAsync(playerId) : Task.CompletedTask;

    public async Task<bool> TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    )
    {
        TeamId team = _roomTeams.GetTeam(playerId);
        int previous = _roomTeams.GetTeamScore(team);

        if (!_roomTeams.TryGiveScoreToPlayerTeam(box, playerId, amount, cap))
        {
            return false;
        }

        await AnnounceScoreAsync(
            new GameScore(team, playerId, amount, ScoreReason.Wired, box),
            previous,
            RoomPalette,
            LiveRoomLedgerHost(),
            ct
        );

        return true;
    }

    public async Task<bool> TryGiveScoreToTeamAsync(
        RoomObjectId box,
        GameTeamColor team,
        int amount,
        int cap,
        CancellationToken ct
    )
    {
        TeamId target = RoomPalette.TeamOf(team);
        int previous = _roomTeams.GetTeamScore(target);

        if (!_roomTeams.TryGiveScoreToTeam(box, target, amount, cap))
        {
            return false;
        }

        await AnnounceScoreAsync(
            new GameScore(target, default, amount, ScoreReason.Wired, box),
            previous,
            RoomPalette,
            LiveRoomLedgerHost(),
            ct
        );

        return true;
    }

    /// <summary>The live arena keeping score in the room's ledger, if there is one — what stamps a
    /// wired give-score with the match it landed in.</summary>
    private ArenaHost? LiveRoomLedgerHost()
    {
        foreach (ArenaHost host in _hosts)
        {
            if (host.IsLive && host.SharesRoomTeams)
            {
                return host;
            }
        }

        return null;
    }

    // ---- scoring: the domain path -------------------------------------------

    /// <summary>
    /// Applies a game's scoring act. Refused outside a live match — "a finished game cannot accept
    /// score changes" is an invariant here rather than a rule each module remembers — and a no-op
    /// for a teamless or zero award, which must not fire the trigger.
    /// </summary>
    internal async Task ApplyScoreAsync(ArenaHost host, GameScore score, CancellationToken ct)
    {
        if (!host.IsLive || !host.Teams.Knows(score.Team) || score.Amount == 0)
        {
            return;
        }

        int previous = host.Teams.GetTeamScore(score.Team);

        host.Teams.AddScore(score.Team, score.Amount);

        // A score clamped at 0 by a negative award did not actually change: no event.
        if (host.Teams.GetTeamScore(score.Team) == previous)
        {
            return;
        }

        await AnnounceScoreAsync(score, previous, host.Palette, host, ct);
    }

    private async Task AnnounceScoreAsync(
        GameScore score,
        int previous,
        HabboTeamPalette palette,
        ArenaHost? host,
        CancellationToken ct
    )
    {
        TeamBook book = host?.Teams ?? _roomTeams;
        int updated = book.GetTeamScore(score.Team);

        // The wired half: SCORE_ACHIEVED reads the room event bus, and a wired box that scores
        // outside any match must reach it exactly as a game's own award does. Only the room's own
        // ledger is published, because only it is what a coloured board and a wired team condition
        // are reading — an arena with a private team space has no colour to announce.
        if (host is null || host.SharesRoomTeams)
        {
            await _roomGrain.PublishRoomEventAsync(
                new WiredTeamScoreChangedEvent
                {
                    RoomId = _roomGrain.RoomId,
                    CausedBy = ActionContext.Wired,
                    Team = palette.ColourOf(score.Team),
                    Score = updated,
                    PreviousScore = previous,
                },
                ct
            );
        }

        await PublishGameEventAsync(
            new GameScoreChangedEvent
            {
                Game = host?.Id.Game ?? GameId.None,
                Match = host?.Match?.Id ?? MatchId.None,
                Score = score,
                PreviousTotal = previous,
                NewTotal = updated,
            },
            ct
        );
    }

    // ---- events ------------------------------------------------------------

    /// <summary>Fans one game event out to every sink, keeping a sink's failure to itself: a broken
    /// scoreboard must not be able to abort a match.</summary>
    internal async Task PublishGameEventAsync(GameEvent evt, CancellationToken ct)
    {
        foreach (IGameEventSink sink in _sinks)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await sink.OnGameEventAsync(evt, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Game event sink {Sink} failed on {Event} in room {RoomId}.",
                    sink.GetType().Name,
                    evt.GetType().Name,
                    _roomGrain.RoomId
                );
            }
        }
    }

    /// <summary>The match's tally in the game's own team terms. A colour appears nowhere in it; the
    /// presentation layer projects it for the boards that need one.</summary>
    internal MatchOutcome BuildOutcome(ArenaHost host)
    {
        Dictionary<TeamId, int> scores = [];
        Dictionary<TeamId, IReadOnlyList<string>> names = [];

        foreach (TeamId team in host.Teams.Teams.Ids())
        {
            scores[team] = host.Teams.GetTeamScore(team);

            List<string> members = [];

            foreach (PlayerId playerId in host.Teams.GetPlayersInTeam(team))
            {
                if (host.Context.NameOf(playerId) is string name)
                {
                    members.Add(name);
                }
            }

            names[team] = members;
        }

        return new MatchOutcome
        {
            WinningTeam = host.Teams.GetLeadingTeam(),
            Scores = scores,
            MemberNames = names,
        };
    }

    // ---- failure containment ------------------------------------------------

    private ArenaValidation SafeValidate(ArenaHost host)
    {
        try
        {
            return host.Game.ValidateArena();
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Arena {Arena} failed to validate in room {RoomId}; it counts as unplayable.",
                host.Id,
                _roomGrain.RoomId
            );

            return ArenaValidation
                .Builder()
                .Require("arena validation", found: 0, required: 1)
                .Build();
        }
    }

    /// <summary>
    /// Runs one step of one arena, keeping its failure to itself. The arenas in a room are
    /// independent: a Freeze rink that cannot read its balance config must not stop the room's
    /// football match from kicking off, and a game that throws mid-match is torn down cleanly rather
    /// than left half-started. Failures are logged, never swallowed.
    /// </summary>
    private async Task<bool> RunGuardedAsync(
        ArenaHost host,
        string step,
        Func<Task> stepAsync,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await stepAsync();

            return true;
        }
        catch (OperationCanceledException)
        {
            // The grain is going away; not a game failure.
            throw;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Arena {Arena} failed to {Step} in room {RoomId} (match {Match}, phase {Phase}); "
                    + "the room's other arenas carry on.",
                host.Id,
                step,
                _roomGrain.RoomId,
                host.Match?.Id ?? MatchId.None,
                host.Phase
            );

            return false;
        }
    }

    /// <summary>A copy of the host list to fan out over: a game that ends another arena from inside a
    /// hook must not invalidate the enumeration in progress.</summary>
    private List<ArenaHost> Snapshot() => [.. _hosts];

    // ---- room teardown ------------------------------------------------------

    /// <summary>
    /// The room is unloading. Every match is torn down through its own cleanup so nothing survives
    /// the activation — no timers, no effects, no queued work, no references.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken ct)
    {
        _roundAnnounced = false;

        foreach (ArenaHost host in Snapshot())
        {
            if (!GameStateMachine.HasMatch(host.Phase))
            {
                continue;
            }

            try
            {
                await TransitionAsync(host, GamePhase.Resetting, ct);
                await TransitionAsync(host, GamePhase.Idle, ct);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogError(
                    ex,
                    "Arena {Arena} failed to shut down in room {RoomId}.",
                    host.Id,
                    _roomGrain.RoomId
                );
            }
        }
    }
}

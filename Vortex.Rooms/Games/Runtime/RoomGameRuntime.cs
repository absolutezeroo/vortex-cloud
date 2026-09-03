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
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Games.Events;
using Vortex.Rooms.Games.Presentation;
using Vortex.Rooms.Games.Scoring;
using Vortex.Rooms.Games.Teams;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>
/// The room's game runtime: the one coordinator, and deliberately not a god manager. It knows the
/// lifecycle, the team ledger, the event fan-out and how to route a component signal — and nothing
/// at all about Battle Banzai, Freeze or football. Every rule lives in a module; this file would not
/// change if the room hosted twenty games.
/// <para><b>Concurrency.</b> Everything here runs inside the room grain's single-threaded turn, so
/// there is no locking and none is wanted: Orleans already serialises the room, and a lock inside an
/// actor can only deadlock. The property that needs care is not exclusion but atomicity across an
/// <c>await</c>: the turn can interleave at every one, so every fan-out iterates a snapshot and every
/// deferred piece of work re-checks the match it belongs to.</para>
/// <para><b>Match isolation.</b> A match id is minted per game per round and carried by everything a
/// module defers. A callback from a finished match finds the phase changed and drops itself, which is
/// what makes "events from match N cannot mutate match N+1" a property of the system rather than a
/// rule each game has to remember.</para>
/// <para><b>State is ephemeral.</b> Built fresh when the room grain activates, so a match dies with
/// the room — matching Habbo, and making a zombie match impossible by construction.</para>
/// </summary>
public sealed class RoomGameRuntime
{
    private readonly RoomGrain _roomGrain;
    private readonly List<GameHost> _hosts = [];
    private readonly List<IGameEventSink> _sinks = [];
    private readonly GameTeamBook _teams = new();

    private long _nowMs;

    /// <summary>Whether the room's round has been announced (GAME_STARTS fired, GAME_ENDS not yet).
    /// One flag for the room, because the wired triggers are room-level: a room does not fire two
    /// GAME_STARTS because it happens to host two games.</summary>
    private bool _roundAnnounced;

    public RoomGameRuntime(RoomGrain roomGrain)
    {
        _roomGrain = roomGrain;
        Chrome = new RoomGameChrome(roomGrain);
    }

    /// <summary>The room's client-facing game plumbing.</summary>
    public IGameChrome Chrome { get; }

    /// <summary>The room's one team + score ledger, shared by every game and read by every wired
    /// team leaf.</summary>
    internal GameTeamBook TeamBook => _teams;

    /// <summary>The timestamp of the room tick being processed, for modules that need "now" outside
    /// their own tick (a signal handler queueing deferred work).</summary>
    internal long NowMs => _nowMs;

    /// <summary>The games plugged into this room, in registration order.</summary>
    public IReadOnlyList<IRoomGame> Games
    {
        get
        {
            List<IRoomGame> games = [];

            foreach (GameHost host in _hosts)
            {
                games.Add(host.Game);
            }

            return games;
        }
    }

    /// <summary>Whether any game in the room currently has a live match. What the wired engine and
    /// the timer furni mean by "the room's game is running".</summary>
    public bool IsRunning
    {
        get
        {
            foreach (GameHost host in _hosts)
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
    /// Plugs a game into the room. The factory receives the context the game will use for its whole
    /// life, so a module captures it in a readonly field instead of being handed the room later.
    /// Called once per game while the room grain is being constructed.
    /// </summary>
    public void Register(Func<IRoomGameContext, IRoomGame> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        GameHost host = NewHost();

        host.Game = factory(host.Context);

        _hosts.Add(host);
    }

    private GameHost NewHost()
    {
        GameHost host = new();
        RoomGameArena arena = new(host, _roomGrain);

        host.Arena = arena;
        host.Context = new RoomGameContext(_roomGrain, this, host, arena);

        return host;
    }

    /// <summary>Attaches an event sink (a scoreboard painter, the diagnostics tracer). Sinks see
    /// every game's events.</summary>
    public void AddSink(IGameEventSink sink) => _sinks.Add(sink);

    // ---- lifecycle ---------------------------------------------------------

    /// <summary>
    /// Starts a match in every game whose arena validates. Idempotent per game: a game already in a
    /// match is skipped rather than restarted.
    /// <para>
    /// The order is load-bearing and has been wrong before. The shared scores are zeroed FIRST, then
    /// the wired GAME_STARTS trigger fires, and only then do the games prepare: a GAME_STARTS box
    /// wired to a give-score action runs off that event, and a reset arriving afterwards would wipe
    /// the points it just awarded with no error and no log.
    /// </para>
    /// </summary>
    public async Task StartGameAsync(CancellationToken ct)
    {
        if (_roundAnnounced)
        {
            return;
        }

        // Set before anything else, so a game that starts another round from inside its own prepare
        // hook falls into the guard above instead of recursing.
        _roundAnnounced = true;

        // Teams survive: they are picked at the gates before kick-off, so wiping membership here
        // would empty the arena.
        _teams.ResetScores();

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameStartedEvent
            {
                RoomId = _roomGrain.RoomId,
                CausedBy = ActionContext.Wired,
            },
            ct
        );

        foreach (GameHost host in Snapshot())
        {
            // A new round supersedes the previous one's showcase: pressing the timer again while
            // Banzai is still blinking its winner starts the next match rather than being ignored
            // for the five seconds the celebration lasts.
            if (host.Phase is GamePhase.RoundEnding or GamePhase.Finished or GamePhase.Resetting)
            {
                await TransitionAsync(host, GamePhase.Resetting, ct);
                await TransitionAsync(host, GamePhase.Idle, ct);
            }

            if (host.Phase != GamePhase.Idle)
            {
                continue;
            }

            await StartMatchAsync(host, ct);
        }
    }

    /// <summary>Ends every running match. Idempotent; the final scores are left standing so a
    /// GAME_ENDS box can read the winner through the team rank/score conditions.</summary>
    public async Task EndGameAsync(CancellationToken ct)
    {
        if (!_roundAnnounced)
        {
            return;
        }

        // Set before the fan-out: a game that ends the round itself calls straight back into here
        // from its own round-ending hook, and this is what stops that becoming a loop.
        _roundAnnounced = false;

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameEndedEvent { RoomId = _roomGrain.RoomId, CausedBy = ActionContext.Wired },
            ct
        );

        foreach (GameHost host in Snapshot())
        {
            await EndMatchAsync(host, ct);
        }
    }

    /// <summary>
    /// A game's own rules decided the round is over. It ends the ROOM's round, not just its own
    /// match: that is what fires GAME_ENDS, and it is why a module must never call its own end hook.
    /// The game-timer furni is reset with it, because an early finish leaves the countdown running.
    /// </summary>
    internal async Task RequestRoundEndAsync(CancellationToken ct)
    {
        await EndGameAsync(ct);

        Chrome.ResetGameTimers();
    }

    /// <summary>Ends one game's match, through the phases and with cleanup guaranteed.</summary>
    internal async Task EndMatchAsync(GameHost host, CancellationToken ct)
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
                await FinishResetAsync(host, ct);

                return;
            }

            await TransitionAsync(host, GamePhase.RoundEnding, ct);
            await AdvancePhasesAsync(host, _nowMs, ct);

            return;
        }

        // Preparing, RoundEnding, Finished: fall straight to cleanup.
        await TransitionAsync(host, GamePhase.Resetting, ct);
        await FinishResetAsync(host, ct);
    }

    private async Task StartMatchAsync(GameHost host, CancellationToken ct)
    {
        ArenaValidation validation = SafeValidate(host);

        if (!validation.CanStart)
        {
            _roomGrain._logger.LogInformation(
                "Game {Game} did not start in room {RoomId}: {Shortfall}.",
                host.Game.Profile.Id,
                _roomGrain.RoomId,
                validation.DescribeShortfall()
            );

            return;
        }

        host.Sequence++;
        host.Match = new GameMatch(
            new MatchId(_roomGrain.RoomId, host.Game.Profile.Id, host.Sequence),
            _nowMs
        );

        // Seeded from the match id, so a match replays identically — which is what makes a Freeze
        // power-up roll or a Banzai teleport destination assertable in a test.
        host.Random = new GameRandom(
            HashCode.Combine(_roomGrain.RoomId.Value, host.Game.Profile.Id.Value, host.Sequence)
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
    /// One room frame. Games in <see cref="GamePhase.Idle"/> are not called at all unless they asked
    /// to be: at twenty frames a second per room, the overwhelming majority of which host no game,
    /// "return early when idle" was still a virtual call per game per frame.
    /// </summary>
    public async Task TickAsync(long nowMs, CancellationToken ct)
    {
        _nowMs = nowMs;

        foreach (GameHost host in Snapshot())
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
    private async Task AdvancePhasesAsync(GameHost host, long nowMs, CancellationToken ct)
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
                    await FinishResetAsync(host, ct);

                    return;

                default:
                    return;
            }
        }
    }

    private async Task FinishResetAsync(GameHost host, CancellationToken ct)
    {
        await TransitionAsync(host, GamePhase.Idle, ct);
    }

    // ---- phase transition --------------------------------------------------

    private async Task TransitionAsync(GameHost host, GamePhase to, CancellationToken ct)
    {
        GamePhase from = host.Phase;

        if (!GameStateMachine.CanTransition(from, to))
        {
            _roomGrain._logger.LogWarning(
                "Rejected game phase transition {From} -> {To} for {Game} in room {RoomId}.",
                from,
                to,
                host.Game.Profile.Id,
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

        if (match is not null)
        {
            await PublishGameEventAsync(
                new GamePhaseChangedEvent
                {
                    Game = host.Game.Profile.Id,
                    Match = match.Id,
                    From = from,
                    To = to,
                },
                ct
            );
        }

        if (match is null)
        {
            return;
        }

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
                        Game = host.Game.Profile.Id,
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
                        Game = host.Game.Profile.Id,
                        Match = match.Id,
                        Result = BuildResult(host),
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

    /// <summary>Routes one component signal to the game that owns the component. O(games in the
    /// room), which is two or three — not a dictionary lookup's worth of indirection.</summary>
    public async Task SignalAsync(GameSignal signal, CancellationToken ct)
    {
        GameId target = signal.Component.Game;

        foreach (GameHost host in _hosts)
        {
            if (host.Game.Profile.Id != target)
            {
                continue;
            }

            await RunGuardedAsync(host, "signal", () => host.Game.OnSignalAsync(signal, ct), ct);

            return;
        }
    }

    public bool IsRunningGame(GameId game)
    {
        foreach (GameHost host in _hosts)
        {
            if (host.Game.Profile.Id == game)
            {
                return host.IsLive;
            }
        }

        return false;
    }

    public GamePhase PhaseOf(GameId game)
    {
        foreach (GameHost host in _hosts)
        {
            if (host.Game.Profile.Id == game)
            {
                return host.Phase;
            }
        }

        return GamePhase.Idle;
    }

    // ---- participants ------------------------------------------------------

    /// <summary>Clears membership when a player leaves the room, so team state never outlives a
    /// player's presence, and lets every game drop whatever it held for them.</summary>
    public async Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        _teams.OnPlayerLeft(playerId);

        foreach (GameHost host in Snapshot())
        {
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
        foreach (GameHost host in Snapshot())
        {
            await RunGuardedAsync(
                host,
                "player-entered",
                () => host.Game.OnParticipantEnteredAsync(playerId, ct),
                ct
            );
        }
    }

    // ---- teams and scores --------------------------------------------------

    public GameTeamColor GetTeam(PlayerId playerId) => _teams.GetTeam(playerId);

    public int GetTeamScore(GameTeamColor team) => _teams.GetTeamScore(team);

    public IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team) =>
        _teams.GetPlayersInTeam(team);

    /// <summary>The team with the highest score, or None on a scoreless round.</summary>
    public GameTeamColor LeadingTeam => _teams.GetLeadingTeam();

    public Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct) =>
        _teams.JoinTeam(playerId, team)
            ? Chrome.BroadcastTeamAuraAsync(playerId, GameAuraSet.Wired, team)
            : Task.CompletedTask;

    public Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct) =>
        _teams.LeaveTeam(playerId) ? Chrome.ClearEffectAsync(playerId) : Task.CompletedTask;

    public async Task<bool> TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    )
    {
        GameTeamColor team = _teams.GetTeam(playerId);
        int previous = _teams.GetTeamScore(team);

        if (!_teams.TryGiveScoreToPlayerTeam(box, playerId, amount, cap))
        {
            return false;
        }

        await AnnounceScoreAsync(
            new GameScore(team, playerId, amount, ScoreReason.Wired, box),
            previous,
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
        int previous = _teams.GetTeamScore(team);

        if (!_teams.TryGiveScoreToTeam(box, team, amount, cap))
        {
            return false;
        }

        await AnnounceScoreAsync(
            new GameScore(team, default, amount, ScoreReason.Wired, box),
            previous,
            ct
        );

        return true;
    }

    /// <summary>
    /// Applies a game's scoring act. Refused outside a live match — "a finished game cannot accept
    /// score changes" is an invariant here rather than a rule each module remembers — and a no-op
    /// for a teamless or zero award, which must not fire the trigger.
    /// </summary>
    internal async Task ApplyScoreAsync(GameHost host, GameScore score, CancellationToken ct)
    {
        if (!host.IsLive)
        {
            return;
        }

        if (!GameTeamBook.IsRealTeam(score.Team) || score.Amount == 0)
        {
            return;
        }

        int previous = _teams.GetTeamScore(score.Team);

        _teams.AddScore(score.Team, score.Amount);

        // A score clamped at 0 by a negative award did not actually change: no event.
        if (_teams.GetTeamScore(score.Team) == previous)
        {
            return;
        }

        await AnnounceScoreAsync(score, previous, ct, host);
    }

    private async Task AnnounceScoreAsync(
        GameScore score,
        int previous,
        CancellationToken ct,
        GameHost? host = null
    )
    {
        int updated = _teams.GetTeamScore(score.Team);

        // The wired half: SCORE_ACHIEVED reads the room event bus, and a wired box that scores
        // outside any match must reach it exactly as a game's own award does.
        await _roomGrain.PublishRoomEventAsync(
            new WiredTeamScoreChangedEvent
            {
                RoomId = _roomGrain.RoomId,
                CausedBy = ActionContext.Wired,
                Team = score.Team,
                Score = updated,
                PreviousScore = previous,
            },
            ct
        );

        await PublishGameEventAsync(
            new GameScoreChangedEvent
            {
                Game = host?.Game.Profile.Id ?? GameId.None,
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

    internal GameMatchResult BuildResult(GameHost host)
    {
        Dictionary<GameTeamColor, int> scores = [];
        Dictionary<GameTeamColor, IReadOnlyList<string>> names = [];

        foreach (GameTeamColor team in host.Game.Profile.Teams.Colours)
        {
            scores[team] = _teams.GetTeamScore(team);

            List<string> members = [];

            foreach (PlayerId playerId in _teams.GetPlayersInTeam(team))
            {
                if (host.Context.NameOf(playerId) is string name)
                {
                    members.Add(name);
                }
            }

            names[team] = members;
        }

        return new GameMatchResult
        {
            WinningTeam = _teams.GetLeadingTeam(),
            Scores = scores,
            MemberNames = names,
        };
    }

    // ---- failure containment ------------------------------------------------

    private ArenaValidation SafeValidate(GameHost host)
    {
        try
        {
            return host.Game.ValidateArena();
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Game {Game} failed to validate its arena in room {RoomId}; treating it as unplayable.",
                host.Game.Profile.Id,
                _roomGrain.RoomId
            );

            return ArenaValidation
                .Builder()
                .Require("arena validation", found: 0, required: 1)
                .Build();
        }
    }

    /// <summary>
    /// Runs one step of one game, keeping its failure to itself. The games in a room are
    /// independent: a Freeze arena that cannot read its balance config must not stop the room's
    /// football match from kicking off, and a game that throws mid-match is torn down cleanly rather
    /// than left half-started. Failures are logged, never swallowed.
    /// </summary>
    private async Task<bool> RunGuardedAsync(
        GameHost host,
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
                "Game {Game} failed to {Step} in room {RoomId} (match {Match}, phase {Phase}); "
                    + "the room's other games carry on.",
                host.Game.Profile.Id,
                step,
                _roomGrain.RoomId,
                host.Match?.Id ?? MatchId.None,
                host.Phase
            );

            return false;
        }
    }

    /// <summary>A copy of the host list to fan out over: a game that registers or ends another game
    /// from inside a hook must not invalidate the enumeration in progress.</summary>
    private List<GameHost> Snapshot() => [.. _hosts];

    // ---- room teardown ------------------------------------------------------

    /// <summary>
    /// The room is unloading. Every match is torn down through its own cleanup so nothing survives
    /// the activation — no timers, no effects, no queued work, no references.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken ct)
    {
        _roundAnnounced = false;

        foreach (GameHost host in Snapshot())
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
                    "Game {Game} failed to shut down in room {RoomId}.",
                    host.Game.Profile.Id,
                    _roomGrain.RoomId
                );
            }
        }
    }
}

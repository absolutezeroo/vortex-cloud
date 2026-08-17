using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>The room's game coordinator. It owns two things every room game shares — the team membership
/// and team scores (the pure <see cref="GameTeamState"/>, the single source of truth, unit-tested in
/// isolation) and the round lifecycle that raises the wired GAME_STARTS / GAME_ENDS triggers — and it
/// drives the games registered on it through <see cref="IRoomMinigame"/>.
///
/// <para>
/// Nothing that starts or stops a round names an individual game: the game-timer furni and the wired
/// control-clock action call <see cref="StartGameAsync"/> / <see cref="EndGameAsync"/> and this class
/// fans out to whatever is registered. Adding Battle Banzai, football or hockey is a
/// <see cref="Register"/> line in the room grain's constructor, not an edit here or at any caller.
/// </para>
///
/// The state is ephemeral — constructed fresh each time the room grain activates, so a game naturally
/// dies with the room, matching Habbo. Every mutation runs inside the room grain's single-threaded turn,
/// so no locking is needed. The team aura is mirrored to the client through
/// <see cref="RoomGameChrome.BroadcastTeamAuraAsync"/> with the <see cref="GameAuraSet.Wired"/> set
/// (Red=33, Green=34, Blue=35, Yellow=36).</summary>
public sealed class RoomGameSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;
    private readonly GameTeamState _state = new();
    private readonly List<IRoomMinigame> _minigames = [];

    private bool _isRunning;

    /// <summary>Whether the room's wired game is currently running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>The room's one team + score store. Shared with every registered game so their teams and
    /// scores are visible to the wired team leaves.</summary>
    internal GameTeamState TeamState => _state;

    /// <summary>The games plugged into this room, in registration order.</summary>
    internal IReadOnlyList<IRoomMinigame> Minigames => _minigames;

    /// <summary>Plugs a game into the room's round lifecycle and tick. Called once per game from the room
    /// grain's constructor.</summary>
    public void Register(IRoomMinigame minigame) => _minigames.Add(minigame);

    /// <summary>Transition the room game to running (idempotent). The first start clears last round's
    /// scores, fires the wired GAME_STARTS trigger and then starts every registered game; a start while
    /// already running is a no-op.</summary>
    public async Task StartGameAsync(CancellationToken ct)
    {
        if (_isRunning)
        {
            return;
        }

        // Set before the fan-out, so a game that starts another round from inside its own StartAsync
        // falls into the no-op above instead of recursing.
        _isRunning = true;

        // Zero the scores BEFORE announcing the start, never after: a GAME_STARTS box wired to a
        // give-score action runs off the event below, and a reset that came afterwards would wipe the
        // points it just awarded without a trace. Teams survive — Freeze picks them at the gates before
        // kick-off, so wiping membership here would empty the arena.
        _state.ResetScores();

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameStartedEvent
            {
                RoomId = _roomGrain.RoomId,
                CausedBy = ActionContext.Wired,
            },
            ct
        );

        await ForEachMinigameAsync("start", game => game.StartAsync(ct), ct);
    }

    /// <summary>Transition the room game to stopped (idempotent). Ending a running game fires the wired
    /// GAME_ENDS trigger and then ends every registered game; ending an already-stopped game is a no-op.
    /// The scores are left standing so the games can show their final tally and a GAME_ENDS box can read
    /// the winner through the team rank/score conditions.</summary>
    public async Task EndGameAsync(CancellationToken ct)
    {
        if (!_isRunning)
        {
            return;
        }

        // Set before the fan-out: a game that ends the round itself (Freeze does, the moment one team is
        // left standing) calls straight back into here, and this is what stops that becoming a loop.
        _isRunning = false;

        await _roomGrain.PublishRoomEventAsync(
            new WiredGameEndedEvent { RoomId = _roomGrain.RoomId, CausedBy = ActionContext.Wired },
            ct
        );

        await ForEachMinigameAsync("end", game => game.EndAsync(ct), ct);
    }

    public GameTeamColor GetTeam(PlayerId playerId) => _state.GetTeam(playerId);

    public int GetTeamScore(GameTeamColor team) => _state.GetTeamScore(team);

    public IReadOnlyList<PlayerId> GetPlayersInTeam(GameTeamColor team) =>
        _state.GetPlayersInTeam(team);

    public Task JoinTeamAsync(PlayerId playerId, GameTeamColor team, CancellationToken ct) =>
        _state.JoinTeam(playerId, team)
            ? _roomGrain.GameChrome.BroadcastTeamAuraAsync(playerId, GameAuraSet.Wired, team)
            : Task.CompletedTask;

    public Task LeaveTeamAsync(PlayerId playerId, CancellationToken ct) =>
        _state.LeaveTeam(playerId)
            ? _roomGrain.GameChrome.ClearEffectAsync(playerId)
            : Task.CompletedTask;

    public async Task<bool> TryGiveScoreToPlayerTeamAsync(
        RoomObjectId box,
        PlayerId playerId,
        int amount,
        int cap,
        CancellationToken ct
    )
    {
        GameTeamColor team = _state.GetTeam(playerId);
        int previousScore = _state.GetTeamScore(team);

        if (!_state.TryGiveScoreToPlayerTeam(box, playerId, amount, cap))
        {
            return false;
        }

        await PublishScoreChangedAsync(team, previousScore, ct);

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
        int previousScore = _state.GetTeamScore(team);

        if (!_state.TryGiveScoreToTeam(box, team, amount, cap))
        {
            return false;
        }

        await PublishScoreChangedAsync(team, previousScore, ct);

        return true;
    }

    /// <summary>Awards (or deducts) points to a team outside the capped wired score actions — the path
    /// the Freeze minigame scores through — and fires the wired SCORE_ACHIEVED trigger like any other
    /// score change. A no-op for a teamless award, which must not fire the trigger.</summary>
    public async Task AddTeamScoreAsync(GameTeamColor team, int amount, CancellationToken ct)
    {
        if (!GameTeamState.IsRealTeam(team) || amount == 0)
        {
            return;
        }

        int previousScore = _state.GetTeamScore(team);

        _state.AddScore(team, amount);

        // A score clamped at 0 by a negative award did not actually change: no event.
        if (_state.GetTeamScore(team) == previousScore)
        {
            return;
        }

        await PublishScoreChangedAsync(team, previousScore, ct);
    }

    private Task PublishScoreChangedAsync(
        GameTeamColor team,
        int previousScore,
        CancellationToken ct
    ) =>
        _roomGrain.PublishRoomEventAsync(
            new WiredTeamScoreChangedEvent
            {
                RoomId = _roomGrain.RoomId,
                CausedBy = ActionContext.Wired,
                Team = team,
                Score = _state.GetTeamScore(team),
                PreviousScore = previousScore,
            },
            ct
        );

    /// <summary>Clears membership when a player leaves the room, so team state never outlives a player's
    /// presence, and lets every registered game drop whatever it held for them. No effect broadcast —
    /// the avatar is already gone.</summary>
    public async Task OnPlayerLeftAsync(PlayerId playerId, CancellationToken ct)
    {
        _state.OnPlayerLeft(playerId);

        await ForEachMinigameAsync("player-left", game => game.OnPlayerLeftAsync(playerId, ct), ct);
    }

    /// <summary>Lets every registered game react to a player entering the room. Team auras re-sync on
    /// their own through the avatar snapshot; this is for game-specific entry state.</summary>
    public Task OnPlayerEnteredAsync(PlayerId playerId, CancellationToken ct) =>
        ForEachMinigameAsync("player-entered", game => game.OnPlayerEnteredAsync(playerId, ct), ct);

    /// <summary>
    /// Runs one lifecycle step against every registered game, keeping each game's failure to itself.
    /// The games in a room are independent, so one that throws must not stop the rest from starting,
    /// ending or cleaning up — a Freeze arena that cannot read its balance config should not be able to
    /// stop the room's football match from kicking off. Failures are logged, never swallowed. The tick
    /// path gets the same isolation from <c>RoomGrain.RunTickStepAsync</c>.
    /// </summary>
    private async Task ForEachMinigameAsync(
        string step,
        Func<IRoomMinigame, Task> stepAsync,
        CancellationToken ct
    )
    {
        foreach (IRoomMinigame minigame in _minigames)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await stepAsync(minigame);
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
                    "Game {Game} failed to {Step} in room {RoomId}; the room's other games carry on.",
                    minigame.Name,
                    step,
                    _roomGrain.RoomId
                );
            }
        }
    }
}

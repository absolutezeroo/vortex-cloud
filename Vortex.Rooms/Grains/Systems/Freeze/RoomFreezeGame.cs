using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>Whether a Freeze game is accepting players (Idle) or in progress (Running).</summary>
public enum FreezeGamePhase
{
    Idle = 0,
    Running = 1,
}

/// <summary>The outcome of a player touching a team gate.</summary>
public enum FreezeGateResult
{
    None = 0,
    Joined = 1,
    Left = 2,
}

/// <summary>
/// The pure state and rules of a room's Freeze game: which players are on which team (each with their
/// own <see cref="FreezePlayerState"/>) and the game phase. No IO — every effect, teleport, tile update
/// and broadcast is done by <see cref="RoomFreezeSystem"/> from the signals this returns. Kept
/// unit-testable in isolation, matching <see cref="GameTeamState"/>.
/// <para>
/// Team membership and score are NOT stored here: they live in the room's shared
/// <see cref="GameTeamState"/> (<see cref="Teams"/>), which every wired team leaf reads. This class
/// mirrors each gate join/leave into it so a Freeze player is a team member as far as
/// <c>wf_cnd_actor_in_team</c>, <c>wf_slc_users_team</c> and the team score/rank conditions are
/// concerned. The default instance exists so the rules stay testable standalone; the room passes its
/// own.
/// </para>
/// </summary>
public sealed class RoomFreezeGame
{
    private readonly Dictionary<PlayerId, FreezePlayerState> _players = [];

    /// <summary>The room's shared team + score store. Set once by <see cref="RoomFreezeSystem"/> to the
    /// room's <see cref="RoomGameSystem"/> state.</summary>
    public GameTeamState Teams { get; init; } = new();

    /// <summary>The live balance for this game; refreshed from server config by the wrapper each round.</summary>
    public FreezeSettings Settings { get; set; } = FreezeSettings.Default;

    public FreezeGamePhase Phase { get; private set; } = FreezeGamePhase.Idle;

    public bool IsRunning => Phase == FreezeGamePhase.Running;

    public IReadOnlyDictionary<PlayerId, FreezePlayerState> Players => _players;

    public FreezePlayerState? GetPlayer(PlayerId playerId) =>
        _players.TryGetValue(playerId, out FreezePlayerState? player) ? player : null;

    public GameTeamColor GetTeam(PlayerId playerId) =>
        _players.TryGetValue(playerId, out FreezePlayerState? player)
            ? player.Team
            : GameTeamColor.None;

    public int GetTeamCount(GameTeamColor team)
    {
        int count = 0;

        foreach (FreezePlayerState player in _players.Values)
        {
            if (player.Team == team && !player.Dead)
            {
                count++;
            }
        }

        return count;
    }

    public int GetTeamScore(GameTeamColor team) => Teams.GetTeamScore(team);

    /// <summary>How many distinct teams still have at least one living player.</summary>
    public int LivingTeamCount()
    {
        int mask = 0;

        foreach (FreezePlayerState player in _players.Values)
        {
            if (!player.Dead && GameTeamState.IsRealTeam(player.Team))
            {
                mask |= 1 << (int)player.Team;
            }
        }

        return System.Numerics.BitOperations.PopCount((uint)mask);
    }

    /// <summary>
    /// Toggles the player's membership of <paramref name="team"/> — walking onto your own team's gate
    /// again leaves it. Only allowed while the game is idle (teams are picked before the round). Returns
    /// what happened so the wrapper can apply/clear the team effect and refresh the gate counters.
    /// </summary>
    public FreezeGateResult ToggleGate(PlayerId playerId, GameTeamColor team)
    {
        if (Phase != FreezeGamePhase.Idle || !GameTeamState.IsRealTeam(team))
        {
            return FreezeGateResult.None;
        }

        // Touching your own team's gate leaves it.
        if (
            _players.TryGetValue(playerId, out FreezePlayerState? existing)
            && existing.Team == team
        )
        {
            _players.Remove(playerId);
            Teams.LeaveTeam(playerId);

            return FreezeGateResult.Left;
        }

        // Joining or switching: the target team must have room. Check before leaving the current team so
        // a rejected switch never strips the player of their existing membership.
        if (GetTeamCount(team) >= Settings.MaxPlayersPerTeam)
        {
            return FreezeGateResult.None;
        }

        _players.Remove(playerId); // leave the old team if switching
        _players[playerId] = new FreezePlayerState(playerId, team, Settings);
        Teams.JoinTeam(playerId, team);

        return FreezeGateResult.Joined;
    }

    /// <summary>Takes the player out of the game — they left the room, walked onto an exit tile, or were
    /// eliminated. They leave the shared team with it, so the wired team leaves stop counting someone who
    /// is no longer playing.</summary>
    public FreezePlayerState? Remove(PlayerId playerId)
    {
        if (_players.Remove(playerId, out FreezePlayerState? player))
        {
            Teams.LeaveTeam(playerId);

            return player;
        }

        return null;
    }

    /// <summary>Starts the round: everyone's loadout is reset. Returns <c>false</c> if already running.</summary>
    public bool Start()
    {
        if (Phase == FreezeGamePhase.Running)
        {
            return false;
        }

        // Last round's scores are cleared by RoomGameSystem.StartGameAsync, which always runs first and
        // does it before GAME_STARTS is published. Clearing them again here would land AFTER that event
        // and silently wipe whatever a GAME_STARTS-triggered score box just awarded.
        foreach (FreezePlayerState player in _players.Values)
        {
            player.Reset(Settings);
        }

        Phase = FreezeGamePhase.Running;

        return true;
    }

    /// <summary>Ends the round and returns the winning team (highest score, or None on a scoreless tie).</summary>
    public GameTeamColor Stop()
    {
        Phase = FreezeGamePhase.Idle;

        return GetWinningTeam();
    }

    public GameTeamColor GetWinningTeam() => Teams.GetLeadingTeam();
}

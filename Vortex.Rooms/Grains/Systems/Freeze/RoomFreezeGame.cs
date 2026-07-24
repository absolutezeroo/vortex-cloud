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
/// own <see cref="FreezePlayerState"/>), the game phase, and per-team score. No IO — every effect,
/// teleport, tile update and broadcast is done by <see cref="RoomFreezeSystem"/> from the signals this
/// returns. Kept unit-testable in isolation, matching <see cref="GameTeamState"/>.
/// </summary>
public sealed class RoomFreezeGame
{
    private readonly Dictionary<PlayerId, FreezePlayerState> _players = [];
    private readonly int[] _scoreByTeam = new int[5]; // indexed by (int)GameTeamColor; 0 unused

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

    public int GetTeamScore(GameTeamColor team) =>
        GameTeamState.IsRealTeam(team) ? _scoreByTeam[(int)team] : 0;

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

        return FreezeGateResult.Joined;
    }

    public FreezePlayerState? Remove(PlayerId playerId)
    {
        if (_players.Remove(playerId, out FreezePlayerState? player))
        {
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

        for (int i = 0; i < _scoreByTeam.Length; i++)
        {
            _scoreByTeam[i] = 0;
        }

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

    public GameTeamColor GetWinningTeam()
    {
        GameTeamColor best = GameTeamColor.None;
        int bestScore = 0;

        for (int team = 1; team < _scoreByTeam.Length; team++)
        {
            if (_scoreByTeam[team] > bestScore)
            {
                bestScore = _scoreByTeam[team];
                best = (GameTeamColor)team;
            }
        }

        return best;
    }

    public void AddTeamScore(GameTeamColor team, int amount)
    {
        if (GameTeamState.IsRealTeam(team))
        {
            _scoreByTeam[(int)team] = System.Math.Max(0, _scoreByTeam[(int)team] + amount);
        }
    }
}

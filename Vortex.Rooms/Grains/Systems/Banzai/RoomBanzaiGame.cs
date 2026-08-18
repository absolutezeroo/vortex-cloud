using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Games;

namespace Vortex.Rooms.Grains.Systems.Banzai;

/// <summary>Whether a Banzai game is accepting players (Idle) or in progress (Running).</summary>
public enum BanzaiGamePhase
{
    Idle = 0,
    Running = 1,
}

/// <summary>The outcome of a player touching a Banzai team gate.</summary>
public enum BanzaiGateResult
{
    None = 0,
    Joined = 1,
    Left = 2,
}

/// <summary>
/// The pure state and rules of a room's Battle Banzai game: the phase, the board, and the gate
/// rules. Unlike Freeze there is NO per-player state to carry (no lives, no ammo), so membership
/// lives ONLY in the room's shared <see cref="GameTeamState"/> (<see cref="Teams"/>) — which is
/// also why a player put on a team by a wired join-team box can claim tiles: the tile only asks
/// what team the walker is on. No IO; <see cref="RoomBanzaiSystem"/> broadcasts the changes.
/// </summary>
public sealed class RoomBanzaiGame
{
    /// <summary>The room's shared team + score store. Set once by <see cref="RoomBanzaiSystem"/> to
    /// the room's <see cref="RoomGameSystem"/> state.</summary>
    public GameTeamState Teams { get; init; } = new();

    public BanzaiSettings Settings { get; set; } = BanzaiSettings.Default;

    public BanzaiBoard Board { get; } = new();

    public BanzaiGamePhase Phase { get; private set; } = BanzaiGamePhase.Idle;

    public bool IsRunning => Phase == BanzaiGamePhase.Running;

    /// <summary>
    /// Toggles the player's membership of <paramref name="team"/> — walking onto your own team's
    /// gate again leaves it. Only allowed while idle (teams are picked before the round). The cap
    /// is checked before leaving the current team, so a rejected switch never strips membership.
    /// </summary>
    public BanzaiGateResult ToggleGate(PlayerId playerId, GameTeamColor team)
    {
        if (Phase != BanzaiGamePhase.Idle || !GameTeamState.IsRealTeam(team))
        {
            return BanzaiGateResult.None;
        }

        if (Teams.GetTeam(playerId) == team)
        {
            Teams.LeaveTeam(playerId);

            return BanzaiGateResult.Left;
        }

        if (Teams.GetTeamMemberCount(team) >= Settings.MaxPlayersPerTeam)
        {
            return BanzaiGateResult.None;
        }

        Teams.JoinTeam(playerId, team);

        return BanzaiGateResult.Joined;
    }

    /// <summary>Starts the round. Returns false when already running.</summary>
    public bool Start()
    {
        if (Phase == BanzaiGamePhase.Running)
        {
            return false;
        }

        Phase = BanzaiGamePhase.Running;

        return true;
    }

    /// <summary>Ends the round and returns the winning team (highest shared score, None on a
    /// scoreless tie).</summary>
    public GameTeamColor Stop()
    {
        Phase = BanzaiGamePhase.Idle;

        return Teams.GetLeadingTeam();
    }
}

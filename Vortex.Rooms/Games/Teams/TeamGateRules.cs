using Vortex.Primitives.Players;

namespace Vortex.Rooms.Games.Teams;

/// <summary>What stepping on a team gate did.</summary>
public enum TeamGateResult
{
    /// <summary>Nothing: the match is under way, the gate names a team this game does not play, or
    /// the team is full.</summary>
    None = 0,

    Joined = 1,

    Left = 2,
}

/// <summary>
/// The rules of a team gate, in one place because every gate-based game has exactly the same ones:
/// teams are picked before kick-off, stepping on your own team's gate again leaves it, and a full
/// team refuses.
/// <para>
/// Pure and shared — Banzai, Freeze and football all route their gates through here rather than each
/// carrying a copy that drifts.
/// </para>
/// </summary>
public static class TeamGateRules
{
    /// <summary>
    /// Toggles the player's membership of <paramref name="team"/>. The capacity check happens BEFORE
    /// leaving the current team, so a rejected switch never strips the membership the player had.
    /// </summary>
    /// <param name="acceptingPlayers">Whether the game is between matches. Gates are inert during
    /// one — a player cannot change sides mid-match.</param>
    public static TeamGateResult Toggle(
        TeamBook teams,
        TeamSet layout,
        PlayerId playerId,
        TeamId team,
        bool acceptingPlayers
    )
    {
        if (!acceptingPlayers || !layout.Contains(team) || !teams.Knows(team))
        {
            return TeamGateResult.None;
        }

        if (teams.GetTeam(playerId) == team)
        {
            teams.LeaveTeam(playerId);

            return TeamGateResult.Left;
        }

        int capacity = layout.CapacityOf(team);

        if (capacity > 0 && teams.GetTeamMemberCount(team) >= capacity)
        {
            return TeamGateResult.None;
        }

        teams.JoinTeam(playerId, team);

        return TeamGateResult.Joined;
    }
}

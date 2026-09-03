namespace Vortex.Rooms.Games.Teams;

/// <summary>
/// One team of one game: its identity, a stable name and how many players fit in it.
/// <para>
/// <see cref="Key"/> is a domain name, not a colour — "red" for a game whose teams really are the
/// Habbo colours, "hunters" for one whose teams are not. It is what <c>HabboTeamPalette</c> matches
/// on to decide which of the four Habbo colours (if any) can present this team, and what a log line
/// or a future analytics event names the team by. Keep it short, lowercase and stable.
/// </para>
/// </summary>
public sealed record GameTeam
{
    public required TeamId Id { get; init; }

    public required string Key { get; init; }

    /// <summary>Members allowed. 0 means unlimited.</summary>
    public int Capacity { get; init; }
}

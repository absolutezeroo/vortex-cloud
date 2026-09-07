namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The whole domain, not the window: guilds counted against the forum traffic they carry.
/// </summary>
/// <param name="AvgMembersPerGroup">Members over guilds, 0 when there are no guilds.</param>
public sealed record GroupTotals(
    int TotalGroups,
    int TotalMembers,
    int TotalThreads,
    int TotalPosts,
    double AvgMembersPerGroup
);

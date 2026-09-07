using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>What moderators did in the window, and the sanctions it left behind.</summary>
public sealed record ModerationStats(
    ModerationWindow Window,
    ModerationTotals Totals,
    ModerationDistribution Distribution,
    IReadOnlyList<ModerationTimelinePoint> Timeline,
    IReadOnlyList<ModerationActorCount> TopActors,
    IReadOnlyList<ModerationTargetCount> TopTargets,
    IReadOnlyList<ModerationRoomCount> TopRooms,
    IReadOnlyList<ModerationRow> Rows
);

using System.Collections.Generic;
using Vortex.Observability.Runtime;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The live traffic block on the overview.
/// </summary>
/// <remarks>
/// The same window the packets page reads, minus the per-operation breakdown: the overview shows
/// who is loudest, not which message is.
/// </remarks>
public sealed record OverviewLive(
    double PacketsPerSecond,
    double ErrorsPerMinute,
    double LatencyP50Ms,
    double LatencyP95Ms,
    IReadOnlyList<LiveAbuserSnapshot> TopAbusers,
    IReadOnlyList<LiveRoomSnapshot> TopRooms
);

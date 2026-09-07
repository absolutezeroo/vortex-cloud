using System.Collections.Generic;
using Vortex.Observability.Runtime;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// Live packet traffic, as the packets page reads it.
/// </summary>
/// <remarks>
/// The rates are rounded here rather than in the browser: they come off a sampling window and the
/// extra digits are noise, not precision, on every surface that shows them.
/// </remarks>
public sealed record PacketStats(
    double PacketsPerSecond,
    double ErrorsPerMinute,
    double LatencyP50Ms,
    double LatencyP95Ms,
    IReadOnlyList<LivePacketOperationSnapshot> TopOperations,
    IReadOnlyList<LivePacketOperationSnapshot> TopFailedOperations
);

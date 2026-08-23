using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Availability;

/// <summary>
/// Scheduled-maintenance warning (header 1737).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2152/_SafeCls_3162.as): a boolean, an int, and a
/// third int the client reads only if bytes remain. It defaults that field to 15 rather than 0, so
/// omitting it is a supported wire shape — this serializer always writes it, which is the same
/// stream the client's guarded read expects.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record MaintenanceStatusMessageComposer : IComposer
{
    [Id(0)]
    public required bool IsInMaintenance { get; init; }

    [Id(1)]
    public required int MinutesUntilMaintenance { get; init; }

    /// <summary>How long the maintenance is expected to last, in minutes. The client's default is
    /// 15 when the field is absent.</summary>
    [Id(2)]
    public int DurationMinutes { get; init; } = 15;
}

using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>
/// A room's wired permission masks and timezone, as the wired menu reads and writes them. Returned
/// by the grain in place of the composer it used to build itself — see
/// <see cref="WiredRoomStatsSnapshot"/> for why.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record WiredRoomSettingsSnapshot
{
    [Id(0)]
    public required int ModifyPermissionMask { get; init; }

    [Id(1)]
    public required int ReadPermissionMask { get; init; }

    [Id(2)]
    public required string Timezone { get; init; }
}

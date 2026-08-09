using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Primitives.Orleans.Snapshots.Navigator;

/// <summary>
/// A promoted-rooms group in the official rooms view (the client localizes
/// <c>promotedroomcategory.&lt;Code&gt;</c> for its title).
/// </summary>
/// <remarks>
/// The client reads the first room unconditionally and only then loops to <c>count</c>, so a group
/// with an empty <see cref="Rooms"/> would leave it reading a room block that was never written and
/// desynchronize the rest of the packet. Never emit a group without at least one room.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record PromotedRoomCategorySnapshot
{
    [Id(0)]
    public required string Code { get; init; }

    [Id(1)]
    public required string LeaderFigure { get; init; }

    [Id(2)]
    public required ImmutableArray<RoomInfoSnapshot> Rooms { get; init; }
}

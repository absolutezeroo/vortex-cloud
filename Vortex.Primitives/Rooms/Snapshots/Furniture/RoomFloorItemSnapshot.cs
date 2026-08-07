using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Furniture;

[GenerateSerializer, Immutable]
public sealed record RoomFloorItemSnapshot : RoomItemSnapshot
{
    /// <summary>
    /// The <c>extra</c> field of the floor item on the wire. Not required: it is zero for all but a
    /// handful of furniture families, and every existing caller means zero.
    /// </summary>
    [Id(0)]
    public int Extra { get; init; }
}

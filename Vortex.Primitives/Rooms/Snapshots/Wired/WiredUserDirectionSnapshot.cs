using Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

[GenerateSerializer, Immutable]
public sealed record WiredUserDirectionSnapshot
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required Rotation BodyRotation { get; init; }

    [Id(2)]
    public required Rotation HeadRotation { get; init; }
}

using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Room.Action;

[GenerateSerializer, Immutable]
public sealed record ExpressionMessageComposer : IComposer
{
    [Id(0)]
    public required RoomObjectId ObjectId { get; init; }

    [Id(1)]
    public required AvatarExpressionType ExpressionType { get; init; }
}

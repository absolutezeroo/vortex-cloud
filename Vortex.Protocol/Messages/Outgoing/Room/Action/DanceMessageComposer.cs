using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Room.Action;

[GenerateSerializer, Immutable]
public sealed record DanceMessageComposer : IComposer
{
    [Id(0)]
    public required int ObjectId { get; init; }

    [Id(1)]
    public required AvatarDanceType DanceType { get; init; }
}

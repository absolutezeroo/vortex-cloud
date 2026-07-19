using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record RoomInviteMessageComposer : IComposer
{
    [Id(0)]
    public required int SenderId { get; init; }

    [Id(1)]
    public required string Message { get; init; }
}

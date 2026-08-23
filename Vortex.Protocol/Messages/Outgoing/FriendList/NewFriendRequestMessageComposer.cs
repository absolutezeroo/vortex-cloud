using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record NewFriendRequestMessageComposer : IComposer
{
    [Id(0)]
    public required FriendRequestSnapshot Request { get; init; }
}

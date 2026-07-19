using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record FriendRequestsMessageComposer : IComposer
{
    [Id(0)]
    public required List<FriendRequestSnapshot> Requests { get; init; }
}

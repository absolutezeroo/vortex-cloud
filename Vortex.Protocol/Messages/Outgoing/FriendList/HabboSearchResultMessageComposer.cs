using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record HabboSearchResultMessageComposer : IComposer
{
    [Id(0)]
    public required List<MessengerSearchResultSnapshot> Friends { get; init; }

    [Id(1)]
    public required List<MessengerSearchResultSnapshot> Others { get; init; }
}

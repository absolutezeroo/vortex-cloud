using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record FriendListFragmentMessageComposer : IComposer
{
    [Id(0)]
    public required int TotalFragments { get; init; }

    [Id(1)]
    public required int FragmentIndex { get; init; }

    [Id(2)]
    public required List<MessengerFriendSnapshot> Fragment { get; init; }
}

using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record MessengerInitMessageComposer : IComposer
{
    [Id(0)]
    public required int UserFriendLimit { get; init; }

    [Id(1)]
    public required int NormalFriendLimit { get; init; }

    [Id(2)]
    public required int ExtendedFriendLimit { get; init; }

    [Id(3)]
    public required List<FriendCategorySnapshot> FriendCategories { get; init; }
}

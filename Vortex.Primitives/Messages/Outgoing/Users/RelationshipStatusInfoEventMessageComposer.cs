using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record RelationshipStatusInfoEventMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required List<RelationshipStatusEntrySnapshot> Relations { get; init; }
}

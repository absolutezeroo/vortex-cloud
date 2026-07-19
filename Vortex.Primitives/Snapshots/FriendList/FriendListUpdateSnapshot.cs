using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Snapshots.FriendList;

[GenerateSerializer, Immutable]
public record FriendListUpdateSnapshot
{
    [Id(0)]
    public required FriendListUpdateActionType ActionType { get; init; }

    [Id(1)]
    public PlayerId FriendId { get; init; }

    [Id(2)]
    public MessengerFriendSnapshot? Friend { get; init; }
}

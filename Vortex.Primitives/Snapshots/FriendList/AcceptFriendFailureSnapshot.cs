using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Snapshots.FriendList;

[GenerateSerializer, Immutable]
public record AcceptFriendFailureSnapshot
{
    [Id(0)]
    public required PlayerId SenderId { get; init; }

    [Id(1)]
    public required FriendListErrorCodeType ErrorCode { get; init; }
}

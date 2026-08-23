using Orleans;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record FriendNotificationMessageComposer : IComposer
{
    [Id(0)]
    public required string AvatarId { get; init; }

    [Id(1)]
    public required FriendNotificationCodeType TypeCode { get; init; }

    [Id(2)]
    public required string Message { get; init; }
}

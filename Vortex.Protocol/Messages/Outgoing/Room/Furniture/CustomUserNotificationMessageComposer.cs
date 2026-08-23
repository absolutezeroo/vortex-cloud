using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Protocol.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record CustomUserNotificationMessageComposer : IComposer
{
    [Id(0)]
    public required CustomUserNotificationType Code { get; init; }
}

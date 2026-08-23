using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

/// <summary>Broadcast when a player receives respect: the target's id and their new respect total.</summary>
[GenerateSerializer, Immutable]
public sealed record RespectNotificationMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required int RespectTotal { get; init; }
}

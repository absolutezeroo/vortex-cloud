using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildCreatedMessageComposer : IComposer
{
    [Id(0)]
    public required int BaseRoomId { get; init; }

    [Id(1)]
    public required int GroupId { get; init; }
}

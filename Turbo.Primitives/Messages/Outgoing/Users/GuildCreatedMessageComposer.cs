using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildCreatedMessageComposer : IComposer
{
    [Id(0)]
    public required int BaseRoomId { get; init; }

    [Id(1)]
    public required int GroupId { get; init; }
}

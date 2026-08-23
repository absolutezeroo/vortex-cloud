using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildMemberMgmtFailedMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required int Reason { get; init; }
}

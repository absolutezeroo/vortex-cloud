using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildEditInfoMessageComposer : IComposer
{
    [Id(0)]
    public required GroupEditInfoSnapshot Info { get; init; }
}

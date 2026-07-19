using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GuildEditFailedMessageComposer : IComposer
{
    [Id(0)]
    public required int Reason { get; init; }
}

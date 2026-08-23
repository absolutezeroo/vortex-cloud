using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record UserBannedMessageComposer : IComposer
{
    [Id(0)]
    public required string Message { get; init; }
}

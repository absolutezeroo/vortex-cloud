using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

[GenerateSerializer, Immutable]
public sealed record ModeratorMessageComposer : IComposer
{
    [Id(0)]
    public required string Message { get; init; }

    [Id(1)]
    public string Url { get; init; } = string.Empty;
}

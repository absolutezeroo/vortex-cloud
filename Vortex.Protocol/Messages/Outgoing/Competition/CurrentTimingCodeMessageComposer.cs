using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Competition;

[GenerateSerializer, Immutable]
public sealed record CurrentTimingCodeMessageComposer : IComposer
{
    [Id(0)]
    public required string SlotConfig { get; init; }

    [Id(1)]
    public required string Code { get; init; }
}

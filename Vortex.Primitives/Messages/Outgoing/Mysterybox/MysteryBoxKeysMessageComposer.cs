using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Mysterybox;

[GenerateSerializer, Immutable]
public sealed record MysteryBoxKeysMessageComposer : IComposer
{
    [Id(0)]
    public required string BoxColor { get; init; }

    [Id(1)]
    public required string KeyColor { get; init; }
}

using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record SilverBalanceMessageComposer : IComposer
{
    [Id(0)]
    public required int SilverBalance { get; init; }
}

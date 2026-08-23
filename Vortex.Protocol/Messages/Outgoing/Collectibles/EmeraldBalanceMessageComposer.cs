using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record EmeraldBalanceMessageComposer : IComposer
{
    [Id(0)]
    public required int EmeraldBalance { get; init; }
}

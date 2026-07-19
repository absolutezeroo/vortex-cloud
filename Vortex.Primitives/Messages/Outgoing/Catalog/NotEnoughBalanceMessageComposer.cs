using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record NotEnoughBalanceMessageComposer : IComposer
{
    [Id(0)]
    public required bool NotEnoughCredits { get; init; }

    [Id(1)]
    public required bool NotEnoughActivityPoints { get; init; }

    [Id(2)]
    public required int ActivityPointType { get; init; }
}

using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record IsOfferGiftableEventMessageComposer : IComposer
{
    [Id(0)]
    public required int OfferId { get; init; }

    [Id(1)]
    public required bool IsGiftable { get; init; }
}

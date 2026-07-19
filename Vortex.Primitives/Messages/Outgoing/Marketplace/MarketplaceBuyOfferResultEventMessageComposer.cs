using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceBuyOfferResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Result { get; init; }

    [Id(1)]
    public required int OfferId { get; init; }

    [Id(2)]
    public required int NewPrice { get; init; }

    [Id(3)]
    public required int OldOfferId { get; init; }
}

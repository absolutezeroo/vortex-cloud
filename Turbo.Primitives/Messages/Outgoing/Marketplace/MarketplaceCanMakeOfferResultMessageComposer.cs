using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceCanMakeOfferResultMessageComposer : IComposer
{
    [Id(0)]
    public required bool CanMakeOffer { get; init; }
}

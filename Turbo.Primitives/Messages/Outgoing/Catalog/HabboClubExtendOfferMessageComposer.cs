using Orleans;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record HabboClubExtendOfferMessageComposer : IComposer
{
    [Id(0)]
    public required ClubOffer Offer { get; init; }

    [Id(1)]
    public int OriginalPricePerMonth { get; init; }

    [Id(2)]
    public int OriginalActivityPointPricePerMonth { get; init; }

    [Id(3)]
    public int OriginalActivityPointType { get; init; }

    [Id(4)]
    public int SubscriptionDaysLeft { get; init; }
}

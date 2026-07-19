using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record HabboClubOffersMessageComposer : IComposer
{
    [Id(0)]
    public required IReadOnlyList<ClubOffer> Offers { get; init; }

    [Id(1)]
    public required ClubOfferRequestSourceType Source { get; init; }
}

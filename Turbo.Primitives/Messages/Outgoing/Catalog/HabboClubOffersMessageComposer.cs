using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record HabboClubOffersMessageComposer : IComposer
{
    [Id(0)] public required IReadOnlyList<ClubOffer> Offers { get; init; }
    [Id(1)] public required ClubOfferRequestSourceType Source { get; init; }
}

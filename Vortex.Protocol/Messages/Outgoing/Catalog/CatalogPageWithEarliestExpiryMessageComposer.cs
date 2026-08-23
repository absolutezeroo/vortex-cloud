using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record CatalogPageWithEarliestExpiryMessageComposer : IComposer
{
    [Id(0)]
    public required string PageName { get; init; }

    [Id(1)]
    public required int SecondsToExpiry { get; init; }

    [Id(2)]
    public required string Image { get; init; }
}

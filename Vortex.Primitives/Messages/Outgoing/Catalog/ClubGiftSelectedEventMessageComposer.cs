using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record ClubGiftSelectedEventMessageComposer : IComposer
{
    [Id(0)]
    public required string ProductCode { get; init; }

    [Id(1)]
    public required IReadOnlyList<CatalogProductSnapshot> Products { get; init; }
}

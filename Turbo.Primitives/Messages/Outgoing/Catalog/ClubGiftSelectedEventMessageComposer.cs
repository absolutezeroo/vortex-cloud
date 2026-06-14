using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record ClubGiftSelectedEventMessageComposer : IComposer
{
    [Id(0)]
    public required string ProductCode { get; init; }

    [Id(1)]
    public required IReadOnlyList<CatalogProductSnapshot> Products { get; init; }
}

using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record CatalogIndexMessageComposer : IComposer
{
    [Id(0)]
    public required CatalogSnapshot Catalog { get; init; }

    [Id(1)]
    public bool NewAdditionsAvailable { get; init; }
}

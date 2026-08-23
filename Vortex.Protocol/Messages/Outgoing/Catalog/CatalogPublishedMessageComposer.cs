using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

/// <summary>
/// The catalogue has been republished (header 773).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1714/_SafeCls_3506.as): a boolean, then a string
/// the client reads only if bytes remain. Writing it always is the longer of the two shapes the
/// parser accepts.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record CatalogPublishedMessageComposer : IComposer
{
    /// <summary>Whether the open catalogue should reload immediately rather than on next open.</summary>
    [Id(0)]
    public required bool InstantlyRefreshCatalogue { get; init; }

    /// <summary>Hash of the new furnidata, so the client can tell whether its copy is stale.</summary>
    [Id(1)]
    public string NewFurniDataHash { get; init; } = string.Empty;
}

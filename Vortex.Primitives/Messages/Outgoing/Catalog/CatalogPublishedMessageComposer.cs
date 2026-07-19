using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record CatalogPublishedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}

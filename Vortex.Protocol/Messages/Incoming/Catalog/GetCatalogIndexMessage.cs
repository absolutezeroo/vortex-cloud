using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record GetCatalogIndexMessage : IMessageEvent
{
    public required CatalogType CatalogType { get; init; }
}

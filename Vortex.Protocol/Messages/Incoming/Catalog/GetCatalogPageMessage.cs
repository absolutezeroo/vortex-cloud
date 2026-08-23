using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record GetCatalogPageMessage : IMessageEvent
{
    public int PageId { get; init; }
    public int OfferId { get; init; }
    public required CatalogType CatalogType { get; init; }
}

using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class GetCatalogIndexMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetCatalogIndexMessage
        {
            CatalogType = CatalogTypeExtensions.FromLegacyString(packet.PopString()),
        };
}

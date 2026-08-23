using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class CatalogPageWithEarliestExpiryMessageComposerSerializer(int header)
    : AbstractSerializer<CatalogPageWithEarliestExpiryMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CatalogPageWithEarliestExpiryMessageComposer message
    )
    {
        packet
            .WriteString(message.PageName)
            .WriteInteger(message.SecondsToExpiry)
            .WriteString(message.Image);
    }
}

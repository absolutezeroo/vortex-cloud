using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class CatalogPublishedMessageComposerSerializer(int header)
    : AbstractSerializer<CatalogPublishedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CatalogPublishedMessageComposer message)
    {
        //
    }
}

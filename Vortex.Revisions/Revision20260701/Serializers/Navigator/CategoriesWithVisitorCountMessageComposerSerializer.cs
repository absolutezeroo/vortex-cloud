using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class CategoriesWithVisitorCountMessageComposerSerializer(int header)
    : AbstractSerializer<CategoriesWithVisitorCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CategoriesWithVisitorCountMessageComposer message
    )
    {
        CategoriesWithVisitorCountSerializer.Serialize(packet, message.Categories);
    }
}

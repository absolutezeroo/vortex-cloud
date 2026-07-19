using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class LimitedEditionSoldOutEventMessageComposerSerializer(int header)
    : AbstractSerializer<LimitedEditionSoldOutEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        LimitedEditionSoldOutEventMessageComposer message
    )
    {
        //
    }
}

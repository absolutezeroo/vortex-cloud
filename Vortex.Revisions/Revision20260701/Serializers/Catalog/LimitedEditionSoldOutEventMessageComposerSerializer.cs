using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

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

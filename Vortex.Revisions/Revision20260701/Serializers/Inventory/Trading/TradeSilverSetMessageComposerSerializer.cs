using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradeSilverSetMessageComposerSerializer(int header)
    : AbstractSerializer<TradeSilverSetMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TradeSilverSetMessageComposer message)
    {
        //
    }
}

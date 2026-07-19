using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingCloseEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingCloseEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradingCloseEventMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.Reason);
    }
}

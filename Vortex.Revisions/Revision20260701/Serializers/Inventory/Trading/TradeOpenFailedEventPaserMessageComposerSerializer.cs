using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradeOpenFailedEventPaserMessageComposerSerializer(int header)
    : AbstractSerializer<TradeOpenFailedEventPaserMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradeOpenFailedEventPaserMessageComposer message
    )
    {
        packet.WriteInteger(message.Reason).WriteString(message.OtherUserName);
    }
}

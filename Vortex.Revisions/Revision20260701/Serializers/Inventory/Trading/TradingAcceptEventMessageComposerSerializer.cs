using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingAcceptEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingAcceptEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradingAcceptEventMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.UserAccepts ? 1 : 0);
    }
}

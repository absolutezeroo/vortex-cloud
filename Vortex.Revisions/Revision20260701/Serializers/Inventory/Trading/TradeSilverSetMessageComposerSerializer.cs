using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Trading;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradeSilverSetMessageComposerSerializer(int header)
    : AbstractSerializer<TradeSilverSetMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TradeSilverSetMessageComposer message)
    {
        packet.WriteInteger(message.PlayerSilver).WriteInteger(message.OtherPlayerSilver);
    }
}

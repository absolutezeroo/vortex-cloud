using Turbo.Primitives.Messages.Outgoing.Inventory.Trading;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingOpenEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingOpenEventMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, TradingOpenEventMessageComposer message)
    {
        packet
            .WriteInteger(message.UserId)
            .WriteInteger(message.UserCanTrade ? 1 : 0)
            .WriteInteger(message.OtherUserId)
            .WriteInteger(message.OtherUserCanTrade ? 1 : 0);
    }
}

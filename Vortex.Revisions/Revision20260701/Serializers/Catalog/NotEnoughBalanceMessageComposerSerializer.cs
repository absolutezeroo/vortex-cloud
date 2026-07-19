using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class NotEnoughBalanceMessageComposerSerializer(int header)
    : AbstractSerializer<NotEnoughBalanceMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, NotEnoughBalanceMessageComposer message)
    {
        packet.WriteBoolean(message.NotEnoughCredits);
        packet.WriteBoolean(message.NotEnoughActivityPoints);
        packet.WriteInteger(message.ActivityPointType);
    }
}

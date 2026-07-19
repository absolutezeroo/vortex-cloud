using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class BonusRareInfoMessageComposerSerializer(int header)
    : AbstractSerializer<BonusRareInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BonusRareInfoMessageComposer message)
    {
        packet.WriteString(message.ProductType);
        packet.WriteInteger(message.ProductClassId);
        packet.WriteInteger(message.TotalCoinsForBonus);
        packet.WriteInteger(message.CoinsStillRequiredToBuy);
    }
}

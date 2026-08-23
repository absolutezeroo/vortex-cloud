using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Purse;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Purse;

internal class CreditBalanceEventMessageComposerSerializer(int header)
    : AbstractSerializer<CreditBalanceEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CreditBalanceEventMessageComposer message
    )
    {
        packet.WriteString(message.Balance);
    }
}

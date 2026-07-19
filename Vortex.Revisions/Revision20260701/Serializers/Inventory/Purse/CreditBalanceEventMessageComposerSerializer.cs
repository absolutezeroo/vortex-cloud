using Vortex.Primitives.Messages.Outgoing.Inventory.Purse;
using Vortex.Primitives.Packets;

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

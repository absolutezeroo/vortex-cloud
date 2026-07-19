using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Badges;

internal class IsBadgeRequestFulfilledEventMessageComposerSerializer(int header)
    : AbstractSerializer<IsBadgeRequestFulfilledEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        IsBadgeRequestFulfilledEventMessageComposer message
    )
    {
        packet.WriteString(message.RequestCode).WriteBoolean(message.Fulfilled);
    }
}

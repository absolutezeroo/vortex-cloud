using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Badges;

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

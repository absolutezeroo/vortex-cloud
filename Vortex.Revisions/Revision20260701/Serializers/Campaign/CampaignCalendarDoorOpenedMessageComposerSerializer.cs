using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Campaign;

namespace Vortex.Revisions.Revision20260701.Serializers.Campaign;

internal class CampaignCalendarDoorOpenedMessageComposerSerializer(int header)
    : AbstractSerializer<CampaignCalendarDoorOpenedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CampaignCalendarDoorOpenedMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.DoorOpened)
            .WriteString(message.ProductName)
            .WriteString(message.CustomImage)
            .WriteString(message.FurnitureClassName);
    }
}

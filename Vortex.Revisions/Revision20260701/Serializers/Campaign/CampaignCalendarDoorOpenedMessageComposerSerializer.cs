using Vortex.Primitives.Messages.Outgoing.Campaign;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Campaign;

internal class CampaignCalendarDoorOpenedMessageComposerSerializer(int header)
    : AbstractSerializer<CampaignCalendarDoorOpenedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CampaignCalendarDoorOpenedMessageComposer message
    )
    {
        //
    }
}

using Vortex.Primitives.Messages.Outgoing.Campaign;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Campaign;

internal class CampaignCalendarDataMessageComposerSerializer(int header)
    : AbstractSerializer<CampaignCalendarDataMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CampaignCalendarDataMessageComposer message
    )
    {
        //
    }
}

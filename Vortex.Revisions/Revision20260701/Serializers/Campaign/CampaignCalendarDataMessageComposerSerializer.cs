using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Campaign;

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

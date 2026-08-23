using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Campaign;
using Vortex.Revisions.Revision20260701.Parsers.Campaign;
using Vortex.Revisions.Revision20260701.Serializers.Campaign;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class CampaignMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.OpenCampaignCalendarDoorAsStaffEvent,
            new OpenCampaignCalendarDoorAsStaffMessageParser()
        );
        builder.MapParser(
            MessageEvent.OpenCampaignCalendarDoorEvent,
            new OpenCampaignCalendarDoorMessageParser()
        );

        builder.MapSerializer(
            typeof(CampaignCalendarDataMessageComposer),
            new CampaignCalendarDataMessageComposerSerializer(
                MessageComposer.CampaignCalendarDataMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(CampaignCalendarDoorOpenedMessageComposer),
            // Was registered against CampaignCalendarDataMessageComposerSerializer, which throws an
            // InvalidCastException the moment anything sends this message. It went unnoticed while
            // both Serialize bodies were empty, because neither reached the cast.
            new CampaignCalendarDoorOpenedMessageComposerSerializer(
                MessageComposer.CampaignCalendarDoorOpenedMessageComposer
            )
        );
    }
}

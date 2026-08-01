using Vortex.Primitives.Messages.Outgoing.Campaign;
using Vortex.Primitives.Networking.Revisions;
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
            new CampaignCalendarDataMessageComposerSerializer(
                MessageComposer.CampaignCalendarDoorOpenedMessageComposer
            )
        );
    }
}

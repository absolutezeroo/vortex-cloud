using Vortex.Primitives.Messages.Outgoing.Availability;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Serializers.Availability;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class AvailabilityMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapSerializer(
            typeof(AvailabilityStatusMessageComposer),
            new AvailabilityStatusMessageComposerSerializer(
                MessageComposer.AvailabilityStatusMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(InfoHotelClosedMessageComposer),
            new InfoHotelClosedMessageComposerSerializer(
                MessageComposer.InfoHotelClosedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(InfoHotelClosingMessageComposer),
            new InfoHotelClosingMessageComposerSerializer(
                MessageComposer.InfoHotelClosingMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(LoginFailedHotelClosedMessageComposer),
            new LoginFailedHotelClosedMessageComposerSerializer(
                MessageComposer.LoginFailedHotelClosedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(MaintenanceStatusMessageComposer),
            new MaintenanceStatusMessageComposerSerializer(
                MessageComposer.MaintenanceStatusMessageComposer
            )
        );
    }
}

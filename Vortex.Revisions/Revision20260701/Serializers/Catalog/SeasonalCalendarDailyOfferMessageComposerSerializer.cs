using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class SeasonalCalendarDailyOfferMessageComposerSerializer(int header)
    : AbstractSerializer<SeasonalCalendarDailyOfferMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SeasonalCalendarDailyOfferMessageComposer message
    )
    {
        //
    }
}

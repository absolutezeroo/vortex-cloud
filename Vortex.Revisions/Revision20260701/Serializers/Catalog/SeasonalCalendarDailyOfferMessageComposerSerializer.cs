using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

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

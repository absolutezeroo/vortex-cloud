using Vortex.Primitives.Messages.Outgoing.Inventory.Trading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingYouAreNotAllowedEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingYouAreNotAllowedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradingYouAreNotAllowedEventMessageComposer message
    )
    {
        //
    }
}

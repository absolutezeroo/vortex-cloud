using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Trading;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Trading;

internal class TradingCompletedEventMessageComposerSerializer(int header)
    : AbstractSerializer<TradingCompletedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        TradingCompletedEventMessageComposer message
    )
    {
        //
    }
}

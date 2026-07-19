using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class SilverBalanceMessageComposerSerializer(int header)
    : AbstractSerializer<SilverBalanceMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, SilverBalanceMessageComposer message)
    {
        packet.WriteInteger(message.SilverBalance);
    }
}

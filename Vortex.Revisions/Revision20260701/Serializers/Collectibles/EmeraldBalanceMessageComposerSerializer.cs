using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class EmeraldBalanceMessageComposerSerializer(int header)
    : AbstractSerializer<EmeraldBalanceMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, EmeraldBalanceMessageComposer message)
    {
        packet.WriteInteger(message.EmeraldBalance);
    }
}

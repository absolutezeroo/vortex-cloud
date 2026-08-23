using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class EmeraldBalanceMessageComposerSerializer(int header)
    : AbstractSerializer<EmeraldBalanceMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, EmeraldBalanceMessageComposer message)
    {
        packet.WriteInteger(message.EmeraldBalance);
    }
}

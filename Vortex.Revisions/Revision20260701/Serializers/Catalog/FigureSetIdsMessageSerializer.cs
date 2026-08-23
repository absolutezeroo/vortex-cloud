using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class FigureSetIdsMessageSerializer(int header)
    : AbstractSerializer<FigureSetIdsMessage>(header)
{
    protected override void Serialize(IServerPacket packet, FigureSetIdsMessage message)
    {
        packet.WriteInteger(0); //length

        packet.WriteInteger(0); //length
    }
}

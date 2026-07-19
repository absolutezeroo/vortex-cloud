using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

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

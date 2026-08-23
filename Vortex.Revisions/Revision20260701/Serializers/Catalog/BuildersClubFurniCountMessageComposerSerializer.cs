using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class BuildersClubFurniCountMessageComposerSerializer(int header)
    : AbstractSerializer<BuildersClubFurniCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        BuildersClubFurniCountMessageComposer message
    )
    {
        packet.WriteInteger(message.FurniCount);
    }
}

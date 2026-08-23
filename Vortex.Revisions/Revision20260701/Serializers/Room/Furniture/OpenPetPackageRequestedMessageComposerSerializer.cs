using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class OpenPetPackageRequestedMessageComposerSerializer(int header)
    : AbstractSerializer<OpenPetPackageRequestedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        OpenPetPackageRequestedMessageComposer message
    )
    {
        packet.WriteInteger(message.ObjectId);

        PetFigureDataSerializer.Serialize(packet, message.Pet);
    }
}

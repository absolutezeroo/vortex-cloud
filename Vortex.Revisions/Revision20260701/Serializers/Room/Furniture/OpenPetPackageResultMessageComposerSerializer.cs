using Vortex.Primitives.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class OpenPetPackageResultMessageComposerSerializer(int header)
    : AbstractSerializer<OpenPetPackageResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        OpenPetPackageResultMessageComposer message
    )
    {
        packet
            .WriteInteger(message.ObjectId)
            .WriteInteger(message.NameValidationStatus)
            .WriteString(message.NameValidationInfo);
    }
}

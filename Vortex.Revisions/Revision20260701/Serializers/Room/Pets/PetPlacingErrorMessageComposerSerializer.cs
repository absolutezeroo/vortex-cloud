using Vortex.Primitives.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetPlacingErrorMessageComposerSerializer(int header)
    : AbstractSerializer<PetPlacingErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetPlacingErrorMessageComposer message)
    {
        packet.WriteInteger(message.ErrorCode);
    }
}

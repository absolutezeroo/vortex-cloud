using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetPlacingErrorMessageComposerSerializer(int header)
    : AbstractSerializer<PetPlacingErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetPlacingErrorMessageComposer message)
    {
        packet.WriteInteger(message.ErrorCode);
    }
}

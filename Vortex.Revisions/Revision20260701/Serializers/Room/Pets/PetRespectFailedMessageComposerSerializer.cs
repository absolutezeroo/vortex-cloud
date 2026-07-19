using Vortex.Primitives.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetRespectFailedMessageComposerSerializer(int header)
    : AbstractSerializer<PetRespectFailedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetRespectFailedMessageComposer message)
    {
        //
    }
}

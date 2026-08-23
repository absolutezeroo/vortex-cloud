using Vortex.Protocol.Messages.Outgoing.Room.Pets;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetRespectFailedMessageComposerSerializer(int header)
    : AbstractSerializer<PetRespectFailedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetRespectFailedMessageComposer message)
    {
        packet.WriteInteger(message.RequiredDays).WriteInteger(message.AvatarAgeInDays);
    }
}

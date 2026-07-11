using Turbo.Primitives.Messages.Outgoing.Room.Pets;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetVocalMessageComposerSerializer(int header)
    : AbstractSerializer<PetVocalMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PetVocalMessageComposer message)
    {
        packet
            .WriteInteger(message.PetObjectId)
            .WriteInteger(message.PetType)
            .WriteString(message.VocalType)
            .WriteInteger(message.VocalIndex);
    }
}

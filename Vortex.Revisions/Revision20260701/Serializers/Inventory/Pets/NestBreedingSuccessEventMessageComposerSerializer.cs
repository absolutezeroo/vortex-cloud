using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class NestBreedingSuccessEventMessageComposerSerializer(int header)
    : AbstractSerializer<NestBreedingSuccessEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NestBreedingSuccessEventMessageComposer message
    )
    {
        packet.WriteInteger(message.NewPetId);
    }
}

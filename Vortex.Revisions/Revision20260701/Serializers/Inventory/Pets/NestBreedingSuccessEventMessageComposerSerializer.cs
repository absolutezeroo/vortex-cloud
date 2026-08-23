using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class NestBreedingSuccessEventMessageComposerSerializer(int header)
    : AbstractSerializer<NestBreedingSuccessEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NestBreedingSuccessEventMessageComposer message
    )
    {
        packet.WriteInteger(message.NewPetId).WriteInteger(message.RarityCategory);
    }
}

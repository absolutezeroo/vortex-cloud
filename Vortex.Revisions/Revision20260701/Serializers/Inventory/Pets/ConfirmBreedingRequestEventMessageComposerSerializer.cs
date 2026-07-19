using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class ConfirmBreedingRequestEventMessageComposerSerializer(int header)
    : AbstractSerializer<ConfirmBreedingRequestEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConfirmBreedingRequestEventMessageComposer message
    )
    {
        packet
            .WriteInteger(message.PetOneId)
            .WriteInteger(message.PetTwoId)
            .WriteInteger(message.OwnerOneId)
            .WriteInteger(message.OwnerTwoId)
            .WriteInteger(message.ProposedRace)
            .WriteString(message.ProposedColor)
            .WriteInteger(message.ProposedGender);
    }
}

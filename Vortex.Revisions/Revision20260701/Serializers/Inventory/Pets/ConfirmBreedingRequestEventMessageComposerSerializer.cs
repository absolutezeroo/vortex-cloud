using Vortex.Primitives.Packets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets;

internal class ConfirmBreedingRequestEventMessageComposerSerializer(int header)
    : AbstractSerializer<ConfirmBreedingRequestEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConfirmBreedingRequestEventMessageComposer message
    )
    {
        packet.WriteInteger(message.NestId);

        Write(packet, message.PetOne);
        Write(packet, message.PetTwo);

        // The rarity-odds chart, empty: nothing here computes breeding odds.
        packet.WriteInteger(0);

        packet.WriteInteger(message.ResultPetType);
    }

    private static void Write(IServerPacket packet, PetBreedingParentSnapshot parent) =>
        packet
            .WriteInteger(parent.WebId)
            .WriteString(parent.Name)
            .WriteInteger(parent.Level)
            .WriteString(parent.Figure)
            .WriteString(parent.Owner);
}

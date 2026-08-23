using Vortex.Primitives.Packets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Room.Pets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Pets;

internal class PetBreedingResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<PetBreedingResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PetBreedingResultEventMessageComposer message
    )
    {
        Write(packet, message.Result);
        Write(packet, message.OtherResult);
    }

    private static void Write(IServerPacket packet, PetBreedingOutcomeSnapshot outcome) =>
        packet
            .WriteInteger(outcome.StuffId)
            .WriteInteger(outcome.ClassId)
            .WriteString(outcome.ProductCode)
            .WriteInteger(outcome.UserId)
            .WriteString(outcome.UserName)
            .WriteInteger(outcome.RarityLevel)
            .WriteBoolean(outcome.HasMutation);
}

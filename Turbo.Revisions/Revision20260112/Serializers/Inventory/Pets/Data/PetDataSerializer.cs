using Turbo.Primitives.Packets;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Revisions.Revision20260112.Serializers.Inventory.Pets.Data;

internal static class PetDataSerializer
{
    public static void Serialize(IServerPacket packet, PetSnapshot pet)
    {
        packet
            .WriteInteger(pet.PetId)
            .WriteString(pet.Name)
            .WriteInteger(pet.Type)
            .WriteInteger(pet.Race)
            .WriteString(pet.Color)
            .WriteInteger(pet.Level)
            .WriteInteger(pet.Experience)
            .WriteInteger(pet.Energy)
            .WriteInteger(pet.Nutrition)
            .WriteInteger(pet.Respect)
            .WriteBoolean(false) // hasSaddle
            .WriteBoolean(false) // isRiding
            .WriteBoolean(false) // canBreed
            .WriteBoolean(false) // canHarvest
            .WriteBoolean(false) // canRevive
            .WriteBoolean(false) // hasRareColor
            .WriteInteger(0)
            .WriteInteger(0)
            .WriteInteger(0)
            .WriteInteger(0);
    }
}

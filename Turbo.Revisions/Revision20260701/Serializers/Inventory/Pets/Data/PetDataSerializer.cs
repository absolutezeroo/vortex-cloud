using Turbo.Primitives.Packets;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Inventory.Pets.Data;

internal static class PetDataSerializer
{
    public static void Serialize(IServerPacket packet, PetSnapshot pet)
    {
        packet
            .WriteInteger(pet.PetId)
            .WriteString(pet.Name)
            // PetFigureData
            .WriteInteger(pet.Type)
            .WriteInteger(pet.Race)
            .WriteString(pet.Color)
            .WriteInteger(pet.Race)
            .WriteInteger(0)
            // end PetFigureData
            .WriteInteger(pet.Level);
    }
}

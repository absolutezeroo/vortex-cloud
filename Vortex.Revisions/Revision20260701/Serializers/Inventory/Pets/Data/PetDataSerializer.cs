using Vortex.Primitives.Packets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Revisions.Revision20260701.Serializers.Room.Pets.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Pets.Data;

internal static class PetDataSerializer
{
    public static void Serialize(IServerPacket packet, PetSnapshot pet)
    {
        packet.WriteInteger(pet.PetId).WriteString(pet.Name);

        PetFigureDataSerializer.Serialize(packet, pet);

        // Rarity ranks seasonal and bred pets. Nothing here ranks them, so every pet reads as the
        // common tier -- but the field has to be written: the client reads it unconditionally, and
        // in a list that shift corrupted every pet after the first.
        packet.WriteInteger(pet.Level).WriteInteger(0);
    }
}

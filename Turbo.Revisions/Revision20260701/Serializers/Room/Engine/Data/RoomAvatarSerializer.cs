using System.Globalization;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Engine.Data;

internal class RoomAvatarSerializer
{
    public static void Serialize(IServerPacket packet, RoomAvatarSnapshot item)
    {
        packet
            .WriteInteger(item.WebId)
            .WriteString(item.Name)
            .WriteString(item.Motto)
            .WriteString(item.Figure)
            .WriteInteger(item.ObjectId)
            .WriteInteger(item.X)
            .WriteInteger(item.Y)
            .WriteString(item.Z.ToString())
            .WriteInteger((int)item.BodyRotation)
            .WriteInteger((int)item.AvatarType);

        if (item is RoomPlayerAvatarSnapshot player)
        {
            SerializePlayerAvatar(packet, player);
        }
        else if (item is RoomPetAvatarSnapshot pet)
        {
            SerializePetAvatar(packet, pet);
        }
    }

    public static void SerializePlayerAvatar(
        IServerPacket packet,
        RoomPlayerAvatarSnapshot snapshot
    )
    {
        packet
            .WriteString(AvatarGenderTypeExtensions.ToLegacyString(snapshot.Gender))
            .WriteInteger(snapshot.GroupId)
            .WriteInteger(snapshot.GroupStatus)
            .WriteString(snapshot.GroupName)
            .WriteString(snapshot.SwimFigure)
            .WriteInteger(snapshot.ActivityPoints)
            .WriteBoolean(snapshot.IsModerator);
    }

    public static void SerializePetAvatar(IServerPacket packet, RoomPetAvatarSnapshot snapshot)
    {
        packet
            .WriteInteger(snapshot.SubType)
            .WriteInteger(snapshot.OwnerId)
            .WriteString(snapshot.OwnerName)
            .WriteInteger(snapshot.RarityLevel)
            .WriteBoolean(snapshot.HasSaddle)
            .WriteBoolean(snapshot.IsRiding)
            .WriteBoolean(snapshot.CanBreed)
            .WriteBoolean(snapshot.CanHarvest)
            .WriteBoolean(snapshot.CanRevive)
            .WriteBoolean(snapshot.HasBreedingPermission)
            .WriteInteger(snapshot.PetLevel)
            .WriteString(snapshot.PetPosture);
    }
}

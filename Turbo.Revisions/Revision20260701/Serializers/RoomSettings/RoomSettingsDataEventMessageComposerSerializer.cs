using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.RoomSettings;

internal class RoomSettingsDataEventMessageComposerSerializer(int header)
    : AbstractSerializer<RoomSettingsDataEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomSettingsDataEventMessageComposer message
    )
    {
        RoomSnapshot s = message.Settings;

        packet
            .WriteInteger(s.RoomId)
            .WriteString(s.Name)
            .WriteString(s.Description)
            .WriteInteger(s.OwnerId)
            .WriteString(s.OwnerName)
            .WriteInteger((int)s.DoorMode)
            .WriteString(s.Password)
            .WriteInteger(s.PlayersMax)
            .WriteInteger(s.PlayersMax)
            .WriteInteger(s.CategoryId);

        packet.WriteInteger(s.Tags.Length);
        foreach (string tag in s.Tags)
        {
            packet.WriteString(tag);
        }

        packet
            .WriteInteger((int)s.TradeType)
            .WriteBoolean(s.AllowPets)
            .WriteBoolean(s.AllowPetsEat)
            .WriteBoolean(s.AllowBlocking)
            .WriteBoolean(s.HideWalls)
            .WriteInteger((int)s.WallThickness)
            .WriteInteger((int)s.FloorThickness)
            .WriteInteger((int)s.ModSettings.WhoCanMute)
            .WriteInteger((int)s.ModSettings.WhoCanKick)
            .WriteInteger((int)s.ModSettings.WhoCanBan);

        bool hasGroup = s.GroupId.HasValue && s.GroupName != null && s.GroupBadge != null;
        packet.WriteBoolean(hasGroup);

        if (hasGroup)
        {
            packet
                .WriteInteger(s.GroupId!.Value)
                .WriteString(s.GroupName!)
                .WriteString(s.GroupBadge!);
        }

        packet
            .WriteInteger((int)s.ChatSettings.ChatMode)
            .WriteInteger((int)s.ChatSettings.BubbleWidth)
            .WriteInteger((int)s.ChatSettings.ScrollSpeed)
            .WriteInteger(s.ChatSettings.FullHearRange)
            .WriteInteger((int)s.ChatSettings.FloodSensitivity)
            .WriteBoolean(false);
    }
}

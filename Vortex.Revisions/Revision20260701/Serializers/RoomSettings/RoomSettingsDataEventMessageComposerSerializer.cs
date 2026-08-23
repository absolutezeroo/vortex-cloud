using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

/// <summary>
/// Wire layout is dictated by the room-settings parser of the WIN63-202607011411 client
/// (<c>_SafePkg_2452._SafeCls_3719</c>, reached from <c>onRoomSettingsData</c>). Three traps:
/// it reads neither the owner block nor the password, the allow/hide flags are 4-byte integers
/// compared against 1 rather than single-byte booleans, and the chat block collapsed to a lone
/// flood-sensitivity integer — this revision dropped mode, bubble width, scroll speed and hear
/// range from the packet (the client hardcodes 0/1/1 for the first three).
/// </summary>
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
            .WriteInteger((int)s.DoorMode)
            .WriteInteger(s.CategoryId)
            .WriteInteger(s.PlayersMax)
            .WriteInteger(s.MaxVisitorsLimit);

        packet.WriteInteger(s.Tags.Length);
        foreach (string tag in s.Tags)
        {
            packet.WriteString(tag);
        }

        packet
            .WriteInteger((int)s.TradeType)
            .WriteInteger(s.AllowPets ? 1 : 0)
            .WriteInteger(s.AllowPetsEat ? 1 : 0)
            .WriteInteger(s.AllowBlocking ? 1 : 0)
            .WriteInteger(s.HideWalls ? 1 : 0)
            .WriteInteger((int)s.WallThickness)
            .WriteInteger((int)s.FloorThickness)
            .WriteInteger((int)s.ChatSettings.FloodSensitivity);

        // The four toggles this revision added. They travelled as constants until 2026-08-03,
        // when RoomEntity gained a column for each - the dialog now round-trips.
        packet
            .WriteBoolean(s.LeaveOnDoorTile)
            .WriteBoolean(s.IdleSleepEnabled)
            .WriteInteger(s.IdleSleepTimeoutSeconds)
            .WriteBoolean(s.IdleAutokickEnabled)
            .WriteInteger(s.IdleAutokickTimeoutSeconds)
            .WriteBoolean(s.MuteAllPets);

        packet
            .WriteInteger((int)s.ModSettings.WhoCanMute)
            .WriteInteger((int)s.ModSettings.WhoCanKick)
            .WriteInteger((int)s.ModSettings.WhoCanBan);

        // hiddenByBc — no builders-club room hiding in this emulator.
        packet.WriteBoolean(false);
    }
}

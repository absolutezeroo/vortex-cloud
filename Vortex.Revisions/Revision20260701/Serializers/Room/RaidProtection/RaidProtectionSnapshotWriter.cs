using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.RaidProtection;

/// <summary>
/// The raid-protection snapshot on the wire, written in one place because the client reads it in
/// one place.
/// </summary>
/// <remarks>
/// The split mirrors the client's own: <c>RaidProtectionSettingsSnapshot</c> has
/// <c>readFromMessage</c> (room id then the rest) and <c>readAfterRoomId</c> (the rest alone), and
/// the save reply uses the second so it can put its result code between the two. Writing the nine
/// fields twice, once per serializer, is how the reply drifts out of step with the push.
/// </remarks>
internal static class RaidProtectionSnapshotWriter
{
    /// <summary>Room id, then the nine that follow it.</summary>
    public static void Write(IServerPacket packet, RoomRaidProtectionSnapshot settings)
    {
        packet.WriteInteger(settings.RoomId);
        WriteAfterRoomId(packet, settings);
    }

    /// <summary>Everything but the room id, for a message that has already written it.</summary>
    public static void WriteAfterRoomId(IServerPacket packet, RoomRaidProtectionSnapshot settings)
    {
        packet.WriteBoolean(settings.Enabled);
        packet.WriteInteger(settings.DetectionSensitivity);
        packet.WriteInteger(settings.ActionType);
        packet.WriteInteger(settings.BanDurationSeconds);
        packet.WriteBoolean(settings.GuardEnabled);
        packet.WriteInteger(settings.GuardDurationSeconds);
        packet.WriteInteger(settings.GuardSensitivity);
        packet.WriteBoolean(settings.IncidentActive);
        packet.WriteInteger(settings.LastRaidAtEpochSeconds);
    }
}

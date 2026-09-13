using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.RaidProtection;

/// <summary>
/// Room id, result code, then the snapshot's remaining nine fields.
/// </summary>
/// <remarks>
/// The order is the client's and it is not the obvious one: <c>_SafeCls_4512.parse</c> reads an int,
/// reads the result code, and only then hands the first int to <c>readAfterRoomId</c>. Writing the
/// code first, or writing the room id twice, feeds the client a result code as a room id and every
/// field after it lands one slot out.
/// </remarks>
internal class RaidProtectionSettingsResultMessageComposerSerializer(int header)
    : AbstractSerializer<RaidProtectionSettingsResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RaidProtectionSettingsResultMessageComposer message
    )
    {
        packet.WriteInteger(message.Settings.RoomId);
        packet.WriteInteger((int)message.Result);
        RaidProtectionSnapshotWriter.WriteAfterRoomId(packet, message.Settings);
    }
}

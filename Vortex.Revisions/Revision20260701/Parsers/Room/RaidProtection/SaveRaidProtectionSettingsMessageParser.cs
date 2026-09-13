using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.RaidProtection;

/// <summary>
/// The nine fields <c>_SafeCls_3936</c> pushes, in its constructor's order (AIR 1.0.31). The three
/// booleans are one byte each: the client's encoder writes a <c>Boolean</c> with
/// <c>ByteArray.writeBoolean</c>, not as an int (<c>wireformat/_SafeCls_3973.as:45</c>).
/// </summary>
internal class SaveRaidProtectionSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SaveRaidProtectionSettingsMessage
        {
            RoomId = packet.PopInt(),
            Enabled = packet.PopBoolean(),
            DetectionSensitivity = packet.PopInt(),
            ActionType = packet.PopInt(),
            BanDurationSeconds = packet.PopInt(),
            GuardEnabled = packet.PopBoolean(),
            GuardDurationSeconds = packet.PopInt(),
            GuardSensitivity = packet.PopInt(),
            Confirmed = packet.PopBoolean(),
        };
}

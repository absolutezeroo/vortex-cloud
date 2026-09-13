using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.RaidProtection;

/// <summary>One int. <c>_SafeCls_3402</c> pushes the room id and nothing else (AIR 1.0.31).</summary>
internal class GetRaidProtectionSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetRaidProtectionSettingsMessage { RoomId = packet.PopInt() };
}

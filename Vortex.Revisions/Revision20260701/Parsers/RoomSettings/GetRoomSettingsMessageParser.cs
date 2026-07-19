using Vortex.Primitives.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.RoomSettings;

internal class GetRoomSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetRoomSettingsMessage { RoomId = packet.PopInt() };
}

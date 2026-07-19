using Vortex.Primitives.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.RoomSettings;

internal class GetBannedUsersFromRoomMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetBannedUsersFromRoomMessage { RoomId = packet.PopInt() };
}

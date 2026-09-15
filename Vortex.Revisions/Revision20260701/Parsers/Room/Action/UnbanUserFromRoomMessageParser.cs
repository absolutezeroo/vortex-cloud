using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Action;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Action;

internal class UnbanUserFromRoomMessageParser : IParser
{
    // Two ints, not one: the banned-users tab sends `new _SafeCls_2593(userId, flatId)`
    // (RoomSettingsCtrl.as:1511), and the composer pushes both in that order. The room id was
    // being left on the wire and the handler fell back to the session's current room.
    public IMessageEvent Parse(IClientPacket packet) =>
        new UnbanUserFromRoomMessage { UserId = packet.PopInt(), RoomId = packet.PopInt() };
}

using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class RemoveOwnRoomRightsRoomMessageParser : IParser
{
    // One int: the room to drop your own rights in (HabboNavigator.as:385). It was never read, so
    // RoomId was always 0 -- and the handler guards on `message.RoomId > 0`, which meant the whole
    // feature returned silently every time it was used.
    public IMessageEvent Parse(IClientPacket packet) =>
        new RemoveOwnRoomRightsRoomMessage { RoomId = packet.PopInt() };
}

using Vortex.Primitives.Messages.Incoming.Inventory.Furni;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Furni;

internal class RequestRoomPropertySetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RequestRoomPropertySetMessage { RoomId = packet.PopInt() };
}

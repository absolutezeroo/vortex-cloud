using Vortex.Primitives.Messages.Incoming.Room.Layout;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Layout;

internal class GetRoomEntryTileMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetRoomEntryTileMessage();
}

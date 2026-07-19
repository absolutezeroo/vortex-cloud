using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class RoomTextSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new RoomTextSearchMessage { Query = packet.PopString() };
}

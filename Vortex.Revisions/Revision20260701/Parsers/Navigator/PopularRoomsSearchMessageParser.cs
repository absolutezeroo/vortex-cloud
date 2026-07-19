using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class PopularRoomsSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PopularRoomsSearchMessage { Query = packet.PopString(), AdIndex = packet.PopInt() };
}

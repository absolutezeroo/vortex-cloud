using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class MyFriendsRoomsSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new MyFriendsRoomsSearchMessage();
}

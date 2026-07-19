using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

public class HabboSearchMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new HabboSearchMessage { SearchQuery = packet.PopString() };
}

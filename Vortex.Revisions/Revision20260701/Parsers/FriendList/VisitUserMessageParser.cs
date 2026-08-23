using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.FriendList;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

public class VisitUserMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new VisitUserMessage { PlayerName = packet.PopString() };
}

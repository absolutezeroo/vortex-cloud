using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.FriendList;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

internal class GetMessengerHistoryMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetMessengerHistoryMessage { ChatId = packet.PopInt(), Message = packet.PopString() };
}

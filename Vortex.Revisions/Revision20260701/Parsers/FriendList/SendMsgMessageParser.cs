using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

public class SendMsgMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SendMsgMessage
        {
            ChatId = packet.PopInt(),
            Message = packet.PopString(),
            ConfirmationId = packet.PopInt(),
        };
}

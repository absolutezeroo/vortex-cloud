using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

public class SendRoomInviteMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        List<int> friendIds = new List<int>();

        int totalInvites = packet.PopInt();

        for (int i = 0; i < totalInvites; i++)
        {
            friendIds.Add(packet.PopInt());
        }

        string message = packet.PopString();

        return new SendRoomInviteMessage { FriendIds = friendIds, Message = message };
    }
}

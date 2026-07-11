using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.FriendList;

public class DeclineFriendMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        bool declineAll = packet.PopBoolean();

        if (declineAll)
        {
            return new DeclineFriendMessage { DeclineAll = declineAll };
        }
        else
        {
            int friendsCount = packet.PopInt();

            List<int> friends = new List<int>(friendsCount);

            for (int i = 0; i < friendsCount; i++)
            {
                int userId = packet.PopInt();

                friends.Add(userId);
            }

            return new DeclineFriendMessage { DeclineAll = declineAll, Friends = friends };
        }
    }
}

using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

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

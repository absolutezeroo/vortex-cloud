using System.Collections.Generic;
using System.IO;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.FriendList;

public class RemoveFriendMessageParser(int maxFriendIds) : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int totalFriends = packet.PopInt();

        if (totalFriends < 0 || totalFriends > maxFriendIds)
        {
            throw new InvalidDataException(
                $"Client declared an invalid friend id count of {totalFriends} (max {maxFriendIds})."
            );
        }

        List<int> friendIds = new List<int>(totalFriends);

        for (int i = 0; i < totalFriends; i++)
        {
            friendIds.Add(packet.PopInt());
        }

        return new RemoveFriendMessage { FriendIds = friendIds };
    }
}

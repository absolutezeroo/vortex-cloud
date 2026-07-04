using System.Collections.Generic;
using System.IO;
using Turbo.Primitives.Messages.Incoming.FriendList;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.FriendList;

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

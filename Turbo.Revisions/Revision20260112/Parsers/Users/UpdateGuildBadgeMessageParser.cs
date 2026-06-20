using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Users;

internal class UpdateGuildBadgeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int groupId = packet.PopInt();

        int badgeCount = packet.PopInt();
        List<int> badgeParts = new List<int>(badgeCount);
        for (int i = 0; i < badgeCount; i++)
            badgeParts.Add(packet.PopInt());

        return new UpdateGuildBadgeMessage { GroupId = groupId, BadgeParts = badgeParts };
    }
}

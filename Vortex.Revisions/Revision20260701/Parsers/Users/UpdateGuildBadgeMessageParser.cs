using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class UpdateGuildBadgeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int groupId = packet.PopInt();

        int badgeCount = packet.PopInt();
        List<int> badgeParts = new(badgeCount);
        for (int i = 0; i < badgeCount; i++)
        {
            badgeParts.Add(packet.PopInt());
        }

        return new UpdateGuildBadgeMessage { GroupId = groupId, BadgeParts = badgeParts };
    }
}

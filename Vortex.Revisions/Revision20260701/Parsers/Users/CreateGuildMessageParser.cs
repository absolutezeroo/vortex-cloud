using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class CreateGuildMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        string name = packet.PopString();
        string description = packet.PopString();
        int baseRoomId = packet.PopInt();
        int primaryColorId = packet.PopInt();
        int secondaryColorId = packet.PopInt();

        int badgeCount = packet.PopInt();
        List<int> badgeParts = new(badgeCount);
        for (int i = 0; i < badgeCount; i++)
        {
            badgeParts.Add(packet.PopInt());
        }

        return new CreateGuildMessage
        {
            Name = name,
            Description = description,
            PrimaryColorId = primaryColorId,
            SecondaryColorId = secondaryColorId,
            BaseRoomId = baseRoomId,
            BadgeParts = badgeParts,
        };
    }
}

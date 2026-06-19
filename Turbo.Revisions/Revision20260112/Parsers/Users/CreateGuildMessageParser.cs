using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Users;

internal class CreateGuildMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        var name = packet.PopString();
        var description = packet.PopString();
        var primaryColorId = packet.PopInt();
        var secondaryColorId = packet.PopInt();
        var baseRoomId = packet.PopInt();

        var badgeCount = packet.PopInt();
        var badgeParts = new List<int>(badgeCount);
        for (var i = 0; i < badgeCount; i++)
            badgeParts.Add(packet.PopInt());

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

using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Inventory.Badges;

internal class SetActivatedBadgesMessageParser : IParser
{
    private const int SlotCount = 5;

    public IMessageEvent Parse(IClientPacket packet)
    {
        var slots = new List<(int SlotId, string BadgeCode)>(SlotCount);

        for (int i = 0; i < SlotCount; i++)
        {
            var slotId = packet.PopInt();
            var badgeCode = packet.PopString();

            slots.Add((slotId, badgeCode));
        }

        return new SetActivatedBadgesMessage { Slots = slots };
    }
}

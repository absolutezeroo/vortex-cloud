using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.Inventory.Badges;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Badges;

internal class SetActivatedBadgesMessageParser : IParser
{
    private const int SlotCount = 5;

    public IMessageEvent Parse(IClientPacket packet)
    {
        List<(int SlotId, string BadgeCode)> slots = new List<(int SlotId, string BadgeCode)>(
            SlotCount
        );

        for (int i = 0; i < SlotCount; i++)
        {
            int slotId = packet.PopInt();
            string badgeCode = packet.PopString();

            slots.Add((slotId, badgeCode));
        }

        return new SetActivatedBadgesMessage { Slots = slots };
    }
}

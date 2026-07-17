using System.Collections.Generic;
using System.IO;
using Turbo.Primitives.Messages.Incoming.Inventory.Trading;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Inventory.Trading;

internal class AddItemsToTradeMessageParser(int maxTradeItems) : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();

        if (count < 0 || count > maxTradeItems)
        {
            throw new InvalidDataException(
                $"Client declared an invalid trade item count of {count} (max {maxTradeItems})."
            );
        }

        List<int> itemIds = new(count);

        for (int i = 0; i < count; i++)
        {
            itemIds.Add(packet.PopInt());
        }

        return new AddItemsToTradeMessage { ItemIds = itemIds };
    }
}

using System.Collections.Generic;
using System.IO;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Collectibles;

/// <summary>
/// A count then that many asset ids, the same shape the ordinary trade uses for a stack of
/// furniture — and bounded the same way, because the count arrives before the ids and a client
/// claiming a few million of them would otherwise be believed.
/// </summary>
internal class AddNftToTradeMessageParser(int maxTradeItems) : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        int count = packet.PopInt();

        if (count < 0 || count > maxTradeItems)
        {
            throw new InvalidDataException(
                $"Client declared an invalid trade relic count of {count} (max {maxTradeItems})."
            );
        }

        List<int> assetIds = new(count);

        for (int i = 0; i < count; i++)
        {
            assetIds.Add(packet.PopInt());
        }

        return new AddNftToTradeMessage { AssetIds = assetIds };
    }
}

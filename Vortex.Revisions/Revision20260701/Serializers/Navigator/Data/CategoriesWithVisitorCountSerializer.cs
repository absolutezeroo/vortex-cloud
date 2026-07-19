using System.Collections.Generic;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

internal class CategoriesWithVisitorCountSerializer
{
    public static void Serialize(IServerPacket packet, CategoriesWithVisitorCountSnapshot message)
    {
        packet.WriteInteger(message.CategoriesWithVisitorCount.Count);

        foreach (KeyValuePair<int, List<int>> category in message.CategoriesWithVisitorCount)
        {
            packet
                .WriteInteger(category.Key)
                .WriteInteger(category.Value[0])
                .WriteInteger(category.Value[1]);
        }
    }
}

using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class FavouritesMessageSerializer(int header)
    : AbstractSerializer<FavouritesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FavouritesMessageComposer message)
    {
        packet.WriteInteger(message.Limit).WriteInteger(message.FavoriteRoomIds.Length);

        foreach (int roomId in message.FavoriteRoomIds)
        {
            packet.WriteInteger(roomId);
        }
    }
}

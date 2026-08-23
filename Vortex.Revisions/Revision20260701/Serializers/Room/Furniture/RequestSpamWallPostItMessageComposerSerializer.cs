using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RequestSpamWallPostItMessageComposerSerializer(int header)
    : AbstractSerializer<RequestSpamWallPostItMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RequestSpamWallPostItMessageComposer message
    )
    {
        packet.WriteInteger(message.ItemId).WriteString(message.Location);
    }
}

using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class BlockListMessageComposerSerializer(int header)
    : AbstractSerializer<BlockListMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BlockListMessageComposer message)
    {
        packet.WriteInteger(message.BlockedPlayerIds.Count);

        foreach (int playerId in message.BlockedPlayerIds)
        {
            packet.WriteInteger(playerId);
        }
    }
}

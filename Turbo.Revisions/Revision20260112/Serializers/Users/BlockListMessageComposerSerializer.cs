using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Serializers.Users;

internal class BlockListMessageComposerSerializer(int header)
    : AbstractSerializer<BlockListMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BlockListMessageComposer message)
    {
        packet.WriteInteger(message.BlockedPlayerIds.Count);

        foreach (var playerId in message.BlockedPlayerIds)
            packet.WriteInteger(playerId);
    }
}

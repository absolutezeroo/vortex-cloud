using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class BlockUserUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<BlockUserUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, BlockUserUpdateMessageComposer message)
    {
        packet.WriteInteger(message.PlayerId);
        packet.WriteBoolean(message.IsBlocked);
    }
}

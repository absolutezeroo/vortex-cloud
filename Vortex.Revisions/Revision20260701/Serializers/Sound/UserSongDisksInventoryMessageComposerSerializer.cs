using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Revisions.Revision20260701.Serializers.Sound;

internal class UserSongDisksInventoryMessageComposerSerializer(int header)
    : AbstractSerializer<UserSongDisksInventoryMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UserSongDisksInventoryMessageComposer message
    )
    {
        //
    }
}

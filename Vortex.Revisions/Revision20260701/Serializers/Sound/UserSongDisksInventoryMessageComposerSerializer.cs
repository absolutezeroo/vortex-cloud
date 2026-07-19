using Vortex.Primitives.Messages.Outgoing.Sound;
using Vortex.Primitives.Packets;

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

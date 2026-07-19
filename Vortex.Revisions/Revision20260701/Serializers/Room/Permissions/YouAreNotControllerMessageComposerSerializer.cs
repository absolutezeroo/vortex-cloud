using Vortex.Primitives.Messages.Outgoing.Room.Permissions;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Permissions;

internal class YouAreNotControllerMessageComposerSerializer(int header)
    : AbstractSerializer<YouAreNotControllerMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        YouAreNotControllerMessageComposer message
    )
    {
        //
    }
}

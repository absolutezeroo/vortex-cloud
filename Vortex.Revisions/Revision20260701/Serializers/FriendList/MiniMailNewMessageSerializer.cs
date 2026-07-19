using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class MiniMailNewMessageSerializer(int header)
    : AbstractSerializer<MiniMailNewMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, MiniMailNewMessageComposer message)
    {
        //
    }
}

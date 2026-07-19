using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class InClientLinkMessageComposerSerializer(int header)
    : AbstractSerializer<InClientLinkMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InClientLinkMessageComposer message)
    {
        //
    }
}

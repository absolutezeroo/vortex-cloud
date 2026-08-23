using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Session;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class CloseConnectionMessageComposerSerializer(int header)
    : AbstractSerializer<CloseConnectionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CloseConnectionMessageComposer message)
    {
        //
    }
}

using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Hotlooks;

namespace Vortex.Revisions.Revision20260701.Serializers.Hotlooks;

internal class HotLooksMessageComposerSerializer(int header)
    : AbstractSerializer<HotLooksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HotLooksMessageComposer message)
    {
        //
    }
}

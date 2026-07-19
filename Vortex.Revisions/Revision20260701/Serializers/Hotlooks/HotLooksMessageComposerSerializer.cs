using Vortex.Primitives.Messages.Outgoing.Hotlooks;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Hotlooks;

internal class HotLooksMessageComposerSerializer(int header)
    : AbstractSerializer<HotLooksMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HotLooksMessageComposer message)
    {
        //
    }
}

using Vortex.Primitives.Messages.Outgoing.Advertisement;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Advertisement;

internal class InterstitialMessageComposerSerializer(int header)
    : AbstractSerializer<InterstitialMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InterstitialMessageComposer message)
    {
        //
    }
}

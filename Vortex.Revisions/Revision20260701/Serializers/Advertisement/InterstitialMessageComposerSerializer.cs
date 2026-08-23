using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Advertisement;

namespace Vortex.Revisions.Revision20260701.Serializers.Advertisement;

internal class InterstitialMessageComposerSerializer(int header)
    : AbstractSerializer<InterstitialMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, InterstitialMessageComposer message)
    {
        packet.WriteBoolean(message.CanShowInterstitial);
    }
}

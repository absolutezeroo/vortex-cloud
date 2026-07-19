using Vortex.Primitives.Messages.Outgoing.Landingview;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.LandingView;

internal class PromoArticlesMessageComposerSerializer(int header)
    : AbstractSerializer<PromoArticlesMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PromoArticlesMessageComposer message)
    {
        //
    }
}

using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Catalog;

internal class SnowWarGameTokensMessageMessageComposerSerializer(int header)
    : AbstractSerializer<SnowWarGameTokensMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SnowWarGameTokensMessageMessageComposer message
    )
    {
        //
    }
}

using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintingEnabledMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintingEnabledMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintingEnabledMessageComposer message
    ) => packet.WriteBoolean(message.Enabled);
}

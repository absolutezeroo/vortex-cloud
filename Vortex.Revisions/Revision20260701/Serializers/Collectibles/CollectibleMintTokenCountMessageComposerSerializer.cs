using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectibleMintTokenCountMessageComposerSerializer(int header)
    : AbstractSerializer<CollectibleMintTokenCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectibleMintTokenCountMessageComposer message
    ) => packet.WriteInteger(message.Count);
}

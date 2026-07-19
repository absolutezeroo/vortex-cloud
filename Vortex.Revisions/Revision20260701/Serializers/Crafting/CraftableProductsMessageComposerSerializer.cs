using Vortex.Primitives.Messages.Outgoing.Crafting;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Crafting;

internal class CraftableProductsMessageComposerSerializer(int header)
    : AbstractSerializer<CraftableProductsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CraftableProductsMessageComposer message
    )
    {
        //
    }
}

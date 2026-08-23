using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Crafting;

namespace Vortex.Revisions.Revision20260701.Serializers.Crafting;

internal class CraftingRecipeMessageComposerSerializer(int header)
    : AbstractSerializer<CraftingRecipeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CraftingRecipeMessageComposer message)
    {
        //
    }
}

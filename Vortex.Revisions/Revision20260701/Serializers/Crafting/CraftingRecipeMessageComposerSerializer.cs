using Vortex.Primitives.Messages.Outgoing.Crafting;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Crafting;

internal class CraftingRecipeMessageComposerSerializer(int header)
    : AbstractSerializer<CraftingRecipeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CraftingRecipeMessageComposer message)
    {
        //
    }
}

using Vortex.Primitives.Messages.Outgoing.Crafting;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Crafting;

internal class CraftingRecipesAvailableMessageComposerSerializer(int header)
    : AbstractSerializer<CraftingRecipesAvailableMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CraftingRecipesAvailableMessageComposer message
    )
    {
        //
    }
}

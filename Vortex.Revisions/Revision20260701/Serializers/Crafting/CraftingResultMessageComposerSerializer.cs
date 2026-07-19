using Vortex.Primitives.Messages.Outgoing.Crafting;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Crafting;

internal class CraftingResultMessageComposerSerializer(int header)
    : AbstractSerializer<CraftingResultMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CraftingResultMessageComposer message)
    {
        //
    }
}

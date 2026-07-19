using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class PostItPlacedEventMessageComposerSerializer(int header)
    : AbstractSerializer<PostItPlacedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PostItPlacedEventMessageComposer message
    )
    {
        //
    }
}

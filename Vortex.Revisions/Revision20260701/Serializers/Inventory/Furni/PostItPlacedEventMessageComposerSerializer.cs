using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Inventory.Furni;

namespace Vortex.Revisions.Revision20260701.Serializers.Inventory.Furni;

internal class PostItPlacedEventMessageComposerSerializer(int header)
    : AbstractSerializer<PostItPlacedEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        PostItPlacedEventMessageComposer message
    )
    {
        // Was empty: the header went out with a zero-length body while the client read two ints
        // off it, so this was a desync waiting for the first post-it to be placed, not a missing
        // feature.
        packet.WriteInteger(message.ItemId).WriteInteger(message.ItemsLeft);
    }
}

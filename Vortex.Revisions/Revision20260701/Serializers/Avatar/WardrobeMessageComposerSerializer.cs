using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Avatar;

internal class WardrobeMessageComposerSerializer(int header)
    : AbstractSerializer<WardrobeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WardrobeMessageComposer message)
    {
        // Leading int is "state", unused by the client's WardrobeModel.updateSlots; then the count
        // and each outfit as (slotId, figure, gender) — matches WardrobeMessageParser / OutfitData.
        packet.WriteInteger(0).WriteInteger(message.Outfits.Count);

        foreach (PlayerWardrobeOutfitSnapshot outfit in message.Outfits)
        {
            packet
                .WriteInteger(outfit.SlotId)
                .WriteString(outfit.Figure)
                .WriteString(outfit.Gender);
        }
    }
}

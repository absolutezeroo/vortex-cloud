using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Collectibles;

internal class CollectableMintableItemTypesMessageComposerSerializer(int header)
    : AbstractSerializer<CollectableMintableItemTypesMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CollectableMintableItemTypesMessageComposer message
    )
    {
        packet.WriteInteger(message.ItemTypes.Length);

        foreach (MintableItemTypeSnapshot type in message.ItemTypes)
        {
            packet
                .WriteInteger(type.ItemTypeId)
                .WriteInteger(type.StartTime)
                .WriteInteger(type.EndTime)
                .WriteBoolean(type.RegionLocked)
                .WriteInteger(type.Price)
                .WriteBoolean(type.LimitedEdition)
                // A short, and last: the only non-int in the row.
                .WriteShort(type.ItemType);
        }
    }
}

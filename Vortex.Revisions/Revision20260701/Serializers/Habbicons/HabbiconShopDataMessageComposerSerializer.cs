using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>
/// The client's <c>_SafeCls_4183.parse</c>: a count and that many collection blocks. Nothing else --
/// no wrapper, no trailing flag.
/// </summary>
internal class HabbiconShopDataMessageComposerSerializer(int header)
    : AbstractSerializer<HabbiconShopDataMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabbiconShopDataMessageComposer message)
    {
        packet.WriteInteger(message.Shop.Collections.Length);

        foreach (HabbiconShopCollectionSnapshot collection in message.Shop.Collections)
        {
            HabbiconWriter.WriteCollection(packet, collection);
        }
    }
}

using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>
/// The client's <c>_SafeCls_4081.parse</c>: one shop row and nothing around it.
/// </summary>
internal class HabbiconInfoMessageComposerSerializer(int header)
    : AbstractSerializer<HabbiconInfoMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, HabbiconInfoMessageComposer message) =>
        HabbiconWriter.WriteShopItem(packet, message.Habbicon);
}

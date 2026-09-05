using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Revisions.Revision20260701.Serializers.Habbicons;

/// <summary>
/// The client's <c>_SafeCls_4246.parse</c>: roomIndex FIRST, then habbiconId. The getters are
/// declared the other way round in the decompiled class, which is a trap -- the parse order is what
/// counts, and swapping these two draws the wrong picture over the wrong avatar.
/// </summary>
internal class RoomUseHabbiconMessageComposerSerializer(int header)
    : AbstractSerializer<RoomUseHabbiconMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RoomUseHabbiconMessageComposer message
    ) => packet.WriteInteger(message.RoomIndex).WriteInteger(message.HabbiconId);
}

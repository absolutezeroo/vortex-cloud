using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.RaidProtection;

/// <summary>Room id then a flag — <c>_SafeCls_4332.parse</c>, AIR 1.0.31.</summary>
internal class RaidProtectionCapabilityMessageComposerSerializer(int header)
    : AbstractSerializer<RaidProtectionCapabilityMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RaidProtectionCapabilityMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId);
        packet.WriteBoolean(message.CanManage);
    }
}

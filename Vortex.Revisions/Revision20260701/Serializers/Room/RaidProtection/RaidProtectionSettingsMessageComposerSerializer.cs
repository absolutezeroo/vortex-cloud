using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.RaidProtection;

/// <summary>
/// The bare snapshot — <c>_SafeCls_4206.parse</c> calls <c>readFromMessage</c>, which takes the room
/// id first (AIR 1.0.31).
/// </summary>
internal class RaidProtectionSettingsMessageComposerSerializer(int header)
    : AbstractSerializer<RaidProtectionSettingsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RaidProtectionSettingsMessageComposer message
    ) => RaidProtectionSnapshotWriter.Write(packet, message.Settings);
}

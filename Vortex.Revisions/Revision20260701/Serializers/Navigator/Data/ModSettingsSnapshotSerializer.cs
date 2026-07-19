using Vortex.Primitives.Orleans.Snapshots.Room.Settings;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator.Data;

internal class ModSettingsSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, ModSettingsSnapshot message)
    {
        packet.WriteInteger((int)message.WhoCanMute);
        packet.WriteInteger((int)message.WhoCanKick);
        packet.WriteInteger((int)message.WhoCanBan);
    }
}

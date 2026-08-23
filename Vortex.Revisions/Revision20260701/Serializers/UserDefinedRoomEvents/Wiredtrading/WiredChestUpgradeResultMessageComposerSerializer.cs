using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestUpgradeResultMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestUpgradeResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredChestUpgradeResultMessageComposer message
    ) => packet.WriteInteger(message.ChestId).WriteInteger(message.ResultCode);
}

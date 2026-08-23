using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestUpdateSuccessMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestUpdateSuccessMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredChestUpdateSuccessMessageComposer message
    ) => packet.WriteInteger(message.ChestId).WriteBoolean(message.IsNotificationPreferences);
}

using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredChestCoinsMessageComposerSerializer(int header)
    : AbstractSerializer<WiredChestCoinsMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredChestCoinsMessageComposer message)
    {
        packet
            .WriteInteger(message.ChestId)
            .WriteInteger(message.Coins)
            .WriteBoolean(message.IsUpdate);
    }
}

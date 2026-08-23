using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredTradeCancelledMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeCancelledMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeCancelledMessageComposer message
    ) => packet.WriteInteger(message.TransactionFailureTypeId);
}

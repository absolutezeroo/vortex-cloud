using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredTradeCompletedMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeCompletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeCompletedMessageComposer message
    ) { }
}

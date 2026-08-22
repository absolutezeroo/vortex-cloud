using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

internal class WiredTradeCompletedMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeCompletedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeCompletedMessageComposer message
    ) { }
}

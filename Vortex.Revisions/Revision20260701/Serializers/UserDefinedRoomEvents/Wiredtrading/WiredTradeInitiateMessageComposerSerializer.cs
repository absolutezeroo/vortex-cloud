using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The requirement reads itself first and its length varies with its type, so the three
/// presentation fields can only follow it — the client's parser has the same ordering constraint
/// and says so.
/// </summary>
internal class WiredTradeInitiateMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeInitiateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeInitiateMessageComposer message
    )
    {
        packet
            .WriteInteger(message.RequirementType)
            .WriteString(message.YouGetText)
            .WriteString(message.LayoutType)
            .WriteBoolean(message.ShowRequirementsImmediate)
            .WriteBoolean(message.OverridePreviousTrade)
            .WriteInteger(message.TimeoutSeconds);
    }
}

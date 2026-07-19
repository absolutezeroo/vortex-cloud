using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents;

internal class WiredValidationErrorEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredValidationErrorEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredValidationErrorEventMessageComposer message
    )
    {
        packet.WriteString(message.LocalizationKey).WriteInteger(message.Parameters.Count);

        foreach ((string key, string value) in message.Parameters)
        {
            packet.WriteString(key).WriteString(value);
        }
    }
}

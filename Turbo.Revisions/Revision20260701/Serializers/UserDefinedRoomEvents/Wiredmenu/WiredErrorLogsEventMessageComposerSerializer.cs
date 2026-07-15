using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredErrorLogsEventMessageComposerSerializer(int header)
    : AbstractSerializer<WiredErrorLogsEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredErrorLogsEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Entries.Count);

        foreach (WiredErrorLogEntry entry in message.Entries)
        {
            packet
                .WriteInteger(entry.ErrorId)
                .WriteString(entry.ErrorName)
                .WriteString(entry.Category)
                .WriteInteger(entry.ThrowCount)
                .WriteLong(entry.MsSinceLastOccurrence);
        }
    }
}

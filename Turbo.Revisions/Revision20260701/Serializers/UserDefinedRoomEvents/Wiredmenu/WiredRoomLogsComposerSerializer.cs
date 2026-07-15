using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredmenu;

internal class WiredRoomLogsComposerSerializer(int header)
    : AbstractSerializer<WiredRoomLogsComposer>(header)
{
    protected override void Serialize(IServerPacket packet, WiredRoomLogsComposer message)
    {
        packet
            .WriteInteger(message.TotalEntries)
            .WriteInteger(message.CurrentPage)
            .WriteInteger(message.Amount)
            .WriteInteger(message.Entries.Count);

        foreach (WiredRoomLogEntry entry in message.Entries)
        {
            packet
                .WriteLong(entry.Id)
                .WriteByte((byte)entry.LogLevel)
                .WriteByte((byte)entry.LogSource)
                .WriteString(entry.Message)
                .WriteLong(entry.Timestamp)
                .WriteString(entry.TimestampStr);
        }

        packet.WriteBoolean(message.LogLevelFilter.HasValue);

        if (message.LogLevelFilter.HasValue)
        {
            packet.WriteByte((byte)message.LogLevelFilter.Value);
        }

        packet.WriteBoolean(message.LogSourceFilter.HasValue);

        if (message.LogSourceFilter.HasValue)
        {
            packet.WriteByte((byte)message.LogSourceFilter.Value);
        }

        packet.WriteBoolean(message.Query is not null);

        if (message.Query is not null)
        {
            packet.WriteString(message.Query);
        }
    }
}
